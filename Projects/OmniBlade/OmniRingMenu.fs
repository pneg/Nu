﻿namespace OmniBlade
open System
open Prime
open Nu
open Nu.Declarative
open OmniBlade

[<AutoOpen>]
module OmniRingMenu =

    type [<NoComparison>] RingMenuCommand =
        | Cancel
        | ItemSelect of string
        | Arrange of Entity * int

    type Entity with
        
        member this.GetRadius = this.Get Property? Radius
        member this.SetRadius = this.Set Property? Radius
        member this.Radius = lens<single> Property? Radius this.GetRadius this.SetRadius this
        member this.GetRingMenuModel = this.GetModel<RingMenuModel>
        member this.SetRingMenuModel = this.SetModel<RingMenuModel>
        member this.RingMenuModel = this.Model<RingMenuModel> ()
        member this.ItemSelectEvent = Events.ItemSelect --> this
        member this.CancelEvent = Events.Cancel --> this

    type RingMenuDispatcher () =
        inherit GuiDispatcher<RingMenuModel, unit, RingMenuCommand> ({ Items = []; ItemCancelOpt = None; Dirt = Gen.id })

        override this.Command (model, command, menu, world) =
            match command with
            | Cancel -> just (World.publish () menu.CancelEvent [] menu world)
            | ItemSelect item -> just (World.publish item menu.ItemSelectEvent [] menu world)
            | Arrange (button, index) ->
                let itemCount = List.length model.Items
                let progress = single index / single itemCount
                let rotation = (progress * single Math.PI * 2.0f) + (menu.GetRotation world * single Math.PI * 2.0f)
                let radius = menu.GetRadius world
                let position = v2 (radius * sin rotation) (radius * -cos rotation)
                let world = button.SetPositionLocal position world
                just world

        static member Properties =
            [define Entity.Radius 128.0f
             define Entity.Rotation 0.0f
             define Entity.SwallowMouseLeft false
             define Entity.Visible false]

        override this.Content (model, menu, _) =
            [Content.entities (model --> fun model -> model.Items) $ fun index item layer world ->
                let itemValue = item.Get world
                let buttonName = menu.Name + "+" + itemValue
                let button = layer / buttonName
                Content.button buttonName
                    [Entity.Size == v2 64.0f 64.0f
                     Entity.Depth <== menu.Depth
                     Entity.UpImage == asset Assets.BattlePackage (itemValue + "Up")
                     Entity.DownImage == asset Assets.BattlePackage (itemValue + "Down")
                     Entity.Persistent == false
                     Entity.ChangeEvent Property? Radius ==> cmd (Arrange (button, index))
                     Entity.ChangeEvent Property? Rotation ==> cmd (Arrange (button, index))
                     Entity.ChangeEvent Property? RingMenuModel ==> cmd (Arrange (button, index))]
             Content.entityOpt (model --> fun model -> model.ItemCancelOpt) $ fun itemCancel _ world ->
                let itemCancelValue = itemCancel.Get world
                Content.button (menu.Name + "+" + itemCancelValue)
                    [Entity.PositionLocal == v2 0.0f -48.0f
                     Entity.Size == v2 64.0f 64.0f
                     Entity.Depth <== menu.Depth
                     Entity.UpImage == asset Assets.BattlePackage (itemCancelValue + "Up")
                     Entity.DownImage == asset Assets.BattlePackage (itemCancelValue + "Down")
                     Entity.Persistent == false]]