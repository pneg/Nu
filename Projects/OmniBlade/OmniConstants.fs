﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace OmniBlade
open Prime
open Nu
open OmniBlade

[<RequireQualifiedAccess>]
module Constants =

    [<RequireQualifiedAccess>]
    module Gui =

        let Dissolve =
            { IncomingTime = 40L
              OutgoingTime = 60L
              DissolveImage = Assets.Default.Image8 }

        let Splash =
            { DissolveDescriptor = Constants.Dissolve.Default
              IdlingTime = 60L
              SplashImageOpt = Some Assets.Gui.Splash }

    [<RequireQualifiedAccess>]
    module Intro =

        let Dissolve =
            { IncomingTime = 90L
              OutgoingTime = 90L
              DissolveImage = Assets.Default.Image8 }

        let Splash =
            { DissolveDescriptor = Constants.Dissolve.Default
              IdlingTime = 160L
              SplashImageOpt = None }

    [<RequireQualifiedAccess>]
    module Gameplay =

        let TileSize = v2 48.0f 48.0f
        let CharacterSize = v2 144.0f 144.0f
        let DialogSplit = '^'
        let ItemLimit = 9

    [<RequireQualifiedAccess>]
    module Field =

#if DEV
        let WalkForce = 17000.0f
#else
        let WalkForce = 8500.0f
#endif
        let LinearDamping = 19.0f
        let PropsGroupName = "Props"
        let TransitionTime = 60L
        let MapRandSize = v2iDup 7
        let AvatarBottomInset = v2 0.0f 24.0f
        let SpiritMovementDuration = 60L
        let SpiritWalkSpeed = 1.75f
        let SpiritRunSpeed = 3.5f
        let SpiritOrbSize = v2Dup 192.0f
        let SpiritOrbRadius = 90.0f
        let SpiritOrbRatio = 0.2f
        let SpiritOrbBlipSize = v2Dup 21.0f
        let SpiritActivityMinimum = 120L
        let SpiritActivityThreshold = 90L
        let SpiritRadius = SpiritOrbRadius / SpiritOrbRatio
        let TreasureProbability = 0.75f
        let RecruitmentFees = [|300; 1500; 7500; 37500|]
        let ConnectorFadeYMax = 1440.0f
        let BackgroundElevation = -10.0f
        let ForegroundElevation = 0.0f
        let EffectElevation = 10.0f
        let SpiritOrbElevation = 20.0f
        let GuiElevation = 30.0f
        let GuiEffectElevation = 40.0f

    [<RequireQualifiedAccess>]
    module Battle =

        let AllyMax = 3
        let ActionTime = 1000
        let AutoBattleReadyTime = 50
        let AutoBattleTechFrequency = 3
        let AllyActionTimeDelta = 4
        let EnemyActionTimeDelta = 3
        let DefendingDamageScalar = 0.5f
        let CancelPosition = v2 -438.0f -228.0f
        let StrikingDistance = 48.0f
        let CharacterCenterOffset = v2 0.0f -30.0f
        let CharacterCenterOffset2 = v2 0.0f -36.0f
        let CharacterCenterOffset3 = v2 0.0f 36.0f
        let CharacterBottomOffset = v2 0.0f -6.0f
        let CharacterBottomOffset2 = v2 0.0f -48.0f
        let CharacterBottomOffset3 = v2 0.0f 48.0f
        let CharacterOffset = v2 -120.0f 0.0f
        let CharacterPulseLength = 60L
        let RingMenuRadius = 78.0f
        let BackgroundElevation = -10.0f
        let ForegroundElevation = 0.0f
        let EffectElevation = 10.0f
        let GuiElevation = 20.0f
        let GuiEffectElevation = 30.0f