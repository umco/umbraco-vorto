using System.Linq;
using Our.Umbraco.Vorto.Models;

namespace Our.Umbraco.Vorto.Extensions
{
    internal static class VortoValueExtensions
    {
        public static string FindBestMatchCulture(this VortoValue value, string cultureName)
        {
            // Check for actual values
            if (value.Values == null)
                return string.Empty;

            // Check for exact match
            if (value.Values.ContainsKey(cultureName))
                return cultureName;

            // Close match
            return cultureName.Length == 2
                ? value.Values.Keys.FirstOrDefault(x => x.StartsWith(cultureName + "-"))
                : string.Empty;
        }
    }
}
