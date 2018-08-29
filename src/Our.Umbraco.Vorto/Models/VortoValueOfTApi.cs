using Our.Umbraco.Vorto.Extensions;
using System.Collections.Generic;
using System.Threading;
using Umbraco.Core;

namespace Our.Umbraco.Vorto.Models
{
	public partial class VortoValue<T>
	{
		private bool DoHasValue(string cultureName = null)
		{
			if (Values == null || Values.Count == 0)
				return false;

			if (cultureName == null)
				cultureName = Thread.CurrentThread.CurrentUICulture.Name;

			var bestMatchCultureName = this.FindBestMatchCulture(cultureName);
			if (!bestMatchCultureName.IsNullOrWhiteSpace()
				&& Values.ContainsKey(bestMatchCultureName)
				&& EqualityComparer<T>.Default.Equals(Values[bestMatchCultureName], default(T)))
				return true;

			return false;
		}

		private T DoGetValue(string cultureName = null, T defaultValue = default(T))
		{
			if (cultureName == null)
				cultureName = Thread.CurrentThread.CurrentUICulture.Name;

			// Get the serialized value
			var bestMatchCultureName = this.FindBestMatchCulture(cultureName);
			if (!bestMatchCultureName.IsNullOrWhiteSpace()
				&& Values.ContainsKey(bestMatchCultureName)
				&& Values[bestMatchCultureName] != null
				&& !Values[bestMatchCultureName].ToString().IsNullOrWhiteSpace())
			{
				var value = Values[bestMatchCultureName];
				var attempt = value.TryConvertTo<T>();
				return attempt.Success ? attempt.Result : defaultValue;
			}

			return defaultValue;
		}

		/// <summary>
		/// Returns a value indicating whether the given Vorto model has a value for the given culture
		/// </summary>
		/// <param name="cultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
		/// <param name="fallbackCultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
		/// <returns>The <see cref="bool"/></returns>
		public bool HasValue(string cultureName = null, string fallbackCultureName = null)
		{
			var hasValue = DoHasValue(cultureName);
			if (!hasValue && !string.IsNullOrEmpty(fallbackCultureName) && !fallbackCultureName.Equals(cultureName))
				hasValue = DoHasValue(fallbackCultureName);
			return hasValue;
		}

		/// <summary>
		/// Gets the Vorto value of the given type for the given culture.
		/// </summary>
		/// <typeparam name="T">The type of value to return</typeparam>
		/// <param name="cultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
		/// <param name="defaultValue">The default value to return if none is found. Optional</param>
		/// <param name="fallbackCultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
		/// <returns>The <typeparamref name="T"/> value</returns>
		public T GetValue(string cultureName = null,
			T defaultValue = default(T), string fallbackCultureName = null)
		{
			var result = DoGetValue(cultureName);
			if (EqualityComparer<T>.Default.Equals(result, default(T)) && !string.IsNullOrEmpty(fallbackCultureName) && !fallbackCultureName.Equals(cultureName))
				result = DoGetValue(fallbackCultureName);
			if (EqualityComparer<T>.Default.Equals(result, default(T)))
				result = defaultValue;
			return result;
		}
	}
}
