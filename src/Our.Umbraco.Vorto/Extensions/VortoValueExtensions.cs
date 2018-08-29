using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Our.Umbraco.Vorto.Models;
using Umbraco.Core;

namespace Our.Umbraco.Vorto.Extensions
{
    public static class VortoValueExtensions
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
                ? value.Values.Keys.FirstOrDefault(x => x.StartsWith(cultureName + "-"))
                : string.Empty;
        }

		private static bool DoHasValue<T>(this VortoValue<T> vortoValue, string cultureName = null)
		{
			if (vortoValue == null || vortoValue.Values == null || vortoValue.Values.Count == 0)
				return false;

			if (cultureName == null)
				cultureName = Thread.CurrentThread.CurrentUICulture.Name;

			var bestMatchCultureName = vortoValue.FindBestMatchCulture(cultureName);
			if (!bestMatchCultureName.IsNullOrWhiteSpace()
				&& vortoValue.Values.ContainsKey(bestMatchCultureName)
				&& EqualityComparer<T>.Default.Equals(vortoValue.Values[bestMatchCultureName], default(T)))
				return true;

			return false;
		}

		private static T DoGetValue<T>(this VortoValue<T> vortoValue, string cultureName = null, T defaultValue = default(T))
		{
			if (cultureName == null)
				cultureName = Thread.CurrentThread.CurrentUICulture.Name;

			// Get the serialized value
			var bestMatchCultureName = vortoValue.FindBestMatchCulture(cultureName);
			if (!bestMatchCultureName.IsNullOrWhiteSpace()
				&& vortoValue.Values.ContainsKey(bestMatchCultureName)
				&& vortoValue.Values[bestMatchCultureName] != null
				&& !vortoValue.Values[bestMatchCultureName].ToString().IsNullOrWhiteSpace())
			{
				var value = vortoValue.Values[bestMatchCultureName];
				var attempt = value.TryConvertTo<T>();
				return attempt.Success ? attempt.Result : defaultValue;
			}

			return defaultValue;
		}

		/// <summary>
		/// Returns a value indicating whether the given Vorto model has a value for the given culture
		/// </summary>
		/// <param name="vortoValue">The vorto value model</param>
		/// <param name="cultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
		/// <param name="fallbackCultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
		/// <returns>The <see cref="bool"/></returns>
		public static bool HasValue<T>(this VortoValue<T> vortoValue, string cultureName = null, string fallbackCultureName = null)
		{
			var hasValue = vortoValue.DoHasValue(cultureName);
			if (!hasValue && !string.IsNullOrEmpty(fallbackCultureName) && !fallbackCultureName.Equals(cultureName))
				hasValue = vortoValue.DoHasValue(fallbackCultureName);
			return hasValue;
		}

		/// <summary>
		/// Gets the Vorto value of the given type for the given culture.
		/// </summary>
		/// <typeparam name="T">The type of value to return</typeparam>
		/// <param name="vortoValue">The vorto value model</param>
		/// <param name="cultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
		/// <param name="defaultValue">The default value to return if none is found. Optional</param>
		/// <param name="fallbackCultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
		/// <returns>The <typeparamref name="T"/> value</returns>
		public static T GetValue<T>(this VortoValue<T> vortoValue, string cultureName = null,
			T defaultValue = default(T), string fallbackCultureName = null)
		{
			var result = vortoValue.DoGetValue<T>(cultureName);
			if (EqualityComparer<T>.Default.Equals(result, default(T)) && !string.IsNullOrEmpty(fallbackCultureName) && !fallbackCultureName.Equals(cultureName))
				result = vortoValue.DoGetValue<T>(fallbackCultureName);
			if (EqualityComparer<T>.Default.Equals(result, default(T)))
				result = defaultValue;
			return result;
		}

	}
}
