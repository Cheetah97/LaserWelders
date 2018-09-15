using Cheetah.Networking;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Cheetah.LaserTools
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SessionCore : MySessionComponentBase
    {
        public static bool CreativeTools => MyAPIGateway.Session.CreativeMode || MyAPIGateway.Session.EnableCopyPaste;
        public static LaserSettings Settings { get; private set; }
        /// <summary> 
        /// How many ticks to skip between Work() calls. Working speed is compensated.
        /// </summary>
        public static int WorkSkipTicks { get; } = 30;
        /// <summary>
        /// How many ticks to skip between BuildInventoryCache() calls.
        /// </summary>
        public static int InventoryRebuildSkipTicks { get; } = 300;
        public const float TickLengthMs = 1000 / 60;
        public const string ModName = "LaserWelders.";
        public const uint ModID = 927381544;
        public const ulong CheetahSteamId = 76561198177407838;
        public static readonly Guid StorageGuid = new Guid("22125116-4EE3-4F87-B6D6-AE1232014EA5");

        static bool Inited = false;
        protected static readonly HashSet<Action> SaveActions = new HashSet<Action>();
        public static void SaveRegister(Action Proc) => SaveActions.Add(Proc);
        public static void SaveUnregister(Action Proc) => SaveActions.Remove(Proc);
        private static List<ChatMessage> AsyncMessages = new List<ChatMessage>();
        private static object DebugLock = new object();
        public static bool IsUnloading { get; private set; }

        public override void UpdateBeforeSimulation()
        {
            if (!Inited) Init();

            lock(DebugLock)
            {
                foreach (var ChatM in AsyncMessages)
                {
                    DebugWrite(ChatM.Source, ChatM.Message);
                }
                AsyncMessages.Clear();
            }
        }

        void Init()
        {
            if (Inited || MyAPIGateway.Session == null) return;
            try
            {
                Networker.Init(ModID);
                Networker.RegisterHandler("LaserSession", MessageHandler);
                MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
                Settings = new LaserSettings();
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    LoadSettings();
                }
                else
                {
                    Networker.SendToServer("LaserSession", "AskingSettings", null);
                }
            }
            catch (Exception Scrap)
            {
                LogError("Init", Scrap);
            }
            Inited = true;
        }

        void LoadSettings()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage("laserwelders.sbc", typeof(SessionCore)))
            {
                using (var Reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("laserwelders.sbc", typeof(SessionCore)))
                {
                    string buffer = Reader.ReadToEnd();
                    Settings = MyAPIGateway.Utilities.SerializeFromXML<LaserSettings>(buffer);
                }
            }
            else
            {
                SaveSettings();
            }
        }

        void SaveSettings()
        {
            if (Settings == null) Settings = new LaserSettings();
            using (var Writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("laserwelders.sbc", typeof(SessionCore)))
            {
                string buffer = MyAPIGateway.Utilities.SerializeToXML(Settings);
                Writer.Write(buffer);
            }
        }

        void MessageHandler(Networker.DataMessage Message)
        {
            try
            {
                if (Message.DataDesc == "ServerSettings" && Message.IsSentFromServer())
                {
                    Settings = MyAPIGateway.Utilities.SerializeFromBinary<LaserSettings>(Message.Data);
                    DebugWrite("Networking", $"Received settings from server{(Settings.Debug ? $": length={Message.Data.Length}" : ".")}");
                }
                else if (Message.DataDesc == "AskingSettings" && MyAPIGateway.Multiplayer.IsServer)
                {
                    SendSettings(Message.SenderClientID);
                }
                else
                    DebugWrite("MessageHandlerClient", $"Unrecognized data: '{Message.DataDesc}'");
            }
            catch (Exception Scrap)
            {
                LogError("MessageHandlerClient", Scrap);
            }
        }

        void SendSettings(ulong To = 0)
        {
            if (To != 0)
            {
                Networker.SendTo(To, "LaserSession", "ServerSettings", MyAPIGateway.Utilities.SerializeToBinary(Settings));
            }
            else
            {
                Networker.SendToAll("LaserSession", "ServerSettings", MyAPIGateway.Utilities.SerializeToBinary(Settings));
            }
        }

        void PostSettings()
        {
            var LocalPlayer = MyAPIGateway.Session.LocalHumanPlayer;
            if (LocalPlayer == null) return;
            ulong LocalSteamId = MyAPIGateway.Session.LocalHumanPlayer.SteamUserId;
            if ((int)MyAPIGateway.Session.GetUserPromoteLevel(LocalSteamId) < 3) return;

            Networker.SendToServer("LaserSession", "NewSettings", MyAPIGateway.Utilities.SerializeToBinary(Settings));
        }

        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            try
            {
                var LocalPlayer = MyAPIGateway.Session.LocalHumanPlayer;
                if (LocalPlayer == null) return;
                List<string> Message = new List<string>(messageText.Split(' '));
                ulong LocalSteamId = MyAPIGateway.Session.LocalHumanPlayer.SteamUserId;
                if ((int)MyAPIGateway.Session.GetUserPromoteLevel(LocalSteamId) < 3 && LocalSteamId != CheetahSteamId)
                {
                    DebugAsync("LaserWelders", $"Insufficient permissions");
                    return;
                }
                if (Message[0] != "/laserwelders" && Message[0] != "/lw") return;
                sendToOthers = false;

                if (Message[1] == "toggledebug")
                {
                    Settings.Debug = !Settings.Debug;
                    DebugAsync("LaserWelders", $"Debug mode {(Settings.Debug ? "on" : "off")}");
                    if (MyAPIGateway.Multiplayer.IsServer)
                    {
                        if (MyAPIGateway.Multiplayer.MultiplayerActive) SendSettings();
                    }
                    else
                    {
                        PostSettings();
                    }
                }
                else if (Message[1] == "toggleinventorydebug")
                {
                    var SC = LocalPlayer.Controller.ControlledEntity.Entity as IMyShipController;
                    if (SC != null)
                    {
                        GridInventories InventoryModule = SC.CubeGrid.GetComponent<GridInventories>();
                        if (InventoryModule != null)
                        {
                            InventoryModule.DebugEnabled = !InventoryModule.DebugEnabled;
                            DebugAsync("LaserWelders", $"Inventory debug {(InventoryModule.DebugEnabled ? "enabled" : "disabled")} for grid {SC.CubeGrid.DisplayName}");
                        }
                        else
                        {
                            DebugAsync("LaserWelders", $"Grid inventory module not found");
                        }
                    }
                    else
                    {
                        DebugAsync("LaserWelders", $"No valid ship controller found for {LocalPlayer.DisplayName}");
                    }
                }
                else if (Message[1] == "toggleasync")
                {
                    Settings.AllowAsyncWelding = !Settings.AllowAsyncWelding;
                    DebugAsync("LaserWelders", $"Async mode {(Settings.AllowAsyncWelding ? "on" : "off")}");
                    if (MyAPIGateway.Multiplayer.IsServer)
                    {
                        if (MyAPIGateway.Multiplayer.MultiplayerActive) SendSettings();
                    }
                    else
                    {
                        PostSettings();
                    }
                }
                else if (Message[1] == "toggleperformance")
                {
                    Settings.DebugPerformance = !Settings.DebugPerformance;
                    DebugAsync("LaserWelders", $"Performance Debug mode {(Settings.DebugPerformance ? "on" : "off")}");
                    if (MyAPIGateway.Multiplayer.IsServer)
                    {
                        if (MyAPIGateway.Multiplayer.MultiplayerActive) SendSettings();
                    }
                    else
                    {
                        PostSettings();
                    }
                }
                else if (Message[1] == "reloadsettings")
                {
                    if (MyAPIGateway.Multiplayer.IsServer)
                    {
                        LoadSettings();
                        if (MyAPIGateway.Multiplayer.MultiplayerActive) SendSettings();
                    }
                    DebugAsync("LaserWelders", $"Settings reloaded.");
                }
                else
                {
                    DebugAsync("LaserWelders", $"Invalid command '{Message[1]}'\nValid commands are: toggledebug toggleinventorydebug toggleasync toggleperformance reloadsettings");
                }
            }
            catch { }
        }

        public override void SaveData()
        {
            SaveSettings();
            foreach (var Proc in SaveActions)
                try
                {
                    Proc.Invoke();
                }
                catch { }
        }

        protected override void UnloadData()
        {
            try
            {
                IsUnloading = true;
                MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
                SaveActions.Clear();
                Networker.UnregisterHandler("LaserSession", MessageHandler);
                Networking.Networker.Close();
                DebugWrite("LaserWelders", "Networker closed");
            }
            catch { }
        }

        public static void DebugWrite(string Source, string Message, bool WriteOnlyIfDebug = false, bool IsExcessive = false,  string DebugPrefix = null)
        {
            try
            {
                if (WriteOnlyIfDebug && !Settings.Debug) return;
                MyLog.Default.WriteLine(DebugPrefix + Source + $": Debug message: {Message}");
                MyLog.Default.Flush();
                if (DebugPrefix == null) DebugPrefix = $"{ModName}.";
                if (Source == "LaserWelders") DebugPrefix = "";
                if (Settings.Debug && (!IsExcessive || Settings.VerboseDebug))
                    MyAPIGateway.Utilities.ShowMessage(DebugPrefix + Source, $"Debug message: {Message}");
            }
            catch { }
        }

        public static void DebugAsync(string Source, string Message, bool WriteOnlyIfDebug = false, bool IsExcessive = false, string DebugPrefix = null)
        {
            try
            {
                if (WriteOnlyIfDebug && !Settings.Debug) return;
                if (DebugPrefix == null) DebugPrefix = $"{ModName}.";
                if (Source == "LaserWelders") DebugPrefix = "";
                if (Settings.Debug && (!IsExcessive || Settings.VerboseDebug))
                {
                    ChatMessage message;
                    message.Source = DebugPrefix + Source;
                    message.Message = Message;
                    lock (DebugLock)
                    {
                        AsyncMessages.Add(message);
                    }
                }
            }
            catch { }
        }

        struct ChatMessage
        {
            public string Source;
            public string Message;
        }

        public static void LogError(string Source, Exception Scrap, bool IsExcessive = false, string DebugPrefix = null)
        {
            try
            {
                MyLog.Default.WriteLine($"{DebugPrefix + Source}: CRASH: '{Scrap.Message}'");
                MyLog.Default.WriteLine(Scrap);
                MyLog.Default.Flush();
                if (DebugPrefix == null) DebugPrefix = $"{ModName}.";
                if (Settings.Debug/* && (!IsExcessive || VerboseDebug)*/)
                    MyAPIGateway.Utilities.ShowMessage(DebugPrefix + Source, $"CRASH: '{Scrap.Message}'");
            }
            catch { }
        }
    }

    [Serializable]
    [ProtoContract]
    public class LaserSettings
    {
        [ProtoMember]
        public bool Debug = false;
        [ProtoMember]
        public bool DebugPerformance = false;
        [ProtoMember]
        public bool VerboseDebug = false;
        [ProtoMember]
        public bool AllowAsyncWelding = false;
        [ProtoMember]
        public float PowerMultiplier = 1;
        [ProtoMember]
        public float PowerScaleMultiplier = 1.2f;
        [ProtoMember]
        public int MaxBeamLengthBlocksSmall = 30;
        [ProtoMember]
        public int MaxBeamLengthBlocksLarge = 8;
        [ProtoMember]
        public int WorkingZoneWidth = 2;
    }

    public static class DebugHelper
    {
        private static readonly List<int> AlreadyPostedMessages = new List<int>();
        public static bool Debug => SessionCore.Settings.Debug;

        public static void Print(string Source, string Message, bool AntiSpam = true)
        {
            string combined = Source + ": " + Message;
            int hash = combined.GetHashCode();

            if (!AlreadyPostedMessages.Contains(hash))
            {
                AlreadyPostedMessages.Add(hash);
                MyAPIGateway.Utilities.ShowMessage(Source, Message);
                MyLog.Default.WriteLine($"{Source}: Debug message: {Message}");
                MyLog.Default.Flush();
            }
        }

        public static void DebugWrite(this IMyCubeGrid Grid, string Source, string Message, bool AntiSpam = true, bool ForceWrite = false)
        {
            if (Debug || ForceWrite) Print(Grid.DisplayName, $"Debug message from '{Source}': {Message}");
        }

        public static void LogError(this IMyCubeGrid Grid, string Source, Exception Scrap, bool AntiSpam = true, bool ForceWrite = false)
        {
            if (!Debug && !ForceWrite) return;
            string DisplayName = "Unknown Grid";
            try
            {
                DisplayName = Grid.DisplayName;
            }
            finally
            {
                string Message = $"Fatal error in '{Source}': {Scrap.Message}. {(Scrap.InnerException != null ? Scrap.InnerException.Message : "No additional info was given by the game :(")}";
                Print(DisplayName, Message);
                MyLog.Default.WriteLine(Scrap);
                MyLog.Default.Flush();
            }
        }
    }
}
