using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Cheetah.LaserTools
{
    public class ToolPersistence : ToolModuleBase
    {
        #region Struct
        [ProtoContract]
        public struct PersistentStruct
        {
            [ProtoMember(1)]
            public float BeamLength;
            [ProtoMember(2)]
            public bool DistanceBased;
            [ProtoMember(3)]
            public float SpeedMultiplier;
        }
        #endregion

        public ToolPersistence(LaserToolBase ToolComp) : base(ToolComp) { }

        public override void Init()
        {
            try
            {
                SessionCore.SaveRegister(Save);
            }
            catch (Exception Scrap)
            {
                SessionCore.LogError($"{Tool.CustomName}.Persistence.Init()", Scrap);
            }
        }

        public void Load()
        {
            try
            {
                if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Multiplayer.IsServer)
                {
                    ToolComp.TermModule.LoadSyncersFromServer();
                    return;
                }
                string Storage = "";

                if (Tool.Storage == null)
                {
                    SessionCore.DebugWrite($"{Tool.CustomName}.Load()", "Tool's Storage doesn't exist.");
                    ToolComp.TermModule.BeamLength = ToolComp.BeamCtlModule.MaxBeamLengthBlocks;
                    return;
                }

                if (!Tool.Storage.ContainsKey(SessionCore.StorageGuid))
                {
                    SessionCore.DebugWrite($"{Tool.CustomName}.Load()", "Tool's Storage is empty.");
                    ToolComp.TermModule.BeamLength = ToolComp.BeamCtlModule.MaxBeamLengthBlocks;
                    return;
                }

                if (Tool.Storage.TryGetValue(SessionCore.StorageGuid, out Storage) == true/* ||
                    MyAPIGateway.Utilities.GetVariable($"settings_{Tool.EntityId}", out Storage)*/)
                {
                    try
                    {
                        SessionCore.DebugWrite($"{Tool.CustomName}.Load()", $"Accessing storage. Raw data: {Storage}");
                        PersistentStruct persistent = MyAPIGateway.Utilities.SerializeFromBinary<PersistentStruct>(Convert.FromBase64String(Storage));
                        ToolComp.TermModule.BeamLength = (int)persistent.BeamLength;
                        if (ToolComp.TermModule.BeamLength > ToolComp.BeamCtlModule.MaxBeamLengthBlocks) ToolComp.TermModule.BeamLength = ToolComp.BeamCtlModule.MaxBeamLengthBlocks;
                        ToolComp.TermModule.DistanceMode = persistent.DistanceBased;
                        ToolComp.TermModule.SpeedMultiplier = (int)persistent.SpeedMultiplier;
                        SessionCore.DebugWrite($"{Tool.CustomName}.Load()", $"Loaded from storage. Persistent Beamlength: {persistent.BeamLength}; Sync Beamlength: {ToolComp.TermModule.BeamLength}");
                    }
                    catch (Exception Scrap)
                    {
                        SessionCore.LogError($"{Tool.CustomName}.Load()", Scrap);
                    }
                }
                else
                {
                    ToolComp.TermModule.BeamLength = ToolComp.BeamCtlModule.MaxBeamLengthBlocks;
                    SessionCore.DebugWrite($"{Tool.CustomName}.Load()", "Storage access failed.");
                }
            }
            catch (Exception Scrap)
            {
                SessionCore.LogError($"{Tool.CustomName}.Load().AccessStorage", Scrap);
            }
        }

        public void Save()
        {
            if (Tool.Storage == null)
            {
                SessionCore.DebugWrite($"{Tool.CustomName}.Save()", "Tool's Storage doesn't exist.");
                return;
            }

            if (!Tool.Storage.ContainsKey(SessionCore.StorageGuid))
            {
                SessionCore.DebugWrite($"{Tool.CustomName}.Save()", "Tool's Storage is empty.");
                Tool.Storage[SessionCore.StorageGuid] = "";
            }

            try
            {
                PersistentStruct persistent;
                persistent.BeamLength = ToolComp.TermModule.BeamLength;
                persistent.DistanceBased = ToolComp.TermModule.DistanceMode;
                persistent.SpeedMultiplier = ToolComp.TermModule.SpeedMultiplier;
                string Raw = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(persistent));
                //MyAPIGateway.Utilities.SetVariable($"settings_{Tool.EntityId}", Raw);
                if (!Tool.Storage.ContainsKey(SessionCore.StorageGuid)) Tool.Storage.Add(SessionCore.StorageGuid, Raw);
                else Tool.Storage[SessionCore.StorageGuid] = Raw;
                SessionCore.DebugWrite($"{Tool.CustomName}.Save()", $"Saved to storage. Raw data: {Raw}");
                SessionCore.DebugWrite($"{Tool.CustomName}.Save()", $"Checked raw data: {Tool.Storage[SessionCore.StorageGuid]}");
            }
            catch (Exception Scrap)
            {
                SessionCore.LogError($"{Tool.CustomName}.Save()", Scrap);
            }
        }

        public override void Close()
        {
            SessionCore.SaveUnregister(Save);
        }
    }
}
