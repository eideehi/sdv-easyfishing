using System;
using System.Globalization;

namespace EideeEasyFishing
{
    internal static class PanelText
    {
        // Truncate only the name, keeping its probability visible and preserving whole Unicode
        // text elements (including emoji and combining characters).
        public static string FitWithSuffix(string name, string suffix, float maxWidth, Func<string, float> measure)
        {
            if (measure(name + suffix) <= maxWidth) return name + suffix;

            const string ellipsis = "…";
            if (measure(ellipsis + suffix) > maxWidth) return suffix.TrimStart();
            var starts = StringInfo.ParseCombiningCharacters(name);
            int low = 0, high = starts.Length;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                int length = middle < starts.Length ? starts[middle] : name.Length;
                if (measure(name.Substring(0, length) + ellipsis + suffix) <= maxWidth)
                    low = middle;
                else
                    high = middle - 1;
            }
            int end = low < starts.Length ? starts[low] : name.Length;
            return name.Substring(0, end) + ellipsis + suffix;
        }
    }
}
