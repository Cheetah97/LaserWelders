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
    public abstract class ToolModuleBase
    {
        public LaserToolBase ToolComp { get; private set; }
        public IMyShipToolBase Tool => ToolComp.Tool;

        public ToolModuleBase(LaserToolBase ToolComp)
        {
            this.ToolComp = ToolComp;
        }

        /// <summary>
        /// Empty by default
        /// </summary>
        public virtual void Init() { }
        /// <summary>
        /// Empty by default
        /// </summary>
        public virtual void Close() { }
    }
}
