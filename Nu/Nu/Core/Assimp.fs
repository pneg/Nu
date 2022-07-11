﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.Numerics
open Nu

/// Specifies how to interpret imported asset units.
type UnitType =
    | UnitMeters
    | UnitCentimeters

[<AutoOpen>]
module AssimpExtensions =

    /// Node extensions.
    type Assimp.Node with

        member this.ImportMatrix (unitType, m : Assimp.Matrix4x4) =
            let scalar = match unitType with UnitMeters -> 1.0f | UnitCentimeters -> 0.01f
            Matrix4x4
                (m.A1, m.B1, m.C1, m.D1,
                 m.A2, m.B2, m.C2, m.D2,
                 m.A3, m.B3, m.C3, m.D3,
                 m.A4 * scalar, m.B4 * scalar, m.C4 * scalar, m.D4)

        /// Collect all the child nodes of a node, including the node itself.
        member this.CollectNodes () =
            seq {
                yield this
                for child in this.Children do
                    yield! child.CollectNodes () }

        /// Collect all the child nodes and transforms of a node, including the node itself.
        member this.CollectNodesAndTransforms (unitType, parentTransform : Matrix4x4) =
            seq {
                let localTransform = this.ImportMatrix (unitType, this.Transform)
                let worldTransform = localTransform * parentTransform
                yield (this, worldTransform)
                for child in this.Children do
                    yield! child.CollectNodesAndTransforms (unitType, worldTransform) }

        /// Map to a TreeNode.
        member this.Map<'a> (unitType, parentTransform : Matrix4x4, mapper : Assimp.Node -> Matrix4x4 -> 'a array TreeNode) : 'a array TreeNode =
            let localTransform = this.ImportMatrix (unitType, this.Transform)
            let worldTransform = localTransform * parentTransform
            let node = mapper this worldTransform
            for child in this.Children do
                let child = child.Map<'a> (unitType, worldTransform, mapper)
                node.Add child
            node
