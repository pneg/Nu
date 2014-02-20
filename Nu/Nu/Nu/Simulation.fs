﻿namespace Nu
open System
open System.Collections.Generic
open System.Reflection
open SDL2
open OpenTK
open TiledSharp
open Nu
open Nu.Core
open Nu.Math

// WISDOM: On avoiding threads where possible...
//
// Beyond the cases where persistent threads are absolutely required or where transient threads
// implement embarassingly parallel processes, threads should be AVOIDED as a rule.
//
// If it were the case that physics were processed on a separate hardware component and thereby
// ought to be run on a separate persistent thread, then the proper way to approach the problem of
// physics system queries is to copy the relevant portion of the physics state from the PPU to main
// memory every frame. This way, queries against the physics state can be done IMMEDIATELY with no
// need for complex intermediate states (albeit against a physics state that is one frame old).

/// Describes data relevant to specific event messages.
type [<ReferenceEquality>] MessageData =
    | MouseMoveData of Vector2
    | MouseButtonData of Vector2 * MouseButton
    | CollisionData of Vector2 * single * Address
    | OtherData of obj
    | NoData

/// A generic message for the Nu game engine.
/// A reference type.
type [<ReferenceEquality>] Message =
    { Handled : bool
      Data : MessageData }

type [<StructuralEquality; NoComparison; CLIMutable>] Entity =
    { Id : Id
      Name : string
      Enabled : bool
      Visible : bool
      Xtension : Xtension
      Position : Vector2
      Depth : single
      Size : Vector2
      Rotation : single }

    static member (?) (this : Entity, memberName) =
        fun args ->
            (?) this.Xtension memberName args

    static member (?<-) (this : Entity, memberName, value) =
        let xtension = Xtension.op_DynamicAssignment (this.Xtension, memberName, value)
        { this with Xtension = xtension }

type [<StructuralEquality; NoComparison; CLIMutable>] Button =
    { Entity : Entity }

type [<StructuralEquality; NoComparison; CLIMutable>] Label =
    { Entity : Entity }

type [<StructuralEquality; NoComparison; CLIMutable>] TextBox =
    { Entity : Entity }

type [<StructuralEquality; NoComparison; CLIMutable>] Toggle =
    { Entity : Entity }

type [<StructuralEquality; NoComparison; CLIMutable>] Feeler =
    { Entity : Entity }

type [<StructuralEquality; NoComparison; CLIMutable>] Block =
    { Entity : Entity }

type [<StructuralEquality; NoComparison; CLIMutable>] Avatar =
    { Entity : Entity }
      
type [<StructuralEquality; NoComparison; CLIMutable>] TileMap =
    { Entity : Entity }

type [<StructuralEquality; NoComparison>] EntityModel =
    | Button of Button
    | Label of Label
    | TextBox of TextBox
    | Toggle of Toggle
    | Feeler of Feeler
    | Block of Block
    | Avatar of Avatar
    | TileMap of TileMap

type [<StructuralEquality; NoComparison>] TileMapData =
    { Map : TmxMap
      MapSize : int * int
      TileSize : int * int
      TileSizeF : Vector2
      TileSet : TmxTileset
      TileSetSize : int * int }
      
type [<StructuralEquality; NoComparison>] TileLayerData =
    { Layer : TmxLayer
      Tiles : TmxLayerTile List }
      
type [<StructuralEquality; NoComparison>] TileData =
    { Tile : TmxLayerTile
      I : int
      J : int
      Gid : int
      GidPosition : int
      Gid2 : int * int
      OptTileSetTile : TmxTilesetTile option
      TilePosition : int * int
      TileSetPosition : int * int }

type [<StructuralEquality; NoComparison; CLIMutable>] Group =
    { Id : Id
      Xtension : Xtension }

    static member (?) (this : Group, memberName) =
        fun args ->
            (?) this.Xtension memberName args

    static member (?<-) (this : Group, memberName, value) =
        let xtension = Xtension.op_DynamicAssignment (this.Xtension, memberName, value)
        { this with Xtension = xtension }

type [<StructuralEquality; NoComparison>] TransitionType =
    | Incoming
    | Outgoing

type [<StructuralEquality; NoComparison; CLIMutable>] Transition =
    { Id : Id
      Lifetime : int
      Ticks : int
      Type : TransitionType
      OptDissolveSprite : Sprite option
      Xtension : Xtension }

    static member (?) (this : Transition, memberName) =
        fun args ->
            (?) this.Xtension memberName args

    static member (?<-) (this : Transition, memberName, value) =
        let xtension = Xtension.op_DynamicAssignment (this.Xtension, memberName, value)
        { this with Xtension = xtension }

type [<StructuralEquality; NoComparison>] ScreenState =
    | IncomingState
    | OutgoingState
    | IdlingState

type [<StructuralEquality; NoComparison; CLIMutable>] Screen =
    { Id : Id
      State : ScreenState
      Incoming : Transition
      Outgoing : Transition
      Xtension : Xtension }

    static member (?) (this : Screen, memberName) =
        fun args ->
            (?) this.Xtension memberName args

    static member (?<-) (this : Screen, memberName, value) =
        let xtension = Xtension.op_DynamicAssignment (this.Xtension, memberName, value)
        { this with Xtension = xtension }

type [<StructuralEquality; NoComparison; CLIMutable>] Game =
    { Id : Id
      OptSelectedScreenAddress : Address option
      Xtension : Xtension }

    static member (?) (this : Game, memberName) =
        fun args ->
            (?) this.Xtension memberName args

    static member (?<-) (this : Game, memberName, value) =
        let xtension = Xtension.op_DynamicAssignment (this.Xtension, memberName, value)
        { this with Xtension = xtension }

/// Describes a game message subscription.
/// A reference type.
type [<ReferenceEquality>] Subscription =
    Subscription of (Address -> Address -> Message -> World -> (Message * bool * World))

/// A map of game message subscriptions.
/// A reference type due to the reference-typeness of Subscription.
and Subscriptions = Map<Address, (Address * Subscription) list>

/// The world, in a functional programming sense.
/// A reference type with some value semantics.
and [<ReferenceEquality>] World =
    { Game : Game
      Screens : Map<Lun, Screen>
      Groups : Map<Lun, Map<Lun, Group>>
      EntityModels : Map<Lun, Map<Lun, Map<Lun, EntityModel>>>
      Camera : Camera
      Subscriptions : Subscriptions
      MouseState : MouseState
      AudioPlayer : AudioPlayer
      Renderer : Renderer
      Integrator : Integrator
      AssetMetadataMap : AssetMetadataMap
      AudioMessages : AudioMessage rQueue
      RenderMessages : RenderMessage rQueue
      PhysicsMessages : PhysicsMessage rQueue
      XTypes : XTypes
      Dispatchers : IXDispatchers
      ExtData : obj } // TODO: consider if this is still the right approach in the context of the new Xtension stuff

    interface IXDispatcherContainer with
        member this.GetDispatchers () = this.Dispatchers
        end

type [<StructuralEquality; NoComparison>] Simulant =
    | Game of Game
    | Screen of Screen
    | Group of Group
    | EntityModel of EntityModel