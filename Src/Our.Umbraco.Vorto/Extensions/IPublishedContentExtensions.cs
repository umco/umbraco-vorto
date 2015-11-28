using System.Linq;
using System.Threading;
using Our.Umbraco.Vorto.Helpers;
using Our.Umbraco.Vorto.Models;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
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
                var vortoModel = prop.Value as VortoValue;
                if (vortoModel != null)
                {
                    var bestMatchCultureName = vortoModel.FindBestMatchCulture(cultureName);
                    if (!bestMatchCultureName.IsNullOrWhiteSpace() 
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
            if (content.HasValue(propertyAlias))
            {
                var prop = content.GetProperty(propertyAlias);
                var vortoModel = prop.Value as VortoValue;
                if (vortoModel != null)
                {
                    // Get the serialized value
                    var bestMatchCultureName = vortoModel.FindBestMatchCulture(cultureName);
                    var value = vortoModel.Values[bestMatchCultureName];

                    if (value != null && !value.ToString().IsNullOrWhiteSpace())
                    {
                        // Get target datatype
                        var targetDataType = VortoHelper.GetTargetDataTypeDefinition(vortoModel.DtdGuid);

                        // Umbraco has the concept of a IPropertyEditorValueConverter which it 
                        // also queries for property resolvers. However I'm not sure what these
                        // are for, nor can I find any implementations in core, so am currently
                        // just ignoring these when looking up converters.
                        // NB: IPropertyEditorValueConverter not to be confused with
                        // IPropertyValueConverter which are the ones most people are creating
                        var properyType = CreateDummyPropertyType(targetDataType.Id, targetDataType.PropertyEditorAlias, content.ContentType);
                        var converters = PropertyValueConvertersResolver.Current.Converters.ToArray();

                        // In umbraco, there are default value converters that try to convert the 
                        // value if all else fails. The problem is, they are also in the list of
                        // converters, and the means for filtering these out is internal, so
                        // we currently have to try ALL converters to see if they can convert
                        // rather than just finding the most appropreate. If the ability to filter
                        // out default value converters becomes public, the following logic could
                        // and probably should be changed.
                        foreach (var converter in converters.Where(x => x.IsConverter(properyType)))
                        {
                            // Convert the type using a found value converter
                            var value2 = converter.ConvertDataToSource(properyType, value, false);

                            // If the value is of type T, just return it
                            if (value2 is T)
                                return (T)value2;

                            // Value is not final value type, so try a regular type conversion aswell
                            var convertAttempt = value2.TryConvertTo<T>();
                            if (convertAttempt.Success)
                                return convertAttempt.Result;

                            // If ConvertDataToSource failed try ConvertSourceToObject.
                            var value3 = converter.ConvertSourceToObject(properyType, value, false);

                            // If the value is of type T, just return it
                            if (value3 is T)
                                return (T)value3;

                            // Value is not final value type, so try a regular type conversion aswell
                            var convertAttempt2 = value3.TryConvertTo<T>();
                            if (convertAttempt2.Success)
                                return convertAttempt2.Result;
                        }

                        // Value is not final value type, so try a regular type conversion
                        var convertAttempt3 = value.TryConvertTo<T>();
                        if (convertAttempt3.Success)
                            return convertAttempt3.Result;

                        // Still not right type so return default value
                        return defaultValue;

                    }
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
