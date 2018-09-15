using Cheetah.Networking;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;

namespace Cheetah.LaserTools
{
    public class WelderHUDModule : ToolModuleBase
    {
        Syncer<string> MissingText;
        public bool MessageExpired => (DateTime.Now - HUDMessageUpdate) > TimeSpan.FromSeconds(5);
        private DateTime HUDMessageUpdate;

        public WelderHUDModule(LaserToolBase ToolComp) : base(ToolComp) { }

        public override void Init()
        {
            try
            {
                if (ToolComp.Tool == null) throw new Exception("ToolComp.Tool is null");
                MissingText = new Syncer<string>(Tool, "Missing", "");
                HUDMessageUpdate = DateTime.Now - TimeSpan.FromSeconds(10);
                try
                {
                    MissingText.GotValueFromServer += ComplainMissingLocal;
                }
                catch (Exception Scrap)
                {
                    SessionCore.LogError($"{Tool.CustomName}.HudModule.Init().Subscribe", Scrap);
                }
            }
            catch (Exception Scrap)
            {
                SessionCore.LogError($"{Tool.CustomName}.HudModule.Init().New", Scrap);
            }
        }

        public override void Close()
        {
            MissingText.GotValueFromServer -= ComplainMissingLocal;
            MissingText.Close();
        }

        void ComplainMissingLocal()
        {
            ComplainMissingLocal(MissingText);
        }

        void ComplainMissingLocal(string Missing)
        {
            try
            {
                var Player = MyAPIGateway.Session.LocalHumanPlayer;
                if (Player == null) return;
                if (Player.IdentityId != Tool.OwnerId) return;

                var Dist = Player.GetPosition().DistanceTo(Tool.GetPosition());
                if (Dist > 200)
                {
                    //SessionCore.DebugWrite(Tool.CustomName, $"ComplainMissing() exit - local player too far ({Math.Round(Dist)}m)", WriteOnlyIfDebug: true);
                    return;
                }
                //SessionCore.DebugWrite(Tool.CustomName, $"ComplainMissing() successfully complained. Missing length: {Missing.Length}", WriteOnlyIfDebug: true);
                ToolComp.MissingHUD.Text = Missing;
                HUDMessageUpdate = DateTime.Now;
                ToolComp.MissingHUD.Show();
            }
            catch (Exception Scrap)
            {
                SessionCore.LogError($"{Tool.CustomName}.ComplainMissing()", Scrap);
            }
        }

        public void ComplainMissing(Dictionary<IMySlimBlock, Dictionary<string, int>> MissingPerBlock)
        {
            if (MissingPerBlock.Count == 0)
            {
                //SessionCore.DebugWrite(Tool.CustomName, $"ComplainMissing() early exit - 0 missing components", WriteOnlyIfDebug: true);
                return;
            }

            //if (SessionCore.Debug)
            //    SessionCore.DebugWrite(Tool.CustomName, $"ComplainMissing(): {MissingPerBlock.Count} blocks, {MissingPerBlock.Values.Sum(x => x.Values.Sum())} total components missing", WriteOnlyIfDebug: true);

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                StringBuilder Text = WriteMissing(MissingPerBlock);
                var Player = MyAPIGateway.Session.Player;
                if (Player == null || Player.IdentityId != Tool.OwnerId)
                {
                    MissingText.Set(Text.ToString());
                    //SessionCore.DebugWrite(Tool.CustomName, $"ComplainMissing() exit - local player == null or ID mismatch", WriteOnlyIfDebug: true);
                }
                else if (Player.IdentityId == Tool.OwnerId)
                {
                    ComplainMissingLocal(Text.ToString());
                }
            }
            else
            {
                //SessionCore.DebugWrite(Tool.CustomName, $"ComplainMissing() exit - not a server", WriteOnlyIfDebug: true);
            }
        }

        StringBuilder WriteMissing(Dictionary<IMySlimBlock, Dictionary<string, int>> MissingPerBlock)
        {
            StringBuilder Text = new StringBuilder();

            if (MissingPerBlock.Count == 1)
            {
                IMySlimBlock Block = MissingPerBlock.Keys.First();
                var Missing = MissingPerBlock[Block];
                bool IsProjected = Block.IsProjectable();
                if (Missing != null && Missing.Count > 0)
                {
                    Text.AppendLine($"{Tool.CustomName}: can't proceed to {(!IsProjected ? "build" : "place")} {Block.BlockDefinition.DisplayNameText}, missing:\n");
                    foreach (var ItemPair in Missing)
                    {
                        Text.AppendLine($"{ItemPair.Key}: {(!IsProjected ? 1 : ItemPair.Value)}");
                        if (IsProjected) break;
                    }
                }
            }
            else if (MissingPerBlock.Count > 1 && MissingPerBlock.Values.Any(x => x.Count > 0))
            {
                Text.AppendLine($"{Tool.CustomName}: can't proceed to build {MissingPerBlock.Count} blocks:\n");
                foreach (IMySlimBlock Block in MissingPerBlock.Keys)
                {
                    var Missing = MissingPerBlock[Block];
                    if (Missing.Count == 0) continue;
                    Text.AppendLine($"{Block.BlockDefinition.DisplayNameText}: missing:");
                    foreach (var ItemPair in Missing)
                    {
                        Text.AppendLine($"{ItemPair.Key}: {ItemPair.Value}");
                    }
                    Text.AppendLine();
                }
            }
            //SessionCore.DebugWrite(Tool.CustomName, $"WriteMissing() assembled StringBuilder - {Text.Length} chars");
            Text.RemoveTrailingNewlines();
            return Text;
        }
    }
}
