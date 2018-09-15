using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Cheetah.LaserTools
{
    public class EntityByDistanceSorter : IComparer<IMyEntity>, IComparer<IMySlimBlock>
    {
        public Vector3D Position { get; set; }
        public EntityByDistanceSorter(Vector3D Position)
        {
            this.Position = Position;
        }

        public int Compare(IMyEntity x, IMyEntity y)
        {
            var DistanceX = Vector3D.DistanceSquared(Position, x.GetPosition());
            var DistanceY = Vector3D.DistanceSquared(Position, y.GetPosition());

            if (DistanceX < DistanceY) return -1;
            if (DistanceX > DistanceY) return 1;
            return 0;
        }

        public int Compare(IMySlimBlock x, IMySlimBlock y)
        {
            var DistanceX = Vector3D.DistanceSquared(Position, x.CubeGrid.GridIntegerToWorld(x.Position));
            var DistanceY = Vector3D.DistanceSquared(Position, y.CubeGrid.GridIntegerToWorld(y.Position));

            if (DistanceX < DistanceY) return -1;
            if (DistanceX > DistanceY) return 1;
            return 0;
        }
    }
}
