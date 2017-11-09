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
            var vortoModel = prop.Value as VortoValue;
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

                    // Get target datatype
                    var targetDataType = VortoHelper.GetTargetDataTypeDefinition(vortoModel.DtdGuid);

                    // Umbraco has the concept of a IPropertyEditorValueConverter which it 
                    // also queries for property resolvers. However I'm not sure what these
                    // are for, nor can I find any implementations in core, so am currently
                    // just ignoring these when looking up converters.
                    // NB: IPropertyEditorValueConverter not to be confused with
                    // IPropertyValueConverter which are the ones most people are creating
                    var properyType = CreateDummyPropertyType(
                        targetDataType.Id,
                        targetDataType.PropertyEditorAlias,
                        content.ContentType);

                    var inPreviewMode = UmbracoContext.Current != null && UmbracoContext.Current.InPreviewMode;

                    // Try convert data to source
                    // We try this first as the value is stored as JSON not
                    // as XML as would occur in the XML cache as in the act
                    // of converting to XML this would ordinarily get called
                    // but with JSON it doesn't, so we try this first
                    var converted1 = properyType.ConvertDataToSource(value, inPreviewMode);
                    if (converted1 is T) return (T)converted1;

                    var convertAttempt = converted1.TryConvertTo<T>();
                    if (convertAttempt.Success) return convertAttempt.Result;

                    // Try convert source to object
                    // If the source value isn't right, try converting to object
                    var converted2 = properyType.ConvertSourceToObject(converted1, inPreviewMode);
                    if (converted2 is T) return (T)converted2;

                    convertAttempt = converted2.TryConvertTo<T>();
                    if (convertAttempt.Success) return convertAttempt.Result;

                    // Try just converting
                    convertAttempt = value.TryConvertTo<T>();
                    if (convertAttempt.Success) return convertAttempt.Result;

                    // Still not right type so return default value
                    return defaultValue;
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
        /// <param name="cultureName">The culture name in the format languagecode2-country/regioncode2</param>
        /// <param name="recursive">Whether to recursively travel up the content tree looking for the value</param>
        /// <param name="defaultValue">The default value to return if none is found</param>
        /// <param name="fallbackCultureName">The culture name in the format languagecode2-country/regioncode2. Optional</param>
        /// <returns>The <typeparamref name="T"/> value</returns>
        public static T GetVortoValue<T>(this IPublishedContent content, string propertyAlias, string cultureName = null,
            bool recursive = false, T defaultValue = default(T), string fallbackCultureName = null)
        {
            var result = content.DoGetVortoValue<T>(propertyAlias, cultureName, recursive, default(T));
            if (result == null && !string.IsNullOrEmpty(fallbackCultureName) && !fallbackCultureName.Equals(cultureName))
                result = content.DoGetVortoValue<T>(propertyAlias, fallbackCultureName, recursive, defaultValue);

            return result;
        }

	    /// <summary>
	    /// Gets the Vorto value for the given content property.
	    /// </summary>
	    /// <param name="content">The cached content</param>
	    /// <param name="propertyAlias">The property alias</param>
	    /// <param name="cultureName">The culture name in the format languagecode2-country/regioncode2</param>
	    /// <param name="recursive">Whether to recursively travel up the content tree looking for the value</param>
	    /// <param name="defaultValue">The default value to return if none is found</param>
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

        private static PublishedPropertyType CreateDummyPropertyType(int dataTypeId, string propertyEditorAlias, PublishedContentType contentType)
		{
            return new PublishedPropertyType(contentType,
				new PropertyType(new DataTypeDefinition(-1, propertyEditorAlias)
				{
					Id = dataTypeId
				}));
		}
	}
}
