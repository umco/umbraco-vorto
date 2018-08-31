using System.Linq;
using Umbraco.Core;
using Our.Umbraco.Vorto.Models;

namespace Our.Umbraco.Vorto.Extensions
{
	internal static class VortoValueExtensions
    {
        internal static string FindBestMatchCulture<T>(this VortoValue<T> value, string cultureName)
        {
            // Check for actual values
            if (value.Values == null)
                return string.Empty;

            // Check for exact match
            if (value.Values.ContainsKey(cultureName))
                return cultureName;

            // Close match
            return cultureName.Length == 2
                ? value.Values.Keys.FirstOrDefault(x => x.InvariantStartsWith(cultureName + "-"))
                : string.Empty;
        }
	}
}
