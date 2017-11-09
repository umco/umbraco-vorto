using System.Threading;
using Our.Umbraco.Vorto.Helpers;
using Our.Umbraco.Vorto.Models;
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
            var vortoModel = content.GetVortoModel(propertyAlias);

            if (vortoModel != null && vortoModel.Values != null)
            {
                var bestMatchCultureName = vortoModel.FindBestMatchCulture(cultureName);
                if (!bestMatchCultureName.IsNullOrWhiteSpace()
                    && vortoModel.Values.ContainsKey(bestMatchCultureName)
                    && vortoModel.Values[bestMatchCultureName] != null
                    && !vortoModel.Values[bestMatchCultureName].ToString().IsNullOrWhiteSpace())
                    return true;
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
        /// Determines if the given property alias has a vorto value.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="propertyAlias"></param>
        /// <param name="cultureName"></param>
        /// <param name="recursive">A value indicating whether to navigate the tree upwards until a property with a value is found.</param>
        /// <param name="fallbackCultureName"></param>
        public static bool HasVortoValue(this IPublishedContent content, string propertyAlias,
            string cultureName = null, bool recursive = false, 
            string fallbackCultureName = null)
        {
            var hasValue = content.DoHasVortoValue(propertyAlias, cultureName, recursive);
            if (!hasValue && !string.IsNullOrEmpty(fallbackCultureName) && !fallbackCultureName.Equals(cultureName))
                hasValue = content.DoHasVortoValue(propertyAlias, fallbackCultureName, recursive);
            return hasValue;
        }

        /// <summary>
        /// Determines if the given property is of type <see cref="VortoValue"/>.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="propertyAlias"></param>
        public static bool IsVortoProperty(this IPublishedContent content, string propertyAlias)
        {
            return content.GetVortoModel(propertyAlias) != null ? true : false;
        }

        #endregion

        #region GetValue

        private static VortoValue GetVortoModel(this IPublishedContent content, string propertyAlias)
        {
            if (content.HasValue(propertyAlias))
            {
                var prop = content.GetProperty(propertyAlias);
                if (prop.Value is VortoValue) return prop.Value as VortoValue;
            }

            return null;
        }

        private static T DoInnerGetVortoValue<T>(this IPublishedContent content, string propertyAlias, string cultureName = null,
            bool recursive = false, T defaultValue = default(T))
        {
            var vortoModel = content.GetVortoModel(propertyAlias);

            if (vortoModel != null && vortoModel.Values != null)
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

                    var inPreviewMode = UmbracoContext.Current.InPreviewMode;

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

        public static T GetVortoValue<T>(this IPublishedContent content, string propertyAlias, string cultureName = null,
            bool recursive = false, T defaultValue = default(T), string fallbackCultureName = null)
        {
            var result = content.DoGetVortoValue<T>(propertyAlias, cultureName, recursive, default(T));
            if (result == null && !string.IsNullOrEmpty(fallbackCultureName) && !fallbackCultureName.Equals(cultureName))
                result = content.DoGetVortoValue<T>(propertyAlias, fallbackCultureName, recursive, defaultValue);
             
            return result;
        }

        public static object GetVortoValue(this IPublishedContent content, string propertyAlias, string cultureName = null,
            bool recursive = false, object defaultValue = null,
            string fallbackCultureName = null)
        {
            return content.GetVortoValue<object>(propertyAlias, cultureName, recursive, defaultValue, fallbackCultureName);
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
