using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
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
    public class ToolPowerModule : ToolModuleBase
    {
        MyResourceSinkComponent MyPowerSink => Tool.ResourceSink as MyResourceSinkComponent;
        public float SuppliedPowerRatio => Tool.ResourceSink.SuppliedRatioByType(Electricity);
        static MyDefinitionId Electricity { get; } = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
        public float GridAvailablePower { get; private set; }
        public bool HasEnoughPower => GridAvailablePower > PowerConsumptionFunc(true);

        public ToolPowerModule(LaserToolBase ToolComp) : base(ToolComp) { }

        public void SetPowerUsage()
        {
            MyPowerSink.SetRequiredInputByType(Electricity, PowerConsumptionFunc());
        }

        public void UpdateAvailablePower()
        {
            GridAvailablePower = Tool.CubeGrid.GetMaxPowerOutput();
        }

        public float PowerConsumptionFunc(bool Test = false)
        {
            try
            {
                if (!Test && !Tool.IsToolWorking()) return 0;
                if (ToolComp.IsDrill)
                {
                    return 10 * SessionCore.Settings.PowerMultiplier;
                }
                else
                {
                    if (ToolComp.TermModule.SpeedMultiplier <= 1)
                        return (float)Math.Pow(SessionCore.Settings.PowerScaleMultiplier, ToolComp.TermModule.BeamLength * Tool.CubeGrid.GridSize) * SessionCore.Settings.PowerMultiplier;
                    else
                        return (float)(Math.Pow(SessionCore.Settings.PowerScaleMultiplier, ToolComp.TermModule.BeamLength * Tool.CubeGrid.GridSize) + ((float)Math.Pow(SessionCore.Settings.PowerScaleMultiplier, ToolComp.TermModule.BeamLength * Tool.CubeGrid.GridSize) * ToolComp.TermModule.SpeedMultiplier - 1 * 0.8f)) * SessionCore.Settings.PowerMultiplier;
                }
            }
            catch
            {
                return 0;
            }
        }
    }
}
