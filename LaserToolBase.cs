using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Cheetah.LaserTools
{
    public abstract partial class LaserToolBase : MyGameLogicComponent
    {
        void Work(int ticks = 1)
        {
            if (IsDrill) return;

            LineD WeldRay = new LineD(BeamCtlModule.BeamStart, BeamCtlModule.BeamEnd);
            List<MyLineSegmentOverlapResult<MyEntity>> Overlaps = new List<MyLineSegmentOverlapResult<MyEntity>>();
            MyGamePruningStructure.GetTopmostEntitiesOverlappingRay(ref WeldRay, Overlaps);

            HashSet<IMyCubeGrid> Grids = new HashSet<IMyCubeGrid>();
            HashSet<IMyCharacter> Characters = new HashSet<IMyCharacter>();
            HashSet<IMyFloatingObject> Flobjes = new HashSet<IMyFloatingObject>();
            Overlaps.Select(x => x.Element as IMyEntity).SortByType(Grids, Characters, Flobjes);
            Grids.Remove(ToolGrid);

            if (SessionCore.Settings.Debug && Vector3D.Distance(Tool.GetPosition(), MyAPIGateway.Session.LocalHumanPlayer.GetPosition()) <= 200)
            {
                string GridNames = "";
                foreach (var grid in Grids)
                {
                    GridNames += $"{grid.DisplayName};";
                }
                DebugNote.Text = $"{Tool.CustomName}: processing {Grids.Count} entities: {GridNames}{(IsWelder ? $" sup.invs: {GridInventoryModule.GetAccessibleInventories(Tool).Count}; unbuilt: {(this as LaserWelder).UnbuiltBlocks.Count}" : "")}";
                GridNames = null;
            }

            foreach (IMyCubeGrid Grid in Grids)
            {
                //if (Grid.EntityId == ToolGrid.EntityId) continue;
                try
                {
                    ProcessGrid(Grid, ticks);
                }
                catch (Exception Scrap)
                {
                    SessionCore.LogError(Grid.DisplayName, Scrap);
                }
            }

            if (MyAPIGateway.Session.IsServer)
            {
                foreach (IMyCharacter Char in Characters)
                {
                    if (Char.WorldAABB.Intersects(ref WeldRay))
                        Char.DoDamage(VanillaToolConstants.GrinderSpeed * ticks / 2, MyDamageType.Grind, true, null, Tool.EntityId);
                }

                foreach (IMyFloatingObject Flobj in Flobjes)
                {
                    if (CargoFillRatio < 0.75)
                        ToolCargo.PickupItem(Flobj);
                    else break;
                }
            }
        }

        protected abstract void ProcessGrid(IMyCubeGrid TargetGrid, int ticks);
        protected virtual void ProcessVoxel(MyVoxelBase Voxel, int ticks) { }

        void Main(int ticks = 0)
        {
            try
            {
                if (ticks == 0) ticks = SessionCore.WorkSkipTicks;
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                if (Tool.IsToolWorking()) watch.Start();
                try
                {
                    if (IsWelder && LocalPlayerIsOwner && HUDModule.MessageExpired) MissingHUD.Hide();
                    PowerModule.SetPowerUsage();

                    if (Tool.IsToolWorking() && PowerModule.HasEnoughPower)
                    {
                        Work(ticks);
                        BeamCtlModule.DrawBeam();
                    }
                    else if (!PowerModule.HasEnoughPower)
                    {
                        DebugNote.Text = $"{Tool.CustomName}: not enough power";
                    }
                    else
                    {
                        DebugNote.Text = $"{Tool.CustomName}: idle";
                        //UnbuiltBlocks.Clear();
                    }
                    Tool.RefreshCustomInfo();
                    //if (SessionCore.Debug) DebugNote.Text = $"{Tool.CustomName} perf. impact: {(RunTimesAvailable ? Math.Round(AvgRunTime, 5).ToString() : "--")}/{(RunTimesAvailable ? Math.Round(MaxRunTime, 5).ToString() : "--")} ms (avg/max)";
                }
                catch (Exception Scrap)
                {
                    SessionCore.LogError($"{Tool.CustomName}.Main().Work()", Scrap);
                }
                if (Tool.IsToolWorking())
                {
                    watch.Stop();
                    watch.Report(Tool.CustomName, "Main()");
                }
            }
            catch (Exception Scrap)
            {
                SessionCore.LogError($"{Tool.CustomName}.Main()", Scrap);
            }
        }

        void Aux(int ticks)
        {
            if (Tool.IsToolWorking() && PowerModule.HasEnoughPower)
            {
                BeamCtlModule.DrawBeam();
            }
        }

        public override void UpdateBeforeSimulation()
        {
            Aux(ticks: 1);
            Ticks += 1;
            if (Ticks >= SessionCore.WorkSkipTicks)
            {
                Ticks = 0;
                if (SessionCore.Settings.AllowAsyncWelding)
                {
                    ParallelTasks.WorkOptions opt = new ParallelTasks.WorkOptions();
                    //opt.DetachFromParent = true;
                    opt.MaximumThreads = 1;
                    opt.QueueFIFO = true;
                    MyAPIGateway.Parallel.Start(CallMain, opt);
                }
                else
                {
                    Main(SessionCore.WorkSkipTicks);
                }
            }
        }

        void CallMain()
        {
            Main(SessionCore.WorkSkipTicks);
        }

        public override void UpdateBeforeSimulation100()
        {
            PowerModule.UpdateAvailablePower();
        }
    }
}