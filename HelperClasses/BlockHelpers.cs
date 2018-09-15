using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;

namespace Cheetah.LaserTools
{
    public static class BlockHelpers
    {
        /// <summary>
        /// Removes "dead" block references from a block list.
        /// </summary>
        /// <param name="StrictCheck">Performs x.IsLive(Strict == true). Generates 2 object builders per every block in list.</param>
        public static void Purge<T>(this IList<T> Enum, bool StrictCheck = false) where T : IMySlimBlock
        {
            Enum = Enum.Where(x => x.IsLive(StrictCheck)).ToList();
        }

        /// <summary>
        /// Removes "dead" block references from a block list.
        /// </summary>
        /// <param name="StrictCheck">Performs x.IsLive(Strict == true). Generates 2 object builders per every block in list.</param>
        public static void PurgeInvalid<T>(this IList<T> Enum, bool StrictCheck = false) where T : IMyCubeBlock
        {
            Enum = Enum.Where(x => x.IsLive(StrictCheck)).ToList();
        }

        public static Dictionary<string, int> ReadMissingComponents(this IMySlimBlock Block)
        {
            Dictionary<string, int> MissingList = new Dictionary<string, int>();
            Block.ReadMissingComponents(MissingList);
            return MissingList;
        }

        public static void ReadMissingComponents(this IMySlimBlock Block, Dictionary<string, int> MissingList, bool ClearDictionary = false)
        {
            if (ClearDictionary) MissingList.Clear();
            if (Block.BuildIntegrity == Block.MaxIntegrity && Block.Integrity == Block.MaxIntegrity) return;
            Block.GetMissingComponents(MissingList);
            /*
            if (Block.StockpileAllocated)
            {
                if (SessionCore.Settings.Debug)
                {
                    Dictionary<string, int> missing = new Dictionary<string, int>();
                    Block.GetMissingComponents(missing);
                    if (missing.Count == 0)
                    {
                        SessionCore.DebugAsync($"BlockHelpers.ReadMissing", $"NO MISSING ACQUIRED: {Math.Round(Block.BuildIntegrity / Block.MaxIntegrity, 3) * 100}% build integrity, {Math.Round(Block.Integrity / Block.MaxIntegrity, 3) * 100}% damage integrity");
                    }
                }
                Block.GetMissingComponents(MissingList);
            }
            else
            {
                foreach (var Component in (Block.BlockDefinition as MyCubeBlockDefinition).Components)
                {
                    string Name = Component.Definition.Id.SubtypeName;
                    if (MissingList.ContainsKey(Name)) MissingList[Name] += Component.Count;
                    else MissingList.Add(Name, Component.Count);
                }
            }*/
        }

        public static Dictionary<string, int> ReadMissingComponents(this ICollection<IMySlimBlock> Blocks)
        {
            var Missing = new Dictionary<string, int>();
            foreach (IMySlimBlock Block in Blocks)
            {
                Block.ReadMissingComponents(Missing);
            }
            return Missing;
        }

        public static void ReadMissingComponents(this ICollection<IMySlimBlock> Blocks, Dictionary<string, int> MissingList, bool ClearDictionary = false)
        {
            if (ClearDictionary) MissingList.Clear();
            foreach (IMySlimBlock Block in Blocks)
            {
                Block.ReadMissingComponents(MissingList);
            }
        }

        public static void ReadMissingComponents(this ICollection<IMySlimBlock> Blocks, Dictionary<string, int> TotalMissingList, Dictionary<IMySlimBlock, Dictionary<string, int>> MissingPerBlock, bool ClearDictionary = false)
        {
            if (ClearDictionary) TotalMissingList.Clear();
            if (ClearDictionary) MissingPerBlock.Clear();
            foreach (IMySlimBlock Block in Blocks)
            {
                var Missing = Block.ReadMissingComponents();
                if (!MissingPerBlock.ContainsKey(Block)) MissingPerBlock.Add(Block, Missing);
                else MissingPerBlock[Block] = Missing;

                foreach (var kvp in Missing)
                {
                    if (TotalMissingList.ContainsKey(kvp.Key)) TotalMissingList[kvp.Key] += kvp.Value;
                    else TotalMissingList.Add(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Check if the given block is a "live" existing block, or a "zombie" reference left after a dead and removed block.
        /// </summary>
        /// <param name="StrictCheck">Performs strict check (checks if block in same place is of same typeid+subtypeid). Generates 2 object builders.</param>
        public static bool IsLive(this IMySlimBlock Block, bool StrictCheck = false)
        {
            if (Block == null) return false;
            if (Block.FatBlock != null && Block.FatBlock.Closed) return false;
            if (Block.IsDestroyed) return false;
            var ThereBlock = Block.CubeGrid.GetCubeBlock(Block.Position);
            if (ThereBlock == null) return false;
            var Builder = Block.GetObjectBuilder();
            var ThereBuilder = ThereBlock.GetObjectBuilder();
            return Builder.TypeId == ThereBuilder.TypeId && Builder.SubtypeId == ThereBuilder.SubtypeId;
        }

        /// <summary>
        /// Check if the given block is a "live" existing block, or a "zombie" reference left after a dead and removed block.
        /// </summary>
        /// <param name="StrictCheck">Performs strict check (checks if block in same place is of same typeid+subtypeid). Generates 2 object builders.</param>
        public static bool IsLive(this IMyCubeBlock Block, bool StrictCheck = false)
        {
            if (Block == null) return false;
            if (Block.Closed) return false;
            if (Block.SlimBlock?.IsDestroyed != false) return false;
            var ThereBlock = Block.CubeGrid.GetCubeBlock(Block.Position);
            if (ThereBlock == null) return false;
            if (!StrictCheck) return true;
            var Builder = Block.GetObjectBuilder();
            var ThereBuilder = ThereBlock.GetObjectBuilder();
            return Builder.TypeId == ThereBuilder.TypeId && Builder.SubtypeId == ThereBuilder.SubtypeId;
        }

        public static float BuildPercent(this IMySlimBlock block)
        {
            return block.Integrity / block.MaxIntegrity;
        }

        public static float BuildPercent(this IMyCubeBlock block)
        {
            return block.SlimBlock.BuildPercent();
        }

        public static Dictionary<string, int> GetMissingComponents(this IMySlimBlock Block)
        {
            var Dict = new Dictionary<string, int>();
            Block.GetMissingComponents(Dict);
            return Dict;
        }
    }
}
