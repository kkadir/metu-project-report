using System;

namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Utility methods
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        ///     Returns string representation of the array.
        /// </summary>
        /// <param name="list">Array to be converted</param>
        /// <returns>A System.String object representing given array</returns>
        internal static string ArrayToString(Array list)
        {
            if (list == null || list.Length == 0)
            {
                return "[]";
            }
            var values = new string[list.Length];
            for (var i = 0; i < list.Length; i++)
            {
                var obj = list.GetValue(i);
                values[i] = obj == null
                    ? null
                    : obj.GetType().IsArray ? ArrayToString(obj as Array) : obj.ToString();
            }
            return "[" + string.Join("; ", values) + "]";
        }

        /// <summary>
        ///     Compares to arrays for values equality.
        /// </summary>
        /// <returns>true if both arrays contain the same values, false otherwise</returns>
        internal static bool ArrayEquals(Array l, Array r)
        {
            if (l == null && r == null)
            {
                return true;
            }

            if (l == null || r == null)
            {
                return false;
            }

            if (l.GetType() != r.GetType())
            {
                return false;
            }

            if (l.Length != r.Length)
            {
                return false;
            }

            for (var i = 0; i < l.Length; i++)
            {
                var lv = l.GetValue(i);
                var rv = r.GetValue(i);

                if (lv == rv || lv == null && rv == null)
                {
                    continue;
                }

                if (lv == null || rv == null)
                {
                    return false;
                }

                if (lv.GetType().IsArray)
                {
                    if (!ArrayEquals(lv as Array, rv as Array))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!lv.Equals(rv))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal delegate long ByteConverter(byte[] buffer, int startIndex, int bytesToConvert);

        internal static long LittleEndianByteConverter(byte[] buffer, int startIndex, int bytesToConvert)
        {
            var result = 0L;

            for (var i = 0; i < bytesToConvert; i++)
            {
                result = unchecked((result << 8) | buffer[startIndex + bytesToConvert - 1 - i]);
            }

            return result;
        }

        internal static long BigEndianByteConverter(byte[] buffer, int startIndex, int bytesToConvert)
        {
            var result = 0L;

            for (var i = 0; i < bytesToConvert; i++)
            {
                result = unchecked((result << 8) | buffer[startIndex + i]);
            }

            return result;
        }
    }
}