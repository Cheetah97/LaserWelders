using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Cheetah.LaserTools
{
    public class BeamController : ToolModuleBase
    {
        protected float GridBlockSize => Tool.CubeGrid.GridSize;
        protected Vector3I BlockDimensions => (Tool.SlimBlock.BlockDefinition as MyCubeBlockDefinition).Size;
        protected Vector3D BlockPosition => Tool.GetPosition();
        public int MinBeamLengthBlocks => 1;
        public int MaxBeamLengthBlocks
        {
            get
            {
                if (SessionCore.Settings == null) return Tool.CubeGrid.GridSizeEnum == MyCubeSize.Small ? 30 : 8;
                return Tool.CubeGrid.GridSizeEnum == MyCubeSize.Small ? SessionCore.Settings.MaxBeamLengthBlocksSmall : SessionCore.Settings.MaxBeamLengthBlocksLarge;
            }
        }
        public float MinBeamLengthM => MinBeamLengthBlocks * GridBlockSize;
        public float MaxBeamLengthM => MaxBeamLengthBlocks * GridBlockSize;
        Vector3D BlockForwardEnd => Tool.WorldMatrix.Forward * GridBlockSize * (BlockDimensions.Z) / 2;
        Vector3 LaserEmitterPosition
        {
            get
            {
                var EmitterDummy = Tool.Model.GetDummy("Laser_Emitter");
                return EmitterDummy != null ? EmitterDummy.Matrix.Translation : (Vector3)BlockForwardEnd;
            }
        }
        public Vector3D BeamStart => BlockPosition + LaserEmitterPosition;
        public Vector3D BeamEnd => BeamStart + Tool.WorldMatrix.Forward * ToolComp.TermModule.BeamLength * GridBlockSize * ToolComp.PowerModule.SuppliedPowerRatio;

        public BeamController(LaserToolBase ToolComp) : base(ToolComp) { }

        public void DrawBeam()
        {
            if (MyAPIGateway.Session.Player == null) return;
            var Internal = BeamColors.InternalBeamColor.ToVector4();
            var External = Vector4.Zero;
            if (ToolComp.IsWelder) External = BeamColors.ExternalWeldBeamColor.ToVector4();
            if (ToolComp.IsGrinder) External = BeamColors.ExternalGrindBeamColor.ToVector4();
            if (ToolComp.IsDrill) External = BeamColors.ExternalDrillBeamColor.ToVector4();
            var BeamStart = this.BeamStart;
            var BeamEnd = this.BeamEnd;
            MySimpleObjectDraw.DrawLine(BeamStart, BeamEnd, MyStringId.GetOrCompute("WeaponLaser"), ref Internal, 0.1f);
            MySimpleObjectDraw.DrawLine(BeamStart, BeamEnd, MyStringId.GetOrCompute("WeaponLaser"), ref External, 0.2f);
        }
    }

    public static class BeamColors
    {
        public static Color InternalBeamColor { get; } = Color.WhiteSmoke;
        public static Color ExternalWeldBeamColor { get; } = Color.DeepSkyBlue;
        public static Color ExternalGrindBeamColor { get; } = Color.IndianRed;
        public static Color ExternalDrillBeamColor { get; } = Color.Gold;
    }
}
