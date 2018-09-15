using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace Cheetah.LaserTools
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipWelder), false, "LargeShipLaserWelder", "SmallShipLaserWelder")]
    public class LaserWelder : LaserToolBase
    {
        IMyShipWelder Welder => Tool as IMyShipWelder;
        public HashSet<IMySlimBlock> UnbuiltBlocks = new HashSet<IMySlimBlock>();

        protected override void ProcessGrid(IMyCubeGrid TargetGrid, int ticks)
        {
            List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
            List<LineD> RayGrid;
            
            if (!TermModule.DistanceMode)
            {
                Vector3D UpOffset = Vector3D.Normalize(Tool.WorldMatrix.Up) * 0.5;
                Vector3D RightOffset = Vector3D.Normalize(Tool.WorldMatrix.Right) * 0.5;
                RayGrid = VectorExtensions.BuildLineGrid(BeamCtlModule.BeamStart, BeamCtlModule.BeamEnd, UpOffset, RightOffset, SessionCore.Settings.WorkingZoneWidth, SessionCore.Settings.WorkingZoneWidth);
                if (TargetGrid.Physics?.Enabled == true)
                {
                    TargetGrid.GetBlocksOnRay(RayGrid, Blocks, x => x.IsWeldable());
                    if (SessionCore.Settings.Debug && SessionCore.Settings.DebugPerformance)
                    {
                        SessionCore.DebugAsync(Tool.CustomName, $"Welding {Blocks.CollapseDuplicates().Count} blocks", WriteOnlyIfDebug: true);
                    }
                    if (MyAPIGateway.Multiplayer.IsServer) Weld(Blocks, ticks);
                }
                else
                {
                    TargetGrid.GetBlocksOnRay(RayGrid, Blocks, x => x.IsProjectable());
                    if (Blocks.Count == 0) return;
                    SessionCore.DebugAsync(Tool.CustomName, $"Placing {Blocks.Count} blocks", WriteOnlyIfDebug: true);
                    try
                    {
                        Place(Blocks);
                    }
                    catch (Exception Scrap)
                    {
                        SessionCore.DebugAsync(Tool.CustomName, $"Place() crashed: {Scrap.Message}\n{Scrap.StackTrace}");
                    }
                }
            }
            else
            {
                RayGrid = new List<LineD> { new LineD(BeamCtlModule.BeamStart, BeamCtlModule.BeamEnd) };
                if (TargetGrid.Physics?.Enabled == true)
                {
                    TargetGrid.GetBlocksOnRay(RayGrid, Blocks, x => x.IsWeldable());
                    if (MyAPIGateway.Multiplayer.IsServer) Weld(Blocks, ticks);
                }
                else
                {
                    TargetGrid.GetBlocksOnRay(RayGrid, Blocks, x => x.IsProjectable());
                    Place(Blocks);
                }
            }
        }

        void Weld(ICollection<IMySlimBlock> Blocks, int ticks = 1)
        {
            UnbuiltBlocks.Clear();
            if (Blocks.Count == 0) return;
            float SpeedRatio = (VanillaToolConstants.WelderSpeed / Blocks.Count) * ticks * TermModule.SpeedMultiplier;
            float BoneFixSpeed = VanillaToolConstants.WelderBoneRepairSpeed * ticks;
            Dictionary<IMySlimBlock, int> UniqueBlocks = new Dictionary<IMySlimBlock, int>();
            if (!TermModule.DistanceMode) UniqueBlocks = Blocks.CollapseDuplicates();
            else
            {
                IMySlimBlock Block = Blocks.OrderByDescending(x => Vector3D.DistanceSquared(x.GetPosition(), Tool.GetPosition())).ToList().First();
                UniqueBlocks.Add(Block, 1);
                SpeedRatio = VanillaToolConstants.WelderSpeed * ticks * TermModule.SpeedMultiplier;
            }
            HashSet<IMySlimBlock> unbuilt = new HashSet<IMySlimBlock>();
            List<IMySlimBlock> CanBuild = new List<IMySlimBlock>();
            List<IMySlimBlock> CannotBuild = new List<IMySlimBlock>();

            if (MyAPIGateway.Session.CreativeMode)
            {
                foreach (IMySlimBlock Block in UniqueBlocks.Keys)
                {
                    float blockRatio = SpeedRatio * UniqueBlocks.GetData(Block);
                    Block.MoveItemsToConstructionStockpile(ToolCargo);
                    Block.IncreaseMountLevel(blockRatio, Welder.OwnerId, ToolCargo, BoneFixSpeed, Welder.HelpOthers);
                }
                return;
            }

            foreach (IMySlimBlock Block in UniqueBlocks.Keys)
            {
                if (Block.CanContinueBuild(ToolCargo)) CanBuild.Add(Block);
                else CannotBuild.Add(Block);
            }

            SessionCore.DebugWrite(Tool.CustomName, $"Can build {CanBuild.Count} blocks", WriteOnlyIfDebug: true);
            SessionCore.DebugWrite(Tool.CustomName, $"Can't build {CannotBuild.Count} blocks", WriteOnlyIfDebug: true);

            bool Pull = false;

            var Components = Reduce(CannotBuild.ReadMissingComponents());
            Dictionary<string, int> Pulled = new Dictionary<string, int>();
            lock (GridInventoryModule.InventoryLock)
            {
                if (Welder.UseConveyorSystem) GridInventoryModule.PullIn(Welder, Components, "Component");
                Pull = Pulled.Count > 0;
            }
            SessionCore.DebugWrite(Tool.CustomName, $"Pulled {Pulled.Values.Sum()} components", WriteOnlyIfDebug: true);

            System.Diagnostics.Stopwatch WelderWatch = new System.Diagnostics.Stopwatch();
            WelderWatch.Start();
            foreach (IMySlimBlock Block in UniqueBlocks.Keys)
            {
                if (!Pull && !CanBuild.Contains(Block))
                {
                    unbuilt.Add(Block);
                    continue;
                }
                float blockRatio = SpeedRatio * UniqueBlocks.GetData(Block);
                if (!Weld(Block, blockRatio, BoneFixSpeed)) unbuilt.Add(Block);
            }
            WelderWatch.Stop();
            WelderWatch.Report(Tool.CustomName, "Welding", UseAsync: true);

            System.Diagnostics.Stopwatch ReadMissingWatch = new System.Diagnostics.Stopwatch();
            ReadMissingWatch.Start();
            if (unbuilt.Count > 0)
            {
                Dictionary<string, int> Missing = new Dictionary<string, int>();
                unbuilt.ReadMissingComponents(Missing);
                Missing = Reduce(Missing);
                if (Missing.Count > 0) UnbuiltBlocks.UnionWith(unbuilt);
            }
            ComplainUnbuilt();
            ReadMissingWatch.Stop();
            ReadMissingWatch.Report(Tool.CustomName, "ReadMissing", true);
            SessionCore.DebugAsync(Tool.CustomName, $"Unbuilt: {unbuilt.Count} blocks", WriteOnlyIfDebug: true);
        }

        /// <summary>
        /// Removes components which are in the tool's cargo from pulling list.
        /// </summary>
        Dictionary<string, int> Reduce(Dictionary<string, int> ComponentList)
        {
            Dictionary<string, int> Reduced = ComponentList;

            VRage.Game.ModAPI.Ingame.IMyInventory MyInventory = ToolCargo;
            var InventoryList = MyInventory.GetItems();
            List<string> ToRemove = new List<string>();
            foreach (var Item in InventoryList)
            {
                if (ComponentList.ContainsKey(Item.Content.SubtypeName))
                {
                    Reduced[Item.Content.SubtypeName] -= (int)Item.Amount;
                    if (Reduced[Item.Content.SubtypeName] <= 0) ToRemove.Add(Item.Content.SubtypeName);
                }
            }

            Reduced.RemoveAll(ToRemove);

            return Reduced;
        }

        /*void WeldDistanceMode(ICollection<IMySlimBlock> Blocks, int ticks = 1)
        {
            UnbuiltBlocks.Clear();
            if (Blocks.Count == 0) return;
            Blocks = Blocks.OrderByDescending(x => Vector3D.DistanceSquared(x.GetPosition(), Tool.GetPosition())).ToList();
            float SpeedRatio = VanillaToolConstants.WelderSpeed * ticks * TermModule.SpeedMultiplier;
            float BoneFixSpeed = VanillaToolConstants.WelderBoneRepairSpeed * ticks;

            IMySlimBlock Block = Blocks.First();
            bool welded = Weld(Block, SpeedRatio, BoneFixSpeed);
            if (!welded)
            {
                var missing = Block.ReadMissingComponents();
                bool pull = false;
                lock (GridInventoryModule.InventoryLock)
                {
                    pull = Welder.UseConveyorSystem && ToolCargo.PullAny(OnboardInventoryOwners, missing);
                }
                if (!Welder.UseConveyorSystem || !pull)
                    UnbuiltBlocks.Add(Block);
            }
            ComplainUnbuilt();
        }
        */
        bool Weld(IMySlimBlock Block, float SpeedRatio, float BoneFixSpeed)
        {
            //if (Block.IsFullIntegrity && !Block.HasDeformation) return;
            if (Block.CanContinueBuild(ToolCargo))// || MyAPIGateway.Session.CreativeMode)
            { // Block should be welded in this order, or it won't work
                //SessionCore.DebugWrite(Tool.CustomName, $"Welding block...");
                Block.MoveItemsToConstructionStockpile(ToolCargo);
                Block.IncreaseMountLevel(SpeedRatio, Welder.OwnerId, ToolCargo, BoneFixSpeed, false);
                return true;
            }
            else if (Block.HasDeformation)
            {
                //SessionCore.DebugWrite(Tool.CustomName, $"Undeforming block...");
                Block.IncreaseMountLevel(SpeedRatio, Welder.OwnerId, ToolCargo, BoneFixSpeed, false);
                return true;
            }
            else
            {
                //SessionCore.DebugWrite(Tool.CustomName, $"Cannot continue building block");
                return false;
            }
        }

        void Place(ICollection<IMySlimBlock> _Blocks)
        {
            try
            {
                if (_Blocks.Count == 0) return;
                HashSet<IMySlimBlock> Blocks = new HashSet<IMySlimBlock>(_Blocks);
                HashSet<IMySlimBlock> unbuilt = new HashSet<IMySlimBlock>();

                var Projector = ((Blocks.First().CubeGrid as MyCubeGrid).Projector as IMyProjector);

                if (MyAPIGateway.Session.CreativeMode)
                {
                    foreach (IMySlimBlock Block in Blocks)
                        Projector.Build(Block, Tool.OwnerId, Tool.EntityId, false);
                    return;
                }

                Dictionary<string, int> ToPull = new Dictionary<string, int>();
                if (Welder.UseConveyorSystem)
                {
                    foreach (IMySlimBlock Block in Blocks)
                    {
                        var FirstItem = ((MyCubeBlockDefinition)Block.BlockDefinition).Components[0].Definition.Id;
                        if (!ToPull.ContainsKey(FirstItem.SubtypeName))
                        {
                            ToPull.Add(FirstItem.SubtypeName, 1);
                        }
                        else
                        {
                            ToPull[FirstItem.SubtypeName] += 1;
                        }
                    }

                    lock (GridInventoryModule.InventoryLock)
                    {
                        GridInventoryModule.PullIn(Welder, Reduce(ToPull), "Component");
                    }
                }

                foreach (IMySlimBlock Block in Blocks)
                {
                    var FirstItem = ((MyCubeBlockDefinition)Block.BlockDefinition).Components[0].Definition.Id;
                    if (ToolCargo.GetItemAmount(FirstItem) >= 1)
                    {
                        Projector.Build(Block, Tool.OwnerId, Tool.EntityId, false);
                        ToolCargo.RemoveItemsOfType(1, FirstItem);
                    }
                    else
                    {
                        unbuilt.Add(Block);
                    }
                }
                SessionCore.DebugAsync(Tool.CustomName, $"Place failed for {unbuilt.Count} blocks: can't pull first component", WriteOnlyIfDebug: true);
                UnbuiltBlocks.UnionWith(unbuilt);
                ComplainUnbuilt();
            }
            catch (Exception Scrap)
            {
                SessionCore.LogError($"{Tool.CustomName}.Place()", Scrap);
            }
        }

        void ComplainUnbuilt()
        {
            
            if (!UnbuiltBlocks.Any())
            {
                //SessionCore.DebugWrite(Tool.CustomName, $"ComplainUnbuilt() early exit - 0 unbuilt", WriteOnlyIfDebug: true);
                return;
            }

            //SessionCore.DebugWrite(Tool.CustomName, $"ComplainUnbuilt()");
            Dictionary<string, int> TotalMissingList = new Dictionary<string, int>();
            Dictionary<IMySlimBlock, Dictionary<string, int>> MissingPerBlock = new Dictionary<IMySlimBlock, Dictionary<string, int>>();
            UnbuiltBlocks.ReadMissingComponents(TotalMissingList, MissingPerBlock);
            //SessionCore.DebugWrite($"{Tool.CustomName}", $"Total missing: {TotalMissingList.Values.Sum()} components", WriteOnlyIfDebug: true);
            //SessionCore.DebugWrite($"{Tool.CustomName}", $"Pull failed");
            HUDModule.ComplainMissing(MissingPerBlock);
            UnbuiltBlocks.Clear();
        }
    }
}