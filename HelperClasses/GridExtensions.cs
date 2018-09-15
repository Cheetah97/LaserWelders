using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;

namespace Cheetah.LaserTools
{
    public static class GridExtensions
    {
       public static void GetBlocks(this IMyCubeGrid Grid, HashSet<IMySlimBlock> blocks, Func<IMySlimBlock, bool> collect = null)
        {
            List<IMySlimBlock> cubes = new List<IMySlimBlock>();
            if (blocks == null) blocks = new HashSet<IMySlimBlock>(); else blocks.Clear();
            Grid.GetBlocks(cubes, collect);
            foreach (var block in cubes)
                blocks.Add(block);
        }

        public static Vector3D GetPosition(this IMySlimBlock block)
        {
            return block.CubeGrid.GridIntegerToWorld(block.Position);
        }

        public static float GetBaseMass(this IMyCubeGrid Grid)
        {
            float baseMass, totalMass;
            (Grid as MyCubeGrid).GetCurrentMass(out baseMass, out totalMass);
            return baseMass;
        }

        public static int GetTotalMass(this IMyCubeGrid Grid)
        {
            return (Grid as MyCubeGrid).GetCurrentMass();
        }

        public static float GetMaxPowerOutput(this IMyCubeGrid Grid)
        {
            return Grid.GetMaxReactorPowerOutput() + Grid.GetMaxBatteryPowerOutput();
        }

        public static bool HasPower(this IMyCubeGrid Grid)
        {
            foreach (IMySlimBlock Reactor in Grid.GetWorkingBlocks<IMyReactor>())
            {
                if (Reactor != null && Reactor.FatBlock.IsWorking) return true;
            }
            foreach (IMySlimBlock Battery in Grid.GetWorkingBlocks<IMyBatteryBlock>())
            {
                if ((Battery as IMyBatteryBlock).CurrentStoredPower > 0f) return true;
            }

            return false;
        }

        public static float GetCurrentReactorPowerOutput(this IMyCubeGrid Grid)
        {
            List<IMyReactor> Reactors = new List<IMyReactor>();
            Grid.GetTerminalSystem().GetBlocksOfType(Reactors, x => x.IsWorking);
            if (Reactors.Count == 0) return 0;

            float SummarizedOutput = 0;
            foreach (var Reactor in Reactors)
                SummarizedOutput += Reactor.CurrentOutput;

            return SummarizedOutput;
        }

        public static float GetMaxReactorPowerOutput(this IMyCubeGrid Grid)
        {
            List<IMyReactor> Reactors = new List<IMyReactor>();
            Grid.GetTerminalSystem().GetBlocksOfType(Reactors, x => x.IsWorking);
            if (Reactors.Count == 0) return 0;

            float SummarizedOutput = 0;
            foreach (var Reactor in Reactors)
                SummarizedOutput += Reactor.MaxOutput;

            return SummarizedOutput;
        }

        public static float GetMaxBatteryPowerOutput(this IMyCubeGrid Grid)
        {
            List<IMyBatteryBlock> Batteries = new List<IMyBatteryBlock>();
            Grid.GetTerminalSystem().GetBlocksOfType(Batteries, x => x.IsWorking && x.HasCapacityRemaining);
            if (Batteries.Count == 0) return 0;

            float SummarizedOutput = 0;
            foreach (var Battery in Batteries)
                SummarizedOutput += Battery.MaxOutput();

            return SummarizedOutput;
        }

        public static float MaxOutput(this IMyBatteryBlock Battery)
        {
            return (MyDefinitionManager.Static.GetCubeBlockDefinition(Battery.BlockDefinition) as MyBatteryBlockDefinition).MaxPowerOutput;
        }

        public static bool HasCockpit(this IMyCubeGrid Grid)
        {
            return Grid.GetWorkingBlocks<IMyCockpit>().Count > 0;
        }

        public static bool HasGyros(this IMyCubeGrid Grid)
        {
            return Grid.GetWorkingBlocks<IMyGyro>().Count > 0;
        }

        public static IMyCockpit GetFirstCockpit(this IMyCubeGrid Grid)
        {
            return Grid.GetWorkingBlocks<IMyCockpit>()[0];
        }

        public static bool Has<T>(this IMyCubeGrid Grid) where T : class, IMyTerminalBlock
        {
            return Grid.GetWorkingBlocks<T>().Count > 0;
        }
    }
}
