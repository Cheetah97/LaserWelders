using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;

namespace Cheetah.LaserTools
{
    public static class GeneralExtensions
    {
        public static IMyPlayer GetPlayer(this IMyPlayerCollection Players, long ID)
        {
            List<IMyPlayer> player = new List<IMyPlayer>(1);
            Players.GetPlayers(player, x => x.IdentityId == ID);
            return player.FirstOrDefault();
        }

        public static void LogError(this IMyCubeGrid Grid, string Source, Exception Scrap)
        {
            string DisplayName = "";
            try
            {
                DisplayName = Grid.DisplayName;
            }
            finally
            {
                MyAPIGateway.Utilities.ShowMessage(DisplayName, $"Fatal error in '{Source}': {Scrap.Message}. {(Scrap.InnerException != null ? Scrap.InnerException.Message : "No additional info was given by the game :(")}");
            }
        }

        public static void DebugWrite(this IMyCubeGrid Grid, string Source, string Message)
        {
            if (SessionCore.Settings.Debug) MyAPIGateway.Utilities.ShowMessage(Grid.DisplayName, $"Debug message from '{Source}': {Message}");
        }

        public static void Report(this System.Diagnostics.Stopwatch Watch, string Source, string WatchedProcessName, bool UseAsync = false)
        {
            if (!SessionCore.Settings.DebugPerformance) return;
            if (!UseAsync)
                SessionCore.DebugWrite(Source, $"{WatchedProcessName} took {Math.Round(Watch.ElapsedTicks * 1000f / System.Diagnostics.Stopwatch.Frequency, 2)} ms to run", WriteOnlyIfDebug: true);
            else
                SessionCore.DebugAsync(Source, $"{WatchedProcessName} took {Math.Round(Watch.ElapsedTicks * 1000f / System.Diagnostics.Stopwatch.Frequency, 2)} ms to run", WriteOnlyIfDebug: true);
        }
    }
}
