using System.Text;

namespace Cheetah.LaserTools
{
    public static class StringExtensions
    {
        public static StringBuilder RemoveTrailingNewlines(this StringBuilder Builder)
        {
            while (Builder.Length > 0 && char.IsWhiteSpace(Builder[Builder.Length - 1])) Builder.Length--;
            return Builder;
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (maxLength < 1) return "";
            if (string.IsNullOrEmpty(value)) return value;
            if (value.Length <= maxLength) return value;

            return value.Substring(0, maxLength);
        }

        public static VRage.Utils.MyStringHash ToMyStringHash(this string value)
        {
            return VRage.Utils.MyStringHash.GetOrCompute(value);
        }
    }

}
