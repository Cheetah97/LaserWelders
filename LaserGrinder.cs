﻿using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace Cheetah.LaserTools
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipGrinder), false, "LargeShipLaserGrinder", "SmallShipLaserGrinder")]
    public class GrinderLogic : LaserToolBase
    {
        IMyShipGrinder Grinder => Tool as IMyShipGrinder;

        protected override void ProcessGrid(IMyCubeGrid TargetGrid, int ticks)
        {
            if (!MyAPIGateway.Multiplayer.IsServer) return;
            if (TargetGrid.EntityId == ToolGrid.EntityId) return;
            List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
            List<LineD> RayGrid = new List<LineD> { new LineD(BeamCtlModule.BeamStart, BeamCtlModule.BeamEnd) };
            if (!TermModule.DistanceMode)
            {
                Vector3D UpOffset = Vector3D.Normalize(Tool.WorldMatrix.Up) * 0.5;
                Vector3D RightOffset = Vector3D.Normalize(Tool.WorldMatrix.Right) * 0.5;
                RayGrid = VectorExtensions.BuildLineGrid(BeamCtlModule.BeamStart, BeamCtlModule.BeamEnd, UpOffset, RightOffset, 2, 2);
            }

            TargetGrid.GetBlocksOnRay(RayGrid, Blocks, x => x.IsGrindable());
            Grind(Blocks, ticks);
        }

        void Grind(ICollection<IMySlimBlock> Blocks, int ticks = 1)
        { 
            if (Blocks.Count == 0) return;
            if (TermModule.DistanceMode) Blocks = Blocks.OrderBy(x => Vector3D.DistanceSquared(x.GetPosition(), Tool.GetPosition())).ToList();
            float SpeedRatio = VanillaToolConstants.GrinderSpeed / (TermModule.DistanceMode ? 1 : Blocks.Count) * ticks * TermModule.SpeedMultiplier;
            foreach (IMySlimBlock Block in Blocks)
            {
                Grind(Block, SpeedRatio);
                if (TermModule.DistanceMode) break;
            }
        }

        void Grind(IMySlimBlock Block, float SpeedRatio)
        {
            Block.DecreaseMountLevel(SpeedRatio, ToolCargo, useDefaultDeconstructEfficiency: true);
            Block.MoveItemsFromConstructionStockpile(ToolCargo);
            Block.DoDamage(0, VRage.Utils.MyStringHash.GetOrCompute("Grind"), true, null, Tool.EntityId);
            
            if (Block.FatBlock?.IsFunctional == false && Block.FatBlock?.HasInventory == true)
            {
                foreach (var Inventory in Block.FatBlock.GetInventories())
                {
                    if (Inventory.CurrentVolume == VRage.MyFixedPoint.Zero) continue;
                    foreach (var Item in Inventory.GetItems())
                    {
                        var Amount = Inventory.ComputeAmountThatFits(Item);
                        ToolCargo.TransferItemFrom(Inventory, (int)Item.ItemId, null, null, Amount, false);
                    }
                }
            }
            if (Block.IsFullyDismounted) Block.CubeGrid.RazeBlock(Block.Position);
        }
    }
}
