﻿namespace RatioMaster.Core
{
    using System;
    using System.Globalization;

    public static class StringExtensions
    {
        internal static double ParseDouble(this string inputString, double defVal)
        {
            if (string.IsNullOrWhiteSpace(inputString))
            {
                return defVal;
            }
            inputString = inputString.Replace(",", ".");
            double readValue;
            if (double.TryParse(inputString, NumberStyles.Float & NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat, out readValue))
            {
                return readValue;
            }
            return defVal;
        }

        internal static int ParseValidInt(this string inputString, int defVal)
        {
            int readValue;
            if (int.TryParse(inputString, out readValue))
            {
                return readValue;
            }
            return defVal;
        }

        internal static long ParseValidInt64(this string inputString, long defaultValue)
        {
            long readValue;
            if (long.TryParse(inputString, out readValue))
            {
                return readValue;
            }
            return defaultValue;
        }

        internal static float ParseValidFloat(this string inputString, float defaultValue)
        {
            if (string.IsNullOrWhiteSpace(inputString))
            {
                return defaultValue;
            }
            inputString = inputString.Replace(",", ".");
            float readValue;
            if (float.TryParse(inputString, NumberStyles.Float & NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat, out readValue))
            {
                return readValue;
            }
            return defaultValue;
        }

        internal static string GetValueDefault(this string inputString, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(inputString))
            {
                return defaultValue;
            }

            return inputString;
        }
    }
}