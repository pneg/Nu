#shader vertex
#version 410 core

const int TexCoordsOffsetVerts = 6;

const vec2 TexCoordsOffsetFilters[TexCoordsOffsetVerts] =
    vec2[TexCoordsOffsetVerts](
        vec2(1,1),
        vec2(0,1),
        vec2(0,0),
        vec2(1,1),
        vec2(0,0),
        vec2(1,0));

const vec2 TexCoordsOffsetFilters2[TexCoordsOffsetVerts] =
    vec2[TexCoordsOffsetVerts](
        vec2(0,0),
        vec2(1,0),
        vec2(1,1),
        vec2(0,0),
        vec2(1,1),
        vec2(0,1));

uniform mat4 view;
uniform mat4 projection;

layout (location = 0) in vec3 position;
layout (location = 1) in vec2 texCoords;
layout (location = 2) in vec3 normal;
layout (location = 3) in vec4 splat0;
layout (location = 4) in vec4 splat1;
layout (location = 5) in mat4 model;
layout (location = 9) in vec4 texCoordsOffset;
layout (location = 10) in vec4 albedo;
layout (location = 11) in vec4 material;
layout (location = 12) in float height;
layout (location = 13) in int invertRoughness;

out vec4 positionOut;
out vec2 texCoordsOut;
out vec3 normalOut;
out vec4 splat0Out;
out vec4 splat1Out;
flat out vec4 albedoOut;
flat out vec4 materialOut;
flat out float heightOut;
flat out int invertRoughnessOut;

void main()
{
    positionOut = model * vec4(position, 1.0);
    int texCoordsOffsetIndex = gl_VertexID % TexCoordsOffsetVerts;
    vec2 texCoordsOffsetFilter = TexCoordsOffsetFilters[texCoordsOffsetIndex];
    vec2 texCoordsOffsetFilter2 = TexCoordsOffsetFilters2[texCoordsOffsetIndex];
    texCoordsOut = texCoords + texCoordsOffset.xy * texCoordsOffsetFilter + texCoordsOffset.zw * texCoordsOffsetFilter2;
    albedoOut = albedo;
    materialOut = material;
    normalOut = transpose(inverse(mat3(model))) * normal;
    heightOut = height;
    invertRoughnessOut = invertRoughness;
    splat0Out = vec4(splat0.r, splat0.g, splat0.b, splat0.a);
    splat1Out = vec4(splat1.r, splat1.g, splat1.b, splat1.a);
    gl_Position = projection * view * positionOut;
}

#shader fragment
#version 410 core

const float GAMMA = 2.2;
const int TERRAIN_LAYERS_MAX = 4;

uniform vec3 eyeCenter;
uniform int layerCount;
uniform sampler2D albedoTextures[TERRAIN_LAYERS_MAX];
uniform sampler2D roughnessTextures[TERRAIN_LAYERS_MAX];
uniform sampler2D ambientOcclusionTextures[TERRAIN_LAYERS_MAX];
uniform sampler2D normalTextures[TERRAIN_LAYERS_MAX];
uniform sampler2D heightTextures[TERRAIN_LAYERS_MAX];

in vec4 positionOut;
in vec2 texCoordsOut;
in vec3 normalOut;
in vec4 splat0Out;
in vec4 splat1Out;
flat in vec4 albedoOut;
flat in vec4 materialOut;
flat in float heightOut;
flat in int invertRoughnessOut;

layout (location = 0) out vec4 position;
layout (location = 1) out vec3 albedo;
layout (location = 2) out vec4 material;
layout (location = 3) out vec4 normalAndHeight;

void main()
{
    // forward position
    position = positionOut;

    // compute spatial converters
    vec3 q1 = dFdx(positionOut.xyz);
    vec3 q2 = dFdy(positionOut.xyz);
    vec2 st1 = dFdx(texCoordsOut);
    vec2 st2 = dFdy(texCoordsOut);
    vec3 normal = normalize(normalOut);
    vec3 tangent = normalize(q1 * st2.t - q2 * st1.t);
    vec3 binormal = -normalize(cross(normal, tangent));
    mat3 toWorld = mat3(tangent, binormal, normal);
    mat3 toTangent = transpose(toWorld);

    // blend height
    float heightBlend = 0.0;
    for (int i = 0; i < layerCount; ++i)
    {
        heightBlend += texture(heightTextures[i], texCoordsOut).r * splat0Out[i];
    }

    // compute tex coords in parallax space
    vec3 eyeCenterTangent = toTangent * eyeCenter;
    vec3 positionTangent = toTangent * positionOut.xyz;
    vec3 toEyeTangent = normalize(eyeCenterTangent - positionTangent);
    float height = heightBlend * heightOut;
    vec2 parallax = toEyeTangent.xy * height;
    vec2 texCoords = texCoordsOut - parallax;

    // blend other materials
    vec4 albedoBlend = vec4(0.0);
    for (int i = 0; i < layerCount; ++i)
    {
        albedoBlend += texture(albedoTextures[i], texCoords) * splat0Out[i];
    }

    float roughnessBlend = 0.0;
    for (int i = 0; i < layerCount; ++i)
    {
        vec4 roughness = texture(roughnessTextures[i], texCoords);
        roughnessBlend += (roughness.a == 1.0f ? roughness.g : roughness.a) * splat0Out[i];
    }

    float ambientOcclusionBlend = 0.0;
    for (int i = 0; i < layerCount; ++i)
    {
        ambientOcclusionBlend += texture(ambientOcclusionTextures[i], texCoords).b * splat0Out[i];
    }

    vec3 normalBlend = vec3(0.0);
    for (int i = 0; i < layerCount; ++i)
    {
        normalBlend += texture(normalTextures[i], texCoords).xyz * splat0Out[i];
    }

    // compute albedo, discarding on zero alpha
    vec4 albedoSample = albedoBlend;
    if (albedoSample.a == 0.0f) discard;
    albedo = pow(albedoSample.rgb, vec3(GAMMA)) * albedoOut.rgb;

    // compute material properties
    float metallic = 0.0f;
    float ambientOcclusion = ambientOcclusionBlend * materialOut.b;
    float roughness = (invertRoughnessOut == 0 ? roughnessBlend : 1.0f - roughnessBlend) * materialOut.g;
    float emission = 0.0f;
    material = vec4(metallic, roughness, ambientOcclusion, emission);

    // compute normal and height
    normalAndHeight.xyz = normalize(toWorld * (normalBlend * 2.0 - 1.0));
    normalAndHeight.a = height;
}
