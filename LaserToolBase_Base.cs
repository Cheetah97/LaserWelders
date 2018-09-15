using Cheetah.Networking;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Cheetah.LaserTools
{
    public abstract partial class LaserToolBase : MyGameLogicComponent
    {
        public IMyShipToolBase Tool { get; private set; }
        public bool IsWelder => Tool is IMyShipWelder;
        public bool IsGrinder => Tool is IMyShipGrinder;
        public bool IsDrill => Tool is IMyShipDrill;
        public bool LocalPlayerIsOwner => MyAPIGateway.Session.LocalHumanPlayer?.IdentityId == Tool.OwnerId;
        public IMyInventory ToolCargo { get; private set; }
        public float CargoFillRatio => (float)((double)ToolCargo.CurrentVolume / (double)ToolCargo.MaxVolume);
        protected List<IMyCubeBlock> OnboardInventoryOwners => GridInventoryModule.GetAccessibleInventories(Tool).Cast<IMyCubeBlock>().ToList();
        public IMyCubeGrid ToolGrid => Tool.CubeGrid;
        public IMyGridTerminalSystem Term => ToolGrid.GetTerminalSystem();
        
        public ToolTermCtl TermModule { get; private set; }
        public BeamController BeamCtlModule { get; private set; }
        public ToolPersistence PersistenceModule { get; private set; }
        public GridInventories GridInventoryModule { get; private set; }
        public WelderHUDModule HUDModule { get; private set; }
        public ToolPowerModule PowerModule { get; private set; }

        public IMyHudNotification MissingHUD { get; private set; }
        protected IMyHudNotification DebugNote;
        ushort Ticks = 0;

        
        protected IMyPlayer Owner { get; private set; }


        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            Tool = Entity as IMyShipToolBase;
            TermModule = new ToolTermCtl(this);
            BeamCtlModule = new BeamController(this);
            PersistenceModule = new ToolPersistence(this);
            HUDModule = new WelderHUDModule(this);
            PowerModule = new ToolPowerModule(this);
            //Tool.ResourceSink.SetRequiredInputFuncByType(Electricity, () => PowerConsumptionFunc());
            try
            {
                if (!Tool.HasComponent<MyModStorageComponent>())
                {
                    Tool.Storage = new MyModStorageComponent();
                    Tool.Components.Add(Tool.Storage);
                    SessionCore.DebugWrite($"{Tool.CustomName}.Init()", "Block doesn't have a Storage component!", IsExcessive: false);
                }
            }
            catch { }
        }

        public override void Close()
        {
            try
            {
                if (SessionCore.Settings.Debug)
                {
                    DebugNote.Hide();
                    DebugNote.AliveTime = 0;
                    DebugNote = null;
                }
            }
            catch (Exception Scrap)
            {
                SessionCore.LogError($"{Tool.CustomName}.Close().DebugClose", Scrap);
            }
            try
            {
                HUDModule.Close();
                PersistenceModule.Close();
                TermModule.Close();
                GridInventoryModule.RemoveWatcher(Tool);
            }
            catch (Exception Scrap)
            {
                SessionCore.LogError($"{Tool.CustomName}.Close()", Scrap);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            
            try
            {
                if (!Networker.Inited) Networker.Init(SessionCore.ModID);
                try
                {
                    if (Tool.CubeGrid.Physics?.Enabled != true)
                    {
                        NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                        return;
                    }
                }
                catch (Exception Scrap)
                {
                    SessionCore.LogError($"{Tool.CustomName}.OnceBeforeFrame().PhysicsCheck", Scrap);
                }


                try
                {
                    ToolCargo = Tool.GetInventory();
                }
                catch (Exception Scrap)
                {
                    SessionCore.LogError($"{Tool.CustomName}.OnceBeforeFrame().GetInventory", Scrap);
                }

                try
                {
                    TermModule.Init();
                }
                catch (Exception Scrap)
                {
                    SessionCore.LogError($"{Tool.CustomName}.OnceBeforeFrame().TermModuleInit", Scrap);
                }

                try
                {
                    PersistenceModule.Init();
                }
                catch (Exception Scrap)
                {
                    SessionCore.LogError($"{Tool.CustomName}.OnceBeforeFrame().PersistenceModuleInit", Scrap);
                }
                
                try
                {
                    PersistenceModule.Load();
                }
                catch (Exception Scrap)
                {
                    SessionCore.LogError($"{Tool.CustomName}.OnceBeforeFrame().PersistenceModuleLoad", Scrap);
                }
                
                try
                {
                    GridInventoryModule = ToolGrid.GetComponent<GridInventories>();
                    if (IsWelder) GridInventoryModule.AddWatcher(Tool);
                }
                catch (Exception Scrap)
                {
                    SessionCore.LogError($"{Tool.CustomName}.OnceBeforeFrame().InventoryModule", Scrap);
                }

                try
                {
                    HUDModule.Init();
                }
                catch (Exception Scrap)
                {
                    SessionCore.LogError($"{Tool.CustomName}.OnceBeforeFrame().HudModule", Scrap);
                }

                if (IsWelder && MyAPIGateway.Session.LocalHumanPlayer?.IdentityId == Tool.OwnerId)
                {
                    MissingHUD = MyAPIGateway.Utilities.CreateNotification("", int.MaxValue, "Red");
                }

                CheckInitControls();
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
                Tool.AppendingCustomInfo += Tool_AppendingCustomInfo;
                DebugNote = MyAPIGateway.Utilities.CreateNotification($"{Tool.CustomName}", int.MaxValue, (IsWelder ? "Blue" : "Red"));
                Owner = MyAPIGateway.Players.GetPlayer(Tool.OwnerId);
                if (SessionCore.Settings.Debug) DebugNote.Show();
            }
            catch (Exception Scrap)
            {
                SessionCore.LogError($"{Tool.CustomName}.OnceBeforeFrame()", Scrap);
            }
        }

        private void Tool_AppendingCustomInfo(IMyTerminalBlock trash, StringBuilder Info)
        {
            Info.Clear();
            //Info.AppendLine($"Current Input: {Math.Round(Tool.ResourceSink.RequiredInputByType(Electricity), 2)} MW");
            Info.AppendLine($"Max Required Input: {Math.Round(PowerModule.PowerConsumptionFunc(true), 2)} MW");
            //Info.AppendLine($"Performance impact: {(RunTimesAvailable ? Math.Round(AvgRunTime, 4).ToString() : "--")}/{(RunTimesAvailable ? Math.Round(MaxRunTime, 4).ToString() : "--")} ms (avg/max)");
            if (Tool is IMyShipWelder)
                Info.AppendLine($"Support inventories: {OnboardInventoryOwners.Count}");
        }

        void CheckInitControls()
        {
            string Message = "Attention! Due to a bug in the game itself, you might not be able to work with these tools via mouse-click.\nIf you run into this issue, you have to use the Toggle switch in terminal or on/off switch on toolbar.\nSorry for inconvenience.";
            if (IsWelder)
            {
                if (!Terminals.InitedWelderControls)
                {
                    Terminals.InitWelderControls();
                    MyAPIGateway.Utilities.ShowMessage("Laser Welders", Message);
                }
            }
            else if (IsGrinder)
            {
                if (!Terminals.InitedGrinderControls)
                {
                    Terminals.InitGrinderControls();
                    MyAPIGateway.Utilities.ShowMessage("Laser Grinders", Message);
                }
            }
        }
    }
}