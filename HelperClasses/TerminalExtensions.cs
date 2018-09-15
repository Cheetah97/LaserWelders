using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace Cheetah.LaserTools
{
    public static class TerminalExtensions
    {
        public static IMyGridTerminalSystem GetTerminalSystem(this IMyCubeGrid Grid)
        {
            return MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);
        }

        public static List<T> GetBlocksOfType<T>(this IMyGridTerminalSystem Term, Func<T, bool> collect = null) where T : class, Sandbox.ModAPI.Ingame.IMyTerminalBlock
        {
            if (Term == null) throw new Exception("GridTerminalSystem is null!");
            List<T> TermBlocks = new List<T>();
            Term.GetBlocksOfType(TermBlocks, collect);
            return TermBlocks;
        }

        public static List<T> GetWorkingBlocks<T>(this IMyCubeGrid Grid, bool OverrideEnabledCheck = false, Func<T, bool> collect = null) where T : class, IMyTerminalBlock
        {
            try
            {
                List<IMySlimBlock> slimBlocks = new List<IMySlimBlock>();
                List<T> Blocks = new List<T>();
                Grid.GetBlocks(slimBlocks, (x) => x != null && x is T && (!OverrideEnabledCheck ? (x as IMyTerminalBlock).IsWorking : (x as IMyTerminalBlock).IsFunctional));

                if (slimBlocks.Count == 0) return new List<T>();
                foreach (var _block in slimBlocks)
                    if (collect == null || collect(_block as T)) Blocks.Add(_block as T);

                return Blocks;
            }
            catch (Exception Scrap)
            {
                Grid.LogError("GridExtensions.GetWorkingBlocks", Scrap);
                return new List<T>();
            }
        }

        public static Dictionary<string, int> CalculateMissingComponents(this IMyCubeGrid Grid)
        {
            if (Grid == null) return new Dictionary<string, int>();
            try
            {
                Dictionary<string, int> MissingComponents = new Dictionary<string, int>();
                List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
                Grid.GetBlocks(Blocks);

                foreach (IMySlimBlock Block in Blocks)
                {
                    try
                    {
                        Block.ReadMissingComponents(MissingComponents);
                    }
                    catch (Exception Scrap)
                    {
                        SessionCore.LogError($"CalculateMissing[{Grid.CustomName}].Iterate", Scrap, DebugPrefix: "LaserWelders.");
                    }
                }
                return MissingComponents;
            }
            catch (Exception Scrap)
            {
                SessionCore.LogError($"CalculateMissing[{Grid.CustomName}]", Scrap, DebugPrefix: "LaserWelders.");
                return new Dictionary<string, int>();
            }
        }

        public static void Trigger(this SpaceEngineers.Game.ModAPI.IMyTimerBlock Timer)
        {
            Timer.GetActionWithName("TriggerNow").Apply(Timer);
        }
    }

}
