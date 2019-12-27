﻿namespace OmniBlade
open System
open Prime
open Nu
open OmniBlade

[<RequireQualifiedAccess>]
module Constants =

    [<RequireQualifiedAccess>]
    module Audio =

        let MasterSongVolume = 0.0f

    [<RequireQualifiedAccess>]
    module Battle =

        let ActionTime = 999
        let ActionTimeInc = 3

    [<RequireQualifiedAccess>]
    module OmniBlade =

        let DissolveData =
            { IncomingTime = 20L
              OutgoingTime = 30L
              DissolveImage = AssetTag.make<Image> Assets.GuiPackage "Dissolve" }
    
        let SplashData =
            { DissolveData = DissolveData
              IdlingTime = 60L
              SplashImage = AssetTag.make<Image> Assets.GuiPackage "Nu" }