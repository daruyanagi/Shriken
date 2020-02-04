using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shuriken
{
    public static class NumberExtension
    {
        public static string ToReadableSize(this double size, int scale = 0, int standard = 1024)
        {
            var unit = new[] { "B", "KB", "MB", "GB" };

            if (scale == unit.Length - 1 || size <= standard)
            {
                return $"{size.ToString(".##")} {unit[scale]}";
            }

            return ToReadableSize(size / standard, scale + 1, standard);
        }
        public static string ToReadableSize(this ulong size, int scale = 0, ulong standard = 1024)
        {
            var unit = new[] { "B", "KB", "MB", "GB" };

            if (scale == unit.Length - 1 || size <= standard)
            {
                return $"{size.ToString(".##")} {unit[scale]}";
            }

            return ToReadableSize(size / standard, scale + 1, standard);
        }
    }
}
