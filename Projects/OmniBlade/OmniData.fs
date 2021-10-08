﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace OmniBlade
open System
open System.Numerics
open System.IO
open FSharpx.Collections
open TiledSharp
open Prime
open Nu

type CharacterIndex =
    | AllyIndex of int
    | EnemyIndex of int

    member this.IsAlly =
        match this with
        | AllyIndex _ -> true
        | EnemyIndex _ -> false

    member this.IsEnemy =
        not this.IsAlly

    static member isFriendly index index2 =
        match (index, index2) with
        | (AllyIndex _, AllyIndex _) -> true
        | (EnemyIndex _, EnemyIndex _) -> true
        | (_, _) -> false

    static member toEntityName index =
        match index with
        | AllyIndex i -> "Ally+" + scstring i
        | EnemyIndex i -> "Enemy+" + scstring i

type Direction =
    | Upward
    | Rightward
    | Downward
    | Leftward

    member this.Opposite =
        match this with
        | Upward -> Downward
        | Rightward -> Leftward
        | Downward -> Upward
        | Leftward -> Rightward

    static member ofVector2 (v2 : Vector2) =
        let angle = double (atan2 v2.Y v2.X)
        let angle = if angle < 0.0 then angle + Math.PI * 2.0 else angle
        let direction =
            if      angle > Math.PI * 1.74997 || angle <= Math.PI * 0.25003 then    Rightward
            elif    angle > Math.PI * 0.74997 && angle <= Math.PI * 1.25003 then    Leftward
            elif    angle > Math.PI * 0.25 && angle <= Math.PI * 0.75 then          Upward
            else                                                                    Downward
        direction

    static member toVector2 direction =
        match direction with
        | Upward -> v2Up
        | Rightward -> v2Right
        | Downward -> v2Down
        | Leftward -> v2Left

type EffectType =
    | Physical
    | Magical

type AffinityType =
    | Fire
    | Ice
    | Lightning
    | Water
    //| Wind - maybe in a sequal...
    | Dark
    | Light
    | Earth
    | Metal
    | Insect

    static member getScalar source target =
        match (source, target) with
        | (Fire, Fire) -> Constants.Battle.AffinityResistanceScalar
        | (Ice, Ice) -> Constants.Battle.AffinityResistanceScalar
        | (Lightning, Lightning) -> Constants.Battle.AffinityResistanceScalar
        | (Water, Water) -> Constants.Battle.AffinityResistanceScalar
        | (Dark, Dark) -> Constants.Battle.AffinityResistanceScalar
        | (Light, Light) -> Constants.Battle.AffinityResistanceScalar
        | (Earth, Earth) -> Constants.Battle.AffinityResistanceScalar
        | (Fire, Ice) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Ice, Fire) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Ice, Insect) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Lightning, Water) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Lightning, Metal) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Water, Lightning) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Dark, Light) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Light, Dark) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Earth, Lightning) -> Constants.Battle.AffinityVulnerabilityScalar
        | (_, _) -> 1.0f

type [<CustomEquality; CustomComparison>] StatusType =
    | Poison
    | Silence
    | Sleep
    | Confuse
    //| Blind - maybe in the sequal
    | Time of bool // true = Haste, false = Slow
    | Power of bool * bool // true = Up, false = Down; true = 2, false = 1
    | Magic of bool * bool // true = Up, false = Down; true = 2, false = 1
    | Shield of bool * bool // true = Up, false = Down; true = 2, false = 1
    //| Counter of bool * bool // true = Up, false = Down; true = 2, false = 1 - maybe in the sequal
    //| Provoke of CharacterIndex - maybe in the sequal

    static member randomize this =
        match this with
        | Poison -> Gen.random1 2 = 0
        | Silence -> Gen.random1 3 = 0
        | Sleep -> Gen.random1 4 = 0
        | Confuse -> Gen.random1 3 = 0
        | Time false | Power (false, _) | Magic (false, _) | Shield (false, _) -> Gen.random1 2 = 0
        | Time true | Power (true, _) | Magic (true, _) | Shield (true, _) -> true

    static member enumerate this =
        match this with
        | Poison -> 0
        | Silence -> 1
        | Sleep -> 2
        | Confuse -> 3
        | Time _ -> 4
        | Power (_, _) -> 5
        | Magic (_, _) -> 6
        | Shield (_, _) -> 7

    static member compare this that =
        compare
            (StatusType.enumerate this)
            (StatusType.enumerate that)

    interface StatusType IComparable with
        member this.CompareTo that =
            StatusType.compare this that

    interface IComparable with
        member this.CompareTo that =
            match that with
            | :? StatusType as that -> (this :> StatusType IComparable).CompareTo that
            | _ -> failwithumf ()

    override this.Equals that =
        match that with
        | :? StatusType as that -> StatusType.enumerate this = StatusType.enumerate that
        | _ -> false

    override this.GetHashCode () =
        StatusType.enumerate this

type AimType =
    | EnemyAim of bool // healthy (N/A)
    | AllyAim of bool // healthy
    | AnyAim of bool // healthy
    | NoAim

type TargetType =
    | SingleTarget of AimType
    | ProximityTarget of single * AimType
    | RadialTarget of single * AimType
    | LineTarget of single * AimType
    | SegmentTarget of single * AimType
    | VerticalTarget of single * AimType
    | HorizontalTarget of single * AimType
    | AllTarget of AimType

    static member getAimType targetType =
        match targetType with
        | SingleTarget aimType -> aimType
        | ProximityTarget (_, aimType) -> aimType
        | RadialTarget (_, aimType) -> aimType
        | LineTarget (_, aimType) -> aimType
        | SegmentTarget (_, aimType) -> aimType
        | VerticalTarget (_, aimType) -> aimType
        | HorizontalTarget (_, aimType) -> aimType
        | AllTarget aimType -> aimType

type TechType =
    | Critical
    | Slash
    | DarkCritical
    | Cyclone
    | PoisonCut
    | PowerCut
    | DispelCut
    | DoubleCut
    | Fire
    | Flame
    | Ice
    | Snowball
    | Bolt
    | BoltBeam
    | Stone
    | Quake
    | Cure
    | Empower
    | Aura
    | Enlighten
    | Protect
    | Weaken
    | Muddle
    | ConjureIfrit
    | Slow
    | Purify

type ActionType =
    | Attack
    | Defend
    | Consume of ConsumableType
    | Tech of TechType
    | Wound

type StatureType =
    | SmallStature
    | NormalStature
    | LargeStature
    | BossStature

type ArchetypeType =
    | Apprentice
    | Fighter
    | Ninja
    | Wizard
    | Conjuror
    | Cleric
    | Bat
    | Spider
    | Snake
    | Willowisp
    | Chillowisp
    | Fairy
    | Gel
    | Beetle
    | Rat
    | Scorpion
    | Plant
    | Ghost
    | Goblin
    | Soldier
    | Knight
    | Imp
    | Zombie
    | Skeleton
    | Shaman
    | Glurble
    | Wolfman
    | Dryad
    | Mummy
    | Witch
    | Squidly
    | Merman
    | Feral
    | Thief
    | Lizardman
    | Trixter
    | Monk
    | Gorgon
    | Tortoise
    | Robot
    | Harpy
    | Jack
    | Avian
    | Troll
    | Mare
    | Djinn
    | Naga
    | Jackorider
    | Trap
    | Vampire
    | Cerebus
    | Hydra
    | Gargoyle
    | ShamanBig
    | FireElemental
    | IceElemental
    | LightningElemental
    | EarthElemental
    | Minotaur
    | Dragon
    | Ogre
    | HydraBig
    | Armoros
    | Golem
    | RobotBig
    | Dinoman
    | Arachnos

type ShopType =
    | Chemist
    | Armory

type ShopkeepAppearanceType =
    | Male
    | Female
    | Fancy

type FieldType =
    | EmptyField
    | DebugField
    | TombOuter
    | TombGround
    | TombBasement
    | Castle of int
    | CastleConnector
    | Forest of int
    | ForestConnector
    | Factory of int
    | FactoryConnector
    | Mountain of int
    | MountainConnector
    | DeadSea of int
    | DeadSeaConnector
    | Ruins of int
    | RuinsConnector
    | Castle2 of int
    | Castle2Connector
    | Desert of int
    | DesertConnector
    | Seasons of int
    | SeasonsConnector
    | Volcano of int
    | VolcanoConnector

    static member toFieldName (fieldType : FieldType) =
        match valueToSymbol fieldType with
        | Symbol.Atom (name, _) -> name
        | Symbols ([Symbol.Atom (name , _); _], _) -> name
        | _ -> failwithumf ()

type BattleType =
    | EmptyBattle
    | DebugBattle
    | CastleBattle
    | CastleBattle2
    | CastleBattle3
    | CastleBattle4
    | CastleBattle5
    | CastleBattle6
    | CastleBattle7
    | CastleBattle8
    | CastleBattle9
    | MadTrixterBattle
    | Castle2Battle
    | Castle2Battle2
    | Castle2Battle3
    | Castle2Battle4
    | Castle2Battle5
    | Castle2Battle6
    | Castle2Battle7
    | Castle2Battle8
    | Castle2Battle9
    | HeavyArmorosBattle
    | Castle3Battle
    | Castle3Battle2
    | Castle3Battle3
    | Castle3Battle4
    | Castle3Battle5
    | Castle3Battle6
    | Castle3Battle7
    | Castle3Battle8
    | Castle3Battle9
    | AraneaImplicitumBattle

type EncounterType =
    | DebugEncounter
    | CastleEncounter
    | Castle2Encounter
    | Castle3Encounter

type LockType =
    | BrassKey

type ChestType =
    | WoodenChest
    | BrassChest

type DoorType =
    | WoodenDoor

type PortalIndex =
    | Center
    | North
    | East
    | South
    | West
    | NE
    | SE
    | NW
    | SW
    | IX of int

type PortalType =
    | AirPortal
    | StairsPortal of bool

type NpcType =
    | ShadeNpc
    | MaelNpc
    | RiainNpc
    | PericNpc
    | RavelNpc
    | AdvenNpc
    | EildaenNpc
    | NostrusNpc
    | MadTrixterNpc
    | HeavyArmorosNpc
    | AraneaImplicitumNpc
    
    static member exists advents specialty =
        match specialty with
        | ShadeNpc -> not (Set.contains ShadeRecruited advents)
        | MaelNpc -> not (Set.contains MaelRecruited advents)
        | RiainNpc -> not (Set.contains RiainRecruited advents)
        | PericNpc -> not (Set.contains PericRecruited advents)
        | MadTrixterNpc -> not (Set.contains MadTrixterDefeated advents)
        | HeavyArmorosNpc -> not (Set.contains HeavyArmorosDefeated advents)
        | AraneaImplicitumNpc -> not (Set.contains AraneaImplicitumDefeated advents)
        | RavelNpc | AdvenNpc | EildaenNpc | NostrusNpc -> true

type ShopkeepType =
    | RobehnShopkeep
    | SchaalShopkeep

type FlameType =
    | FatFlame
    | SkinnyFlame
    | SmallFlame
    | LargeFlame

type SwitchType =
    | ThrowSwitch
    
type SensorType =
    | AirSensor
    | HiddenSensor
    | StepPlateSensor

type PoiseType =
    | Poising
    | Defending
    | Charging

type AnimationType =
    | LoopedWithDirection
    | LoopedWithoutDirection
    | SaturatedWithDirection
    | SaturatedWithoutDirection

type CharacterAnimationType =
    | WalkAnimation
    | CelebrateAnimation
    | ReadyAnimation
    | PoiseAnimation of PoiseType
    | AttackAnimation
    | WoundAnimation
    | SpinAnimation
    | DamageAnimation
    | IdleAnimation
    | CastAnimation
    | Cast2Animation
    | SlashAnimation
    | WhirlAnimation
    | BuryAnimation // TODO: get rid of this

type AllyType =
    | Jinn
    | Shade
    | Mael
    | Riain
    | Peric

type EnemyType =
    | DebugGoblin
    | DarkBat
    | BlueGoblin
    | MadMinotaur
    | MadTrixter
    | LowerGorgon
    | FacelessSoldier
    | Hawk
    | HeavyArmoros
    | PitViper
    | Cloak
    | BloodArmoros
    | AraneaImplicitum
    | Kyla

type CharacterType =
    | Ally of AllyType
    | Enemy of EnemyType

    static member getName characterType =
        match characterType with
        | Ally ty -> string ty
        | Enemy ty -> string ty

type [<NoEquality; NoComparison>] SpiritType =
    | WeakSpirit
    | NormalSpirit
    | StrongSpirit

    static member getColor spiritType =
        match spiritType with
        | WeakSpirit -> Color (byte 255, byte 255, byte 255, byte 127)
        | NormalSpirit -> Color (byte 255, byte 191, byte 191, byte 127)
        | StrongSpirit -> Color (byte 255, byte 127, byte 127, byte 127)

type [<NoEquality; NoComparison>] CueTarget =
    | AvatarTarget // (field only)
    | CharacterTarget of CharacterType // (field only)
    | NpcTarget of NpcType // (field only)
    | ShopkeepTarget of ShopkeepType // (field only)
    | CharacterIndexTarget of CharacterIndex // (battle only)

type [<NoEquality; NoComparison>] CuePredicate =
    | Gold of int
    | Item of ItemType
    | Items of ItemType list
    | Advent of Advent
    | Advents of Advent Set

type [<NoEquality; NoComparison>] CueWait =
    | Wait
    | Timed of int64
    | NoWait

type [<NoEquality; NoComparison>] MoveType =
    | Walk
    | Run
    | Mosey
    | Instant

    member this.MoveSpeedOpt =
        match this with
        | Walk -> Some Constants.Gameplay.CueWalkSpeed
        | Run -> Some Constants.Gameplay.CueRunSpeed
        | Mosey -> Some Constants.Gameplay.CueMoseySpeed
        | Instant -> None

    static member computeStepAndStepCount (origin : Vector2) destination (moveType : MoveType) =
        let delta = destination - origin
        match moveType.MoveSpeedOpt with
        | Some moveSpeed ->
            let stepCount = delta.Length () / moveSpeed
            let step = delta / stepCount
            (step, int (ceil stepCount))
        | None -> (delta, 1)

[<Syntax   ("Gold Item Items Advent Advents " +
            "Wait Timed NoWait " +
            "Nil PlaySound PlaySong FadeOutSong Face Glow Recruit Unseal " +
            "AddItem RemoveItem AddAdvent RemoveAdvent " +
            "Wait Animate Fade Warp Battle Dialog Prompt " +
            "If Not Define Assign Parallel Sequence",
            "", "", "", "",
            Constants.PrettyPrinter.DefaultThresholdMin,
            Constants.PrettyPrinter.DetailedThresholdMax)>]
type [<NoEquality; NoComparison>] Cue =
    | Nil
    | PlaySound of single * Sound AssetTag
    | PlaySong of int * int * single * double * Song AssetTag
    | FadeOutSong of int
    | Face of CueTarget * Direction
    | ClearSpirits
    | Recruit of AllyType
    | Unseal of int * Advent
    | AddItem of ItemType
    | RemoveItem of ItemType
    | AddAdvent of Advent
    | RemoveAdvent of Advent
    | ReplaceAdvent of Advent * Advent
    | Wait of int64
    | WaitState of int64
    | Fade of CueTarget * int64
    | FadeState of int64 * CueTarget * int64
    | Animate of CueTarget * CharacterAnimationType * CueWait
    | AnimateState of int64 * CueWait
    | Move of CueTarget * Vector2 * MoveType
    | MoveState of int64 * CueTarget * Vector2 * Vector2 * MoveType
    | Warp of FieldType * Vector2 * Direction
    | WarpState
    | Battle of BattleType * Advent Set // TODO: P1: consider using three Cues (start, end, post) in battle rather than advents directly...
    | BattleState
    | Dialog of string * bool
    | DialogState
    | Prompt of string * (string * Cue) * (string * Cue)
    | PromptState
    | If of CuePredicate * Cue * Cue
    | Not of CuePredicate * Cue * Cue
    | Define of string * Cue
    | Assign of string * Cue
    | Expand of string
    | Parallel of Cue list
    | Sequence of Cue list
    static member isNil cue = match cue with Nil -> true | _ -> false
    static member notNil cue = match cue with Nil -> false | _ -> true
    static member isInterrupting (inventory : Inventory) (advents : Advent Set) cue =
        match cue with
        | Nil | PlaySound _ | PlaySong _ | FadeOutSong _ | Face _ | ClearSpirits | Recruit _ | Unseal _ -> false
        | AddItem _ | RemoveItem _ | AddAdvent _ | RemoveAdvent _ | ReplaceAdvent _ -> false
        | Wait _ | WaitState _ | Fade _ | FadeState _ | Move _ | MoveState _ | Warp _ | WarpState _ | Battle _ | BattleState _ | Dialog _ | DialogState _ | Prompt _ | PromptState _ -> true
        | Animate (_, _, wait) | AnimateState (_, wait) -> match wait with Timed 0L | NoWait -> false | _ -> true
        | If (p, c, a) ->
            match p with
            | Gold gold -> if inventory.Gold >= gold then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Item itemType -> if Inventory.containsItem itemType inventory then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Items itemTypes -> if Inventory.containsItems itemTypes inventory then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Advent advent -> if advents.Contains advent then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Advents advents2 -> if advents.IsSupersetOf advents2 then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
        | Not (p, c, a) ->
            match p with
            | Gold gold -> if inventory.Gold < gold then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Item itemType -> if not (Inventory.containsItem itemType inventory) then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Items itemTypes -> if not (Inventory.containsItems itemTypes inventory) then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Advent advent -> if not (advents.Contains advent) then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Advents advents2 -> if not (advents.IsSupersetOf advents2) then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
        | Define (_, _) | Assign (_, _) -> false
        | Expand _ -> true // NOTE: we just assume this expands into something interrupting to be safe.
        | Parallel cues -> List.exists (Cue.isInterrupting inventory advents) cues
        | Sequence cues -> List.exists (Cue.isInterrupting inventory advents) cues
    static member notInterrupting inventory advents cue = not (Cue.isInterrupting inventory advents cue)

type CueDefinitions =
    Map<string, Cue>

type [<NoEquality; NoComparison>] Branch =
    { Cue : Cue
      Requirements : Advent Set }

[<RequireQualifiedAccess>]
module OmniSeedState =

    type OmniSeedState =
        private
            { RandSeedState : uint64 }

    let rotate isFade fieldType state =
        if not isFade then
            match fieldType with
            | EmptyField | DebugField | TombOuter | TombGround | TombBasement
            | CastleConnector | ForestConnector | FactoryConnector | MountainConnector | DeadSeaConnector
            | RuinsConnector | Castle2Connector | DesertConnector | SeasonsConnector | VolcanoConnector -> state.RandSeedState
            | Castle n -> state.RandSeedState <<< n
            | Forest n -> state.RandSeedState <<< n + 6
            | Factory n -> state.RandSeedState <<< n + 12
            | Mountain n -> state.RandSeedState <<< n + 18
            | DeadSea n -> state.RandSeedState <<< n + 24
            | Ruins n -> state.RandSeedState <<< n + 30
            | Castle2 n -> state.RandSeedState <<< n + 36
            | Desert n -> state.RandSeedState <<< n + 42
            | Seasons n -> state.RandSeedState <<< n + 48
            | Volcano n -> state.RandSeedState <<< n + 54
        else state.RandSeedState <<< 60

    let makeFromSeedState randSeedState =
        { RandSeedState = randSeedState }

    let make () =
        { RandSeedState = Rand.DefaultSeedState }

type OmniSeedState = OmniSeedState.OmniSeedState

type WeaponData =
    { WeaponType : WeaponType // key
      WeaponSubtype : WeaponSubtype
      PowerBase : int
      MagicBase : int
      Cost : int
      Description : string }

type ArmorData =
    { ArmorType : ArmorType // key
      ArmorSubtype : ArmorSubtype
      EnduranceBase : int
      MindBase : int
      Cost : int
      Description : string }
    member this.EnduranceBaseDisplay = this.EnduranceBase / Constants.Gameplay.ArmorStatBaseDisplayDivisor
    member this.MindBaseDisplay = this.MindBase / Constants.Gameplay.ArmorStatBaseDisplayDivisor

type AccessoryData =
    { AccessoryType : AccessoryType // key
      ShieldBase : int
      CounterBase : int
      Immunities : StatusType Set
      AffinityOpt : AffinityType option
      Cost : int
      Description : string }

type ConsumableData =
    { ConsumableType : ConsumableType // key
      Scalar : single
      Curative : bool
      Techative : bool
      Revive : bool
      StatusesAdded : StatusType Set
      StatusesRemoved : StatusType Set
      AimType : AimType
      Cost : int
      Description : string }

type TechData =
    { TechType : TechType // key
      TechCost : int
      EffectType : EffectType
      Scalar : single
      Split : bool
      Curative : bool
      Cancels : bool
      Absorb : single // percentage of outcome that is absorbed by the caster
      AffinityOpt : AffinityType option
      StatusesAdded : StatusType Set
      StatusesRemoved : StatusType Set
      TargetType : TargetType
      Description : string }

    member this.AimType =
        TargetType.getAimType this.TargetType

type ArchetypeData =
    { ArchetypeType : ArchetypeType // key
      Stamina : single // hit points scalar
      Strength : single // power scalar
      Intelligence : single // magic scalar
      Defense : single // defense scalar
      Absorb : single // absorb scalar
      Focus : single // tech points scalar
      Wealth : single // gold scalar
      Mythos : single // exp scala
      WeaponSubtype : WeaponSubtype
      ArmorSubtype : ArmorSubtype
      Techs : Map<int, TechType> // tech availability according to level
      ChargeTechs : (int * TechType) list
      Immunities : StatusType Set
      AffinityOpt : AffinityType option
      Stature : StatureType
      Description : string }

type TechAnimationData =
    { TechType : TechType // key
      TechStart : int64
      TechingStart : int64
      AffectingStart : int64
      AffectingStop : int64
      TechingStop : int64
      TechStop : int64 }

type KeyItemData =
    { KeyItemData : unit }

type [<NoEquality; NoComparison>] DoorData =
    { DoorType : DoorType // key
      DoorKeyOpt : string option
      OpenImage : Image AssetTag
      ClosedImage : Image AssetTag }

type [<NoEquality; NoComparison>] ShopData =
    { ShopType : ShopType // key
      ShopItems : ItemType list }

type [<NoEquality; NoComparison>] EnemyDescriptor =
    { EnemyType : EnemyType
      EnemyPosition : Vector2 }

type [<NoEquality; NoComparison>] BattleData =
    { BattleType : BattleType // key
      BattleAllyPositions : Vector2 list
      BattleEnemies : EnemyType list
      BattleTileMap : TileMap AssetTag
      BattleTileIndexOffset : int
      BattleTileIndexOffsetRange : int * int
      BattleSongOpt : Song AssetTag option }

type [<NoEquality; NoComparison>] EncounterData =
    { EncounterType : EncounterType // key
      BattleTypes : BattleType list }

type [<NoEquality; NoComparison>] CharacterData =
    { CharacterType : CharacterType // key
      ArchetypeType : ArchetypeType
      LevelBase : int
      AnimationSheet : Image AssetTag
      PortraitOpt : Image AssetTag option
      WeaponOpt : WeaponType option
      ArmorOpt : ArmorType option
      Accessories : AccessoryType list
      TechProbabilityOpt : single option
      GoldScalar : single
      ExpScalar : single
      Description : string }

type [<NoEquality; NoComparison>] CharacterAnimationData =
    { CharacterAnimationType : CharacterAnimationType // key
      AnimationType : AnimationType
      LengthOpt : int64 option
      Run : int
      Delay : int64
      Offset : Vector2i }

type [<ReferenceEquality; NoComparison>] PropData =
    | Portal of PortalType * PortalIndex * Direction * FieldType * PortalIndex * bool * Advent Set // leads to a different portal
    | Door of DoorType * KeyItemType option * Cue * Cue * Advent Set // for simplicity, we just have north / south doors
    | Chest of ChestType * ItemType * Guid * BattleType option * Cue * Advent Set
    | Switch of SwitchType * Cue * Cue * Advent Set
    | Sensor of SensorType * BodyShape option * Cue * Cue * Advent Set
    | Character of CharacterType * Direction * bool * Cue * Advent Set
    | Npc of NpcType * Direction * Cue * Advent Set
    | NpcBranching of NpcType * Direction * Branch list * Advent Set
    | Shopkeep of ShopkeepType * Direction * ShopType * Advent Set
    | Seal of Color * Cue * Advent Set
    | Flame of FlameType * bool
    | SavePoint
    | ChestSpawn
    | EmptyProp

type [<NoEquality; NoComparison>] PropDescriptor =
    { PropBounds : Vector4
      PropElevation : single
      PropData : PropData
      PropId : int }

type [<NoEquality; NoComparison>] FieldTileMap =
    | FieldStatic of TileMap AssetTag
    | FieldConnector of TileMap AssetTag * TileMap AssetTag
    | FieldRandom of int * single * OriginRand * int * string

type [<NoEquality; NoComparison>] FieldData =
    { FieldType : FieldType // key
      FieldTileMap : FieldTileMap
      FieldTileIndexOffset : int
      FieldTileIndexOffsetRange : int * int
      FieldBackgroundColor : Color
      FieldDebugAdvents : Advent Set
      FieldDebugKeyItems : KeyItemType list
      FieldSongOpt : Song AssetTag option
      EncounterTypeOpt : EncounterType option
      Definitions : CueDefinitions
      Treasures : ItemType list }

[<RequireQualifiedAccess>]
module FieldData =

    let mutable tileMapsMemoized = Map.empty<uint64 * FieldType, Choice<TmxMap, TmxMap * TmxMap, TmxMap * Origin>>
    let mutable propObjectsMemoized = Map.empty<uint64 * FieldType, (TmxMap * TmxObjectGroup * TmxObject) list>
    let mutable propDescriptorsMemoized = Map.empty<uint64 * FieldType, PropDescriptor list>

    let objectToPropOpt (object : TmxObject) (group : TmxObjectGroup) (tileMap : TmxMap) =
        let propPosition = v2 (single object.X) (single tileMap.Height * single tileMap.TileHeight - single object.Y) // invert y
        let propSize = v2 (single object.Width) (single object.Height)
        let propBounds = v4Bounds propPosition propSize
        let propElevation =
            match group.Properties.TryGetValue Constants.TileMap.ElevationPropertyName with
            | (true, elevationStr) -> Constants.Field.ForegroundElevation + scvalue elevationStr
            | (false, _) -> Constants.Field.ForegroundElevation
        match object.Properties.TryGetValue Constants.TileMap.InfoPropertyName with
        | (true, propDataStr) ->
            let propData = scvalue propDataStr
            Some { PropBounds = propBounds; PropElevation = propElevation; PropData = propData; PropId = object.Id }
        | (false, _) -> None

    let inflateProp prop (treasures : ItemType FStack) rand =
        match prop.PropData with
        | ChestSpawn ->
            let (probability, rand) = Rand.nextSingleUnder 1.0f rand
            if probability < Constants.Field.TreasureProbability then
                let (treasure, treasures, rand) =
                    if FStack.notEmpty treasures then
                        let (index, rand) = Rand.nextIntUnder (FStack.length treasures) rand
                        (FStack.index index treasures, FStack.removeAt index treasures, rand)
                    else (Consumable GreenHerb, treasures, rand)
                let (id, rand) = let (i, rand) = Rand.nextInt rand in let (j, rand) = Rand.nextInt rand in (Gen.idFromInts i j, rand)
                let prop = { prop with PropData = Chest (WoodenChest, treasure, id, None, Cue.Nil, Set.empty) }
                (prop, treasures, rand)
            else ({ prop with PropData = EmptyProp }, treasures, rand)
        | _ -> (prop, treasures, rand)

    let tryGetTileMap omniSeedState fieldData world =
        let rotatedSeedState = OmniSeedState.rotate false fieldData.FieldType omniSeedState
        let memoKey = (rotatedSeedState, fieldData.FieldType)
        match Map.tryFind memoKey tileMapsMemoized with
        | None ->
            let tileMapOpt =
                match fieldData.FieldTileMap with
                | FieldStatic fieldAsset ->
                    match World.tryGetTileMapMetadata fieldAsset world with
                    | Some (_, _, tileMap) -> Some (Choice1Of3 tileMap)
                    | None -> None
                | FieldConnector (fieldAsset, fieldFadeAsset) ->
                    match (World.tryGetTileMapMetadata fieldAsset world, World.tryGetTileMapMetadata fieldFadeAsset world) with
                    | (Some (_, _, tileMap), Some (_, _, tileMapFade)) -> Some (Choice2Of3 (tileMap, tileMapFade))
                    | (_, _) -> None
                | FieldRandom (walkLength, bias, originRand, floor, fieldPath) ->
                    let rand = Rand.makeFromSeedState rotatedSeedState
                    let (origin, rand) = OriginRand.toOrigin originRand rand
                    let (cursor, mapRand, _) = MapRand.makeFromRand walkLength bias Constants.Field.MapRandSize origin floor rand
                    let fieldName = FieldType.toFieldName fieldData.FieldType
                    let mapTmx = MapRand.toTmx fieldName fieldPath origin cursor floor mapRand
                    Some (Choice3Of3 (mapTmx, origin))
            match tileMapOpt with
            | Some tileMapChc -> tileMapsMemoized <- Map.add memoKey tileMapChc tileMapsMemoized
            | None -> ()
            tileMapOpt
        | tileMapOpt -> tileMapOpt

    let getPropObjects omniSeedState fieldData world =
        let rotatedSeedState = OmniSeedState.rotate false fieldData.FieldType omniSeedState
        let memoKey = (rotatedSeedState, fieldData.FieldType)
        match Map.tryFind memoKey propObjectsMemoized with
        | None ->
            let propObjects =
                match tryGetTileMap omniSeedState fieldData world with
                | Some tileMapChc ->
                    match tileMapChc with
                    | Choice1Of3 tileMap
                    | Choice2Of3 (tileMap, _)
                    | Choice3Of3 (tileMap, _) ->
                        if tileMap.ObjectGroups.Contains Constants.Field.PropsGroupName then
                            let group = tileMap.ObjectGroups.Item Constants.Field.PropsGroupName
                            enumerable<TmxObject> group.Objects |> Seq.map (fun propObject -> (tileMap, group, propObject)) |> Seq.toList
                        else []
                | None -> []
            propObjectsMemoized <- Map.add memoKey propObjects propObjectsMemoized
            propObjects
        | Some propObjects -> propObjects

    let getPropDescriptors omniSeedState fieldData world =
        let rotatedSeedState = OmniSeedState.rotate false fieldData.FieldType omniSeedState
        let memoKey = (rotatedSeedState, fieldData.FieldType)
        match Map.tryFind memoKey propDescriptorsMemoized with
        | None ->
            let rand = Rand.makeFromSeedState rotatedSeedState
            let propObjects = getPropObjects omniSeedState fieldData world
            let propsUninflated = List.choose (fun (tileMap, group, object) -> objectToPropOpt object group tileMap) propObjects
            let (propsRandomized, rand) = Rand.nextPermutation propsUninflated rand
            let (propDescriptors, _, _) =
                List.foldBack (fun prop (propDescriptors, treasures, rand) ->
                    let (propDescriptor, treasures, rand) = inflateProp prop treasures rand
                    let treasures = if FStack.isEmpty treasures then FStack.ofSeq fieldData.Treasures else treasures
                    (propDescriptor :: propDescriptors, treasures, rand))
                    propsRandomized
                    ([], FStack.ofSeq fieldData.Treasures, rand)
            propDescriptorsMemoized <- Map.add memoKey propDescriptors propDescriptorsMemoized
            propDescriptors
        | Some propDescriptors -> propDescriptors

    let getPortals omniSeedState fieldData world =
        let propDescriptors = getPropDescriptors omniSeedState fieldData world
        List.filter (fun propDescriptor -> match propDescriptor.PropData with Portal _ -> true | _ -> false) propDescriptors

    let tryGetPortal omniSeedState portalIndex fieldData world =
        let portals = getPortals omniSeedState fieldData world
        List.tryFind (fun prop -> match prop.PropData with Portal (_, portalIndex2, _, _, _, _, _) -> portalIndex2 = portalIndex | _ -> failwithumf ()) portals

    let tryGetSpiritType omniSeedState avatarBottom fieldData world =
        match tryGetTileMap omniSeedState fieldData world with
        | Some tileMapChc ->
            match tileMapChc with
            | Choice3Of3 (tileMap, origin) ->
                match fieldData.FieldTileMap with
                | FieldRandom (walkLength, _, _, _, _) ->
                    let tileMapBounds = v4Bounds v2Zero (v2 (single tileMap.Width * single tileMap.TileWidth) (single tileMap.Height * single tileMap.TileHeight))
                    let distanceFromOriginMax =
                        let walkRatio = single walkLength * Constants.Field.WalkLengthScalar
                        let tileMapBoundsScaled = tileMapBounds.Scale (v2Dup walkRatio)
                        let delta = tileMapBoundsScaled.Bottom - tileMapBoundsScaled.Top
                        delta.Length ()
                    let distanceFromOrigin =
                        match origin with
                        | OriginC -> let delta = avatarBottom - tileMapBounds.Center in delta.Length ()
                        | OriginN -> let delta = avatarBottom - tileMapBounds.Top in delta.Length ()
                        | OriginE -> let delta = avatarBottom - tileMapBounds.Right in delta.Length ()
                        | OriginS -> let delta = avatarBottom - tileMapBounds.Bottom in delta.Length ()
                        | OriginW -> let delta = avatarBottom - tileMapBounds.Left in delta.Length ()
                        | OriginNE -> let delta = avatarBottom - tileMapBounds.TopRight in delta.Length ()
                        | OriginNW -> let delta = avatarBottom - tileMapBounds.TopLeft in delta.Length ()
                        | OriginSE -> let delta = avatarBottom - tileMapBounds.BottomRight in delta.Length ()
                        | OriginSW -> let delta = avatarBottom - tileMapBounds.BottomLeft in delta.Length ()
                    let battleIndex = int (3.0f / distanceFromOriginMax * distanceFromOrigin)
                    match battleIndex with
                    | 0 -> Some WeakSpirit
                    | 1 -> Some NormalSpirit
                    | _ -> Some StrongSpirit
                | FieldStatic _ | FieldConnector _ -> None
            | Choice1Of3 _ -> None
            | Choice2Of3 _ -> None
        | None -> None

[<RequireQualifiedAccess>]
module Data =

    type [<NoEquality; NoComparison>] OmniData =
        { Weapons : Map<WeaponType, WeaponData>
          Armors : Map<ArmorType, ArmorData>
          Accessories : Map<AccessoryType, AccessoryData>
          Consumables : Map<ConsumableType, ConsumableData>
          Techs : Map<TechType, TechData>
          Archetypes : Map<ArchetypeType, ArchetypeData>
          Characters : Map<CharacterType, CharacterData>
          Shops : Map<ShopType, ShopData>
          Battles : Map<BattleType, BattleData>
          Encounters : Map<EncounterType, EncounterData>
          TechAnimations : Map<TechType, TechAnimationData>
          CharacterAnimations : Map<CharacterAnimationType, CharacterAnimationData>
          Fields : Map<FieldType, FieldData> }

    let private readSheet<'d, 'k when 'k : comparison> filePath (getKey : 'd -> 'k) =
        Math.init () // HACK: initializing Math type converters for required type converters in fsx script.
        let text = File.ReadAllText filePath
        let symbol = flip (Symbol.ofStringCsv true) (Some filePath) text
        let value = symbolToValue<'d list> symbol
        Map.ofListBy (fun data -> getKey data, data) value

    let private readFromFiles () =
        { Weapons = readSheet Assets.Data.WeaponDataFilePath (fun data -> data.WeaponType)
          Armors = readSheet Assets.Data.ArmorDataFilePath (fun data -> data.ArmorType)
          Accessories = readSheet Assets.Data.AccessoryDataFilePath (fun data -> data.AccessoryType)
          Consumables = readSheet Assets.Data.ConsumableDataFilePath (fun data -> data.ConsumableType)
          Techs = readSheet Assets.Data.TechDataFilePath (fun data -> data.TechType)
          Archetypes = readSheet Assets.Data.ArchetypeDataFilePath (fun data -> data.ArchetypeType)
          Characters = readSheet Assets.Data.CharacterDataFilePath (fun data -> data.CharacterType)
          Shops = readSheet Assets.Data.ShopDataFilePath (fun data -> data.ShopType)
          Battles = readSheet Assets.Data.BattleDataFilePath (fun data -> data.BattleType)
          Encounters = readSheet Assets.Data.EncounterDataFilePath (fun data -> data.EncounterType)
          TechAnimations = readSheet Assets.Data.TechAnimationDataFilePath (fun data -> data.TechType)
          CharacterAnimations = readSheet Assets.Data.CharacterAnimationDataFilePath (fun data -> data.CharacterAnimationType)
          Fields = readSheet Assets.Data.FieldDataFilePath (fun data -> data.FieldType) }

    let Value =
        readFromFiles ()