using Cheetah.Networking;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Cheetah.LaserTools
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid), false)]
    public sealed class GridInventories : MyGameLogicComponent
    {
        public IMyCubeGrid Grid => Entity as IMyCubeGrid;
        public HashSet<IMyTerminalBlock> OnboardInventoryOwners { get; private set; } = new HashSet<IMyTerminalBlock>();
        private Dictionary<IMyTerminalBlock, HashSet<IMyTerminalBlock>> InventoriesPerWatcher = new Dictionary<IMyTerminalBlock, HashSet<IMyTerminalBlock>>();
        public DateTime LastActualRefresh { get; private set; }
        public IMyGridTerminalSystem Term { get; private set; }
        public bool DebugEnabled;
        public object InventoryLock { get; } = new object();

        private HashSet<IMyTerminalBlock> Watchers = new HashSet<IMyTerminalBlock>();
        private bool HasWatchers => Watchers.Count >= 1;
        private bool NeedsRefresh = false;
        private int PreviousGridsCount = 0;
        private int CurrentGridsCount => MyAPIGateway.GridGroups.GetGroup(Grid, GridLinkTypeEnum.Physical).Count;
        public DateTime LastUpdate { get; private set; }
        private StringBuilder InventoriesStatus = new StringBuilder();
        private bool ShallShowDebug = false;
        private DateTime LastShow;
        //private Syncer<InventoryListing> InventoryList;

        public override void UpdateBeforeSimulation10()
        {
            if (DebugEnabled && ShallShowDebug) Debug();
            if (!HasWatchers) return;
            if (PreviousGridsCount != CurrentGridsCount) NeedsRefresh = true;
            if ((DateTime.Now - LastUpdate) > TimeSpan.FromSeconds(15)) NeedsRefresh = true;

            if (NeedsRefresh)
            {
                ParallelTasks.WorkOptions opt = new ParallelTasks.WorkOptions();
                opt.MaximumThreads = 1;
                opt.QueueFIFO = true;
                MyAPIGateway.Parallel.Start(UpdateInventoryCache, opt);
            }
        }

        bool IsValidInventory(IMyTerminalBlock Block)
        {
            if (Block == null) return false;
            if (Block.IsOfType<IMyCargoContainer>()
                || Block.IsOfType<IMyAssembler>()
                || Block.IsOfType<IMyShipConnector>()
                || Block.IsOfType<IMyCollector>())
            {
                if (!Block.IsFunctional)
                {
                    if (DebugEnabled) InventoriesStatus.AppendLine($"{Block.CustomName} skipped: non-functional");
                    return false;
                }
                
                if (string.IsNullOrWhiteSpace(Block.Name))
                {
                    Block.Name = $"Entity_{Block.EntityId}";
                    MyAPIGateway.Entities.SetEntityName(Block);
                }
                return true;
            }
            
            return false;
        }

        public List<IMyTerminalBlock> GetAccessibleInventories(IMyTerminalBlock RequestingBlock)
        {
            if (SessionCore.IsUnloading ||!InventoriesPerWatcher.ContainsKey(RequestingBlock) || InventoriesPerWatcher[RequestingBlock] == null) return new List<IMyTerminalBlock>();
            return InventoriesPerWatcher[RequestingBlock].ToList();
        }

        private HashSet<IMyTerminalBlock> RefreshAccessibleInventories(IMyTerminalBlock RequestingBlock)
        {
            HashSet<IMyTerminalBlock> AccessibleBlocks = new HashSet<IMyTerminalBlock>();
            if (SessionCore.IsUnloading) return AccessibleBlocks;

            if (string.IsNullOrWhiteSpace(RequestingBlock.Name))
            {
                RequestingBlock.Name = $"Entity_{RequestingBlock.EntityId}";
                MyAPIGateway.Entities.SetEntityName(RequestingBlock);
            }
            var RequestingInventory = RequestingBlock.GetInventory();
            if (DebugEnabled)
            {
                InventoriesStatus.AppendLine();
                InventoriesStatus.AppendLine($"Inventory data for {RequestingBlock.CustomName}:");
            }
            foreach (var Block in OnboardInventoryOwners)
            {
                if (Block.EntityId == RequestingBlock.EntityId) continue;
                if (!Block.IsFunctional)  // Stuff happens, why not
                {
                    if (DebugEnabled) InventoriesStatus.AppendLine($"{Block.CustomName} skipped: non-functional");
                    continue;
                }
                if (!Block.HasPlayerAccess(RequestingBlock.OwnerId))
                {
                    if (DebugEnabled) InventoriesStatus.AppendLine($"{Block.CustomName} skipped: no access");
                    continue;
                }
                if (Block.InventoryCount == 0)
                {
                    if (DebugEnabled) InventoriesStatus.AppendLine($"{Block.CustomName} skipped: 0 inventories");
                    continue;
                }
                if (!Block.GetInventory().IsConnectedTo(RequestingInventory) && !Sandbox.Game.MyVisualScriptLogicProvider.IsConveyorConnected(RequestingBlock.Name, Block.Name))
                {
                    if (DebugEnabled) InventoriesStatus.AppendLine($"{Block.CustomName} skipped: no conveyor connection");
                    continue;
                }
                AccessibleBlocks.Add(Block);
                if (DebugEnabled) InventoriesStatus.AppendLine($"{Block.CustomName} added.");
            }
            if (DebugEnabled) InventoriesStatus.AppendLine();
            return AccessibleBlocks;
        }

        public void UpdateInventoryCache()
        {
            if (SessionCore.IsUnloading) return;
            lock (InventoryLock)
            {
                InventoriesStatus.Clear();
                OnboardInventoryOwners.Clear();
                if (!HasWatchers) return;
                ShallShowDebug = true;
                System.Diagnostics.Stopwatch Watch = new System.Diagnostics.Stopwatch();
                Watch.Start();
                List<IMyTerminalBlock> trash = new List<IMyTerminalBlock>();
                Func<IMyTerminalBlock, bool> Puller = (Block) =>
                {
                    if (IsValidInventory(Block)) OnboardInventoryOwners.Add(Block);
                    return false;
                };
                Term.GetBlocksOfType(trash, Puller);

                foreach (var Watcher in Watchers)
                {
                    InventoriesPerWatcher[Watcher] = RefreshAccessibleInventories(Watcher);
                }
                LastUpdate = DateTime.Now;
                NeedsRefresh = false;

                Watch.Stop();
                Watch.Report(Grid.CustomName, "Grid inventories refresh", UseAsync: true);
            }
        }

        public void Debug()
        {
            if (MyAPIGateway.Session.LocalHumanPlayer?.IdentityId == Grid.BigOwners[0])
            {
                StringBuilder Screen = new StringBuilder();
                Screen.AppendLine($"Total valid onboard owners: {OnboardInventoryOwners.Count}");
                Screen.Append(InventoriesStatus.ToString());
                if ((DateTime.Now - LastShow) > TimeSpan.FromSeconds(10))
                {
                    MyAPIGateway.Utilities.ShowMissionScreen($"Inventory debug for {Grid.DisplayName}", "", "", Screen.ToString(), null, "Nice!");
                    LastShow = DateTime.Now;
                }
            }
            ShallShowDebug = false;
        }

        public Dictionary<string, int> PullIn(IMyTerminalBlock Puller, Dictionary<string, int> ComponentList, string TypeConstraint)
        {
            lock (InventoryLock)
            {
                // If you are wondering why I'm using Ingame interfaces in ModAPI, the Ingame inventory functions are more error-proof
                Dictionary<string, int> PulledList = new Dictionary<string, int>();
                var AccessibleInventoryOwners = GetAccessibleInventories(Puller);
                VRage.Game.ModAPI.Ingame.IMyInventory PullingInventory = (Puller as Sandbox.ModAPI.Ingame.IMyTerminalBlock).GetInventory();
                if (PullingInventory == null) return null;
                foreach (var Block in AccessibleInventoryOwners)
                {
                    VRage.Game.ModAPI.Ingame.IMyInventory BlockInventory;
                    if (Block.InventoryCount > 1) BlockInventory = (Block as Sandbox.ModAPI.Ingame.IMyTerminalBlock).GetInventory(1);
                    else BlockInventory = (Block as Sandbox.ModAPI.Ingame.IMyTerminalBlock).GetInventory(0);

                    var InventoryList = BlockInventory.GetItems();
                    foreach (VRage.Game.ModAPI.Ingame.IMyInventoryItem Item in InventoryList)
                    {
                        if (Item.Amount <= 0 || (TypeConstraint.Contains("Component") && Item.Amount < 1)) continue; // KSWH and their code...
                        if (!Item.Content.TypeId.ToString().Contains(TypeConstraint)) continue;
                        if (!ComponentList.ContainsKey(Item.Content.SubtypeName)) continue;
                        int NecessaryAmount = ComponentList[Item.Content.SubtypeName];
                        if (NecessaryAmount <= 0) continue; //This means we've pulled everything we need

                        int PullableAmount = Item.Amount > NecessaryAmount ? NecessaryAmount : (int)Item.Amount;
                        PullableAmount = (int)PullingInventory.ComputeAmountThatFits(Item, PullableAmount);
                        if (PullableAmount == 0) continue;

                        int ItemIndex = InventoryList.IndexOf(Item);
                        int ItemAmount = (int)Item.Amount;
                        PullingInventory.TransferItemFrom(BlockInventory, ItemIndex, null, true, PullableAmount);

                        ComponentList[Item.Content.SubtypeName] -= PullableAmount;
                        if (PulledList.ContainsKey(Item.Content.SubtypeName)) PulledList[Item.Content.SubtypeName] += ItemAmount;
                        else PulledList.Add(Item.Content.SubtypeName, ItemAmount);
                    }
                }
                return PulledList;
            }
        }

        #region Watchers
        public void AddWatcher(IMyTerminalBlock Block)
        {
            if (Block == null) return;
            Watchers.Add(Block);
        }

        public void RemoveWatcher(IMyTerminalBlock Block)
        {
            if (Block == null) return;
            Watchers.Remove(Block);
        }

        void OnGridChange(IMySlimBlock Block)
        {
            if (HasWatchers) NeedsRefresh = true;
        }

        void OnGridSplit(IMyCubeGrid trash1, IMyCubeGrid trash2)
        {
            if (HasWatchers) NeedsRefresh = true;
        }
        #endregion

        #region Event Subscriptions
        void SubscribeToEvents()
        {
            Grid.OnBlockAdded += OnGridChange;
            Grid.OnBlockRemoved += OnGridChange;
            Grid.OnBlockIntegrityChanged += OnGridChange;
            Grid.OnGridSplit += OnGridSplit;
        }

        void UnsubscribeFromEvents()
        {
            Grid.OnBlockAdded -= OnGridChange;
            Grid.OnBlockRemoved -= OnGridChange;
            Grid.OnBlockIntegrityChanged -= OnGridChange;
            Grid.OnGridSplit -= OnGridSplit;
        }
        #endregion

        #region Initialization and Closure
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (!Networker.Inited) Networker.Init(SessionCore.ModID);
            if (Grid.Physics == null || Grid.Physics.Enabled == false)
            {
                NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                return;
            }
            LastActualRefresh = DateTime.Now - TimeSpan.FromSeconds(1);
            Term = Grid.GetTerminalSystem();
            SubscribeToEvents();
            //InventoryList = new Syncer<InventoryListing>(Grid, "InventoryListing");
            NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.EACH_10TH_FRAME | VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;
            PreviousGridsCount = MyAPIGateway.GridGroups.GetGroup(Grid, GridLinkTypeEnum.Physical).Count;
            LastUpdate = DateTime.Now;
            LastShow = DateTime.Now - TimeSpan.FromSeconds(10);
            NeedsRefresh = true;
        }

        public override void Close()
        {
            UnsubscribeFromEvents();
            try
            {
                //InventoryList.Close();
            }
            catch (Exception Scrap)
            {
                SessionCore.LogError($"{Grid.CustomName}.GridInventory.Close().CloseSyncer", Scrap);
            }
        }

        [ProtoBuf.ProtoContract]
        private class InventoryListing
        {
            [ProtoBuf.ProtoMember]
            public List<long> OnboardInventoryOwnersTotal;
            [ProtoBuf.ProtoMember]
            public Dictionary<long, List<long>> OnboardOwnersPerBlock;
        }
        #endregion
    }
}
