﻿namespace BlazeVector
open System
open System.Numerics
open Prime
open Nu
open BlazeVector

type PlayerDispatcher () =
    inherit Entity2dDispatcherImNui (true, false, false)

    static member Facets =
        [typeof<RigidBodyFacet>
         typeof<AnimatedSpriteFacet>]

    static member Properties =
        [define Entity.Size (v3 24.0f 48.0f 0.0f)
         define Entity.Presence Omnipresent
         define Entity.Static false
         define Entity.BodyType Dynamic
         define Entity.BodyShape (CapsuleShape { Height = 0.5f; Radius = 0.25f; TransformOpt = None; PropertiesOpt = None })
         define Entity.Friction 0.0f
         define Entity.LinearDamping 3.0f
         define Entity.AngularFactor v3Zero
         define Entity.GravityOverride (Some v3Zero)
         define Entity.CelCount 16
         define Entity.CelRun 4
         define Entity.CelSize (v2 48.0f 96.0f)
         define Entity.AnimationDelay (UpdateTime 3L)
         define Entity.AnimationSheet Assets.Gameplay.PlayerImage]

    override this.Process (entity, world) =

        // walk
        let bodyId = entity.GetBodyId world
        let world =
            let groundTangentOpt = World.getBodyToGroundContactTangentOpt bodyId world
            let force =
                match groundTangentOpt with
                | Some groundTangent ->
                    let downForce = if groundTangent.Y > 0.0f then Constants.Gameplay.PlayerClimbForce else 0.0f
                    Vector3.Multiply (groundTangent, v3 Constants.Gameplay.PlayerWalkForce downForce 0.0f)
                | None -> v3 Constants.Gameplay.PlayerWalkForce Constants.Gameplay.PlayerFallForce 0.0f
            World.applyBodyForce force None bodyId world

        // jump
        let world =
            if World.getBodyGrounded bodyId world && World.isKeyboardKeyPressed KeyboardKey.Space world then
                let world = World.jumpBody true Constants.Gameplay.PlayerJumpSpeed bodyId world
                World.playSound Constants.Audio.SoundVolumeDefault Assets.Gameplay.JumpSound world
                world
            else world

        // shoot when above fall height every 5 updates
        let world =
            if (entity.GetPosition world).Y > -320.0f && world.UpdateTime % 5L = 0L then
                let (bullet, world) = World.createEntity<BulletDispatcher> NoOverlay None entity.Group world // OPTIMIZATION: NoOverlay to avoid reflection.
                let world = bullet.SetPosition (entity.GetPosition world + v3 24.0f 1.0f 0.0f) world
                let world = bullet.SetElevation (entity.GetElevation world) world
                let world = bullet.SetCreationTime world.UpdateTime world
                let world = World.applyBodyLinearImpulse (v3 Constants.Gameplay.BulletForce 0.0f 0.0f) None (bullet.GetBodyId world) world
                World.playSound Constants.Audio.SoundVolumeDefault Assets.Gameplay.ShotSound world
                world
            else world

        // fin
        world