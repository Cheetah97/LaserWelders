using Cheetah.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cheetah.LaserTools
{
    public class ToolTermCtl : ToolModuleBase
    {
        #region Syncers and accessors
        Syncer<bool> SyncDistanceMode;
        public bool DistanceMode
        {
            get { return SyncDistanceMode.Get(); }
            set { SyncDistanceMode.Set(value); }
        }
        Syncer<float> SyncBeamLength;
        public int BeamLength
        {
            get
            {
                if (ToolComp.IsDrill) return 10;
                return (int)SyncBeamLength.Get();
            }
            set { if (!ToolComp.IsDrill) SyncBeamLength.Set(value); }
        }
        Syncer<float> SyncSpeedMultiplier;
        public int SpeedMultiplier
        {
            get { return (int)SyncSpeedMultiplier.Get(); }
            set { SyncSpeedMultiplier.Set(value); }
        }
        #endregion

        public ToolTermCtl(LaserToolBase ToolComp) : base(ToolComp) { }

        public override void Init()
        {
            SyncBeamLength = new Syncer<float>(ToolComp.Tool, "BeamLength", 1, Checker: val => val >= ToolComp.BeamCtlModule.MinBeamLengthBlocks && val <= ToolComp.BeamCtlModule.MaxBeamLengthBlocks);
            SyncDistanceMode = new Syncer<bool>(ToolComp.Tool, "DistanceBasedMode");
            SyncSpeedMultiplier = new Syncer<float>(ToolComp.Tool, "SpeedMultiplier", 1, Checker: val => val >= 1 && val <= 4);
            SyncBeamLength.GotValueFromServer += ToolComp.Tool.UpdateVisual;
            SyncDistanceMode.GotValueFromServer += ToolComp.Tool.UpdateVisual;
            SyncSpeedMultiplier.GotValueFromServer += ToolComp.Tool.UpdateVisual;
        }

        public void LoadSyncersFromServer()
        {
            SyncBeamLength.Ask();
            SyncDistanceMode.Ask();
            SyncSpeedMultiplier.Ask();
        }

        public override void Close()
        {
            SyncBeamLength.GotValueFromServer -= ToolComp.Tool.UpdateVisual;
            SyncDistanceMode.GotValueFromServer -= ToolComp.Tool.UpdateVisual;
            SyncSpeedMultiplier.GotValueFromServer -= ToolComp.Tool.UpdateVisual;
            SyncBeamLength.Close();
            SyncDistanceMode.Close();
            SyncSpeedMultiplier.Close();
        }
    }
}
