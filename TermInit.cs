using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Cheetah.LaserTools
{
    public static class Terminals
    {
        public static bool InitedWelderControls { get; private set; } = false;
        public static void InitWelderControls()
        {
            if (InitedWelderControls) return;

            MyAPIGateway.TerminalControls.AddControl<IMyShipWelder>(Controls.LaserBeam<IMyShipWelder>());
            MyAPIGateway.TerminalControls.AddControl<IMyShipWelder>(Controls.SpeedMultiplier<IMyShipWelder>());
            var DistanceMode = Controls.DistanceMode<IMyShipWelder>();
            DistanceMode.Title = MyStringId.GetOrCompute("Weld Furthest First");
            DistanceMode.Tooltip = MyStringId.GetOrCompute($"If enabled, Laser Welder will build furthest block first before proceeding on new one.");
            MyAPIGateway.TerminalControls.AddControl<IMyShipWelder>(DistanceMode);

            InitedWelderControls = true;
        }

        public static bool InitedGrinderControls { get; private set; } = false;
        public static void InitGrinderControls()
        {
            if (InitedGrinderControls) return;

            MyAPIGateway.TerminalControls.AddControl<IMyShipGrinder>(Controls.LaserBeam<IMyShipGrinder>());
            MyAPIGateway.TerminalControls.AddControl<IMyShipGrinder>(Controls.SpeedMultiplier<IMyShipGrinder>());
            var DistanceMode = Controls.DistanceMode<IMyShipGrinder>();
            DistanceMode.Title = MyStringId.GetOrCompute("Grind Closest First");
            DistanceMode.Tooltip = MyStringId.GetOrCompute($"If enabled, Laser Grinder will dismantle closest block first before proceeding on new one.");
            MyAPIGateway.TerminalControls.AddControl<IMyShipGrinder>(DistanceMode);

            InitedGrinderControls = true;
        }

        public static bool InitedDrillControls { get; private set; } = false;
        public static void InitDrillControls()
        {
            if (InitedDrillControls) return;

            MyAPIGateway.TerminalControls.AddControl<IMyShipDrill>(Controls.HarvestEfficiency());

            InitedDrillControls = true;
        }
    }
}
