using System.Collections.Generic;
using VRageMath;

namespace Cheetah.LaserTools
{
    public static class VectorExtensions
    {
        public static float DistanceTo(this Vector3D From, Vector3D To)
        {
            return (float)(To - From).Length();
        }

        public static bool IsNullEmptyOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static Vector3D LineTowards(this Vector3D From, Vector3D To, double Length)
        {
            return From + (Vector3D.Normalize(To - From) * Length);
        }

        public static Vector3D InverseVectorTo(this Vector3D From, Vector3D To, double Length)
        {
            return From + (Vector3D.Normalize(From - To) * Length);
        }

        /// <summary>
        /// Builds a grid of vectors.
        /// </summary>
        /// <param name="HalfHeight">Half-height of the resulting grid, in offsets. E.g. for grid of 5 points this is 4/2=2.</param>
        /// <param name="HalfWidth">Half-width of the resulting grid, in offsets. E.g. for grid of 5 points this is 4/2=2.</param>
        /// <returns></returns>
        public static List<Vector3D> BuildGrid(Vector3D Center, Vector3D UpOffset, Vector3D RightOffset, int HalfHeight, int HalfWidth)
        {
            List<Vector3D> Grid = new List<Vector3D>(((HalfHeight * 2) + 1) * ((HalfWidth * 2) + 1));

            Vector3D LeftBottomCorner = Center + (RightOffset * -1 * HalfWidth) + (UpOffset * -1 * HalfHeight);

            for (int width = 0; width <= HalfWidth * 2; width++)
            {
                for (int height = 0; height <= HalfHeight * 2; height++)
                {
                    Grid.Add(LeftBottomCorner + (UpOffset * height) + (RightOffset * width));
                }
            }

            return Grid;
        }

        /// <summary>
        /// Builds a grid of vectors. Note that this function assumes normalized, 1m length vectors.
        /// </summary>
        /// <param name="HalfHeight">Half-height of the resulting grid, in offsets. E.g. for grid of 5 points this is 4/2=2.</param>
        /// <param name="HalfWidth">Half-width of the resulting grid, in offsets. E.g. for grid of 5 points this is 4/2=2.</param>
        /// <returns></returns>
        public static List<LineD> BuildLineGrid(Vector3D Center, Vector3D ForwardOffset, Vector3D UpOffset, Vector3D RightOffset, float LineLength, int HalfHeight, int HalfWidth)
        {
            List<LineD> Grid = new List<LineD>(((HalfHeight * 2) + 1) * ((HalfWidth * 2) + 1));

            Vector3D LeftBottomCorner = Center + (RightOffset * -1 * HalfWidth) + (UpOffset * -1 * HalfHeight);

            for (int width = 0; width <= HalfWidth * 2; width++)
            {
                for (int height = 0; height <= HalfHeight * 2; height++)
                {
                    Vector3D Point1 = LeftBottomCorner + (UpOffset * height) + (RightOffset * width);
                    Vector3D Point2 = Point1 + (ForwardOffset * LineLength);
                    Grid.Add(new LineD(Point1, Point2));
                }
            }

            return Grid;
        }

        /// <summary>
        /// Builds a grid of lines.
        /// </summary>
        /// <param name="HalfHeight">Half-height of the resulting grid, in offsets. E.g. for grid of 5 points this is 4/2=2.</param>
        /// <param name="HalfWidth">Half-width of the resulting grid, in offsets. E.g. for grid of 5 points this is 4/2=2.</param>
        /// <returns></returns>
        public static List<LineD> BuildLineGrid(Vector3D CenterStart, Vector3D CenterEnd, Vector3D UpOffset, Vector3D RightOffset, int HalfHeight, int HalfWidth)
        {
            List<LineD> Grid = new List<LineD>(((HalfHeight * 2) + 1) * ((HalfWidth * 2) + 1));

            Vector3D LeftBottomCornerStart = CenterStart + (RightOffset * -1 * HalfWidth) + (UpOffset * -1 * HalfHeight);
            Vector3D LeftBottomCornerEnd = CenterEnd + (RightOffset * -1 * HalfWidth) + (UpOffset * -1 * HalfHeight);

            for (int width = 0; width <= HalfWidth * 2; width++)
            {
                for (int height = 0; height <= HalfHeight * 2; height++)
                {
                    Vector3D Point1 = LeftBottomCornerStart + (UpOffset * height) + (RightOffset * width);
                    Vector3D Point2 = LeftBottomCornerEnd + (UpOffset * height) + (RightOffset * width);
                    Grid.Add(new LineD(Point1, Point2));
                }
            }

            return Grid;
        }
    }

}
