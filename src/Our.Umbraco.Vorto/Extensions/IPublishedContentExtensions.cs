using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using Our.Umbraco.Vorto.Helpers;
using Our.Umbraco.Vorto.Models;
using Our.Umbraco.Vorto.Web.PropertyEditors;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;

namespace Our.Umbraco.Vorto.Extensions
{
	public static class IPublishedContentExtensions
	{
        #region HasValue

        private static bool DoInnerHasVortoValue(this IPublishedContent content, string propertyAlias,
            string cultureName = null, bool recursive = false)
	    {
            if (content.HasValue(propertyAlias))
            {
                var prop = content.GetProperty(propertyAlias);
				if (prop == null)
				{
					return false;
				}

                var dataValue = prop.DataValue;
                if (dataValue == null)
                {
                    return false;
                }

                VortoValue vortoModel;

                try
                {
                    // We purposfully parse the raw data value ourselves bypassing the property
                    // value converters so that we don't require an UmbracoContext during a
                    // HasValue check. As we won't actually use the value, this is ok here. 
                    vortoModel = JsonConvert.DeserializeObject<VortoValue>(dataValue.ToString());
                }
                catch
                {
                    return false;
                }

                if (vortoModel?.Values != null)
                {
                    var bestMatchCultureName = vortoModel.FindBestMatchCulture(cultureName);
                    if (!bestMatchCultureName.IsNullOrWhiteSpace()
                        && vortoModel.Values.ContainsKey(bestMatchCultureName)
                        && vortoModel.Values[bestMatchCultureName] != null
                        && !vortoModel.Values[bestMatchCultureName].ToString().IsNullOrWhiteSpace())
                        return true;
                }
            }

            return recursive && content.Parent != null
                ? content.Parent.DoInnerHasVortoValue(propertyAlias, cultureName, recursive)
                : false;
	    }

		private static bool DoHasVortoValue(this IPublishedContent content, string propertyAlias,
            string cultureName = null, bool recursive = false)
		{
			if (cultureName == null)
				cultureName = Thread.CurrentThread.CurrentUICulture.Name;

			if (!content.HasValue(propertyAlias, recursive))
				return false;

		    return content.DoInnerHasVortoValue(propertyAlias, cultureName, recursive);
		}

        /// <summary>
        /// Returns a value indicating whether the given content property has a Vorto value
        /// </summary>
        /// <param name="content">The cached content</param>
        /// <param name="propertyAlias">The property alias</param>
        /// <param name="cultureName">The culture name in the format languagecode2-country/regioncode2</param>
        /// <param name="recursive">Whether to recursively travel up the content tree looking for the value</param>
        /// <param name="fallbackCultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
        /// <returns>The <see cref="bool"/></returns>
        public static bool HasVortoValue(this IPublishedContent content, string propertyAlias,
            string cultureName = null, bool recursive = false,
            string fallbackCultureName = null)
        {
            var hasValue = content.DoHasVortoValue(propertyAlias, cultureName, recursive);
            if (!hasValue && !string.IsNullOrEmpty(fallbackCultureName) && !fallbackCultureName.Equals(cultureName))
                hasValue = content.DoHasVortoValue(propertyAlias, fallbackCultureName, recursive);
            return hasValue;
        }

        #endregion

        #region GetValue

        private static T DoInnerGetVortoValue<T>(this IPublishedContent content, string propertyAlias, string cultureName = null,
            bool recursive = false, T defaultValue = default(T))
        {
            var prop = content.GetProperty(propertyAlias);
			if (prop == null)
			{
				// PR #100 - Prevent generation of NullReferenceException: allow return of defaultValue or traversal up the tree if prop is null
				return defaultValue;
			}

            var vortoModel = prop.Value as VortoValue<T>;
            if (vortoModel?.Values != null)
            {
                // Get the serialized value
                var bestMatchCultureName = vortoModel.FindBestMatchCulture(cultureName);
                if (!bestMatchCultureName.IsNullOrWhiteSpace()
                    && vortoModel.Values.ContainsKey(bestMatchCultureName)
                    && vortoModel.Values[bestMatchCultureName] != null
                    && !vortoModel.Values[bestMatchCultureName].ToString().IsNullOrWhiteSpace())
                {
                    var value = vortoModel.Values[bestMatchCultureName];
					var attempt = value.TryConvertTo<T>();
					return attempt.Success ? attempt.Result : defaultValue;
                }
            }

            return recursive && content.Parent != null
                ? content.Parent.DoInnerGetVortoValue<T>(propertyAlias, cultureName, recursive, defaultValue)
                : defaultValue;
        }

		private static T DoGetVortoValue<T>(this IPublishedContent content, string propertyAlias, string cultureName = null,
            bool recursive = false, T defaultValue = default(T))
		{
			if (cultureName == null)
				cultureName = Thread.CurrentThread.CurrentUICulture.Name;

		    return content.DoInnerGetVortoValue(propertyAlias, cultureName, recursive, defaultValue);
		}

		/// <summary>
		/// Gets the Vorto value for the given content property as the given type.
		/// </summary>
		/// <typeparam name="T">The type of value to return</typeparam>
		/// <param name="content">The cached content</param>
		/// <param name="propertyAlias">The property alias</param>
		/// <param name="cultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
		/// <param name="recursive">Whether to recursively travel up the content tree looking for the value. Optional</param>
		/// <param name="defaultValue">The default value to return if none is found. Optional</param>
		/// <param name="fallbackCultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
		/// <returns>The <typeparamref name="T"/> value</returns>
		public static T GetVortoValue<T>(this IPublishedContent content, string propertyAlias, string cultureName = null,
            bool recursive = false, T defaultValue = default(T), string fallbackCultureName = null)
        {
            var result = content.DoGetVortoValue<T>(propertyAlias, cultureName, recursive);
            if (EqualityComparer<T>.Default.Equals(result, default(T)) && !string.IsNullOrEmpty(fallbackCultureName) && !fallbackCultureName.Equals(cultureName))
                result = content.DoGetVortoValue<T>(propertyAlias, fallbackCultureName, recursive);
			if (EqualityComparer<T>.Default.Equals(result, default(T)))
				result = defaultValue;

            return result;
        }

		/// <summary>
		/// Gets the Vorto value for the given content property.
		/// </summary>
		/// <param name="content">The cached content</param>
		/// <param name="propertyAlias">The property alias</param>
		/// <param name="cultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
		/// <param name="recursive">Whether to recursively travel up the content tree looking for the value. Optional</param>
		/// <param name="defaultValue">The default value to return if none is found. Optional</param>
		/// <param name="fallbackCultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
		/// <returns>The <see cref="object"/> value</returns>
		public static object GetVortoValue(this IPublishedContent content, string propertyAlias, string cultureName = null,
            bool recursive = false, object defaultValue = null,
            string fallbackCultureName = null)
        {
            return content.GetVortoValue<object>(propertyAlias, cultureName, recursive, defaultValue, fallbackCultureName);
        }

        #endregion

        #region IsVortoProperty

        /// <summary>
        /// Determines if the given property is a Vorto based property.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="propertyAlias"></param>
        public static bool IsVortoProperty(this IPublishedContent content, string propertyAlias)
        {
            var propertyType = content.ContentType?.GetPropertyType(propertyAlias);
            return propertyType?.PropertyEditorAlias.InvariantEquals(VortoPropertyEditor.PropertyEditorAlias) ?? false;
        }

        #endregion
	}
}
