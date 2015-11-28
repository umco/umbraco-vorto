using System;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
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

		private static bool DoHasVortoValue(this IPublishedContent content, string propertyAlias,
            string cultureOrLanguage = null, bool recursive = false)
		{
			if (cultureOrLanguage == null)
				cultureOrLanguage = Thread.CurrentThread.CurrentUICulture.Name;

			if (!content.HasValue(propertyAlias, recursive))
				return false;

			var prop = content.GetProperty(propertyAlias, recursive);
			if (prop.Value is VortoValue)
			{
				var vortoModel = prop.Value as VortoValue;
                cultureOrLanguage = ValidateVortoCultureOrLanguage(cultureOrLanguage, vortoModel);

                if (!vortoModel.Values.ContainsKey(cultureOrLanguage) || vortoModel.Values[cultureOrLanguage] == null
					|| vortoModel.Values[cultureOrLanguage].ToString().IsNullOrWhiteSpace())
						return false;
			}

			return true;
		}

        public static bool HasVortoValue(this IPublishedContent content, string propertyAlias,
            string cultureOrLanguage = null, bool recursive = false, string fallbackCultureName = null)
        {
            var hasValue = content.DoHasVortoValue(propertyAlias, cultureOrLanguage, recursive);
            if (!hasValue && !string.IsNullOrEmpty(fallbackCultureName) && !fallbackCultureName.Equals(cultureOrLanguage))
                hasValue = content.DoHasVortoValue(propertyAlias, fallbackCultureName, recursive);
            return hasValue;
        }

		#endregion

		#region IfHasValue

		// String
		//public static IHtmlString IfHasVortoValue(this IPublishedContent content, string propertyAlias, 
		//	string valueIfTrue, string valueIfFalse = null)
		//{
		//	return !content.HasVortoValue(propertyAlias) 
		//		? new HtmlString(valueIfFalse ?? string.Empty) 
		//		: new HtmlString(valueIfTrue);
		//}

		// No type
		//public static HelperResult IfHasVortoValue(this IPublishedContent content, string propertyAlias,
		//	Func<object, HelperResult> templateIfTrue, Func<object, HelperResult> templateIfFalse = null)
		//{
		//	return content.IfHasVortoValue()
		//}

		// Type
		//public static HelperResult IfHasVortoValue<T>(this IPublishedContent content, string propertyAlias,
		//	Func<T, HelperResult> templateIfTrue, Func<T, HelperResult> templateIfFalse = null)
		//{
		//	return new HelperResult(writer =>
		//	{
		//		if (!content.HasVortoValue(propertyAlias))
		//		{
		//			if (templateIfFalse != null)
		//				templateIfFalse(null).WriteTo(writer);
		//		}
		//		else
		//		{
		//			var value = content.GetVortoValue(propertyAlias);
		//			templateIfTrue(value).WriteTo(writer);
		//		}
		//	});
		//}

		#endregion

		#region GetValue

		private static object DoGetVortoValue(this IPublishedContent content, string propertyAlias, string cultureOrLanguage = null,
			bool recursive = false, object defaultValue = null)
		{
			return content.GetVortoValue<object>(propertyAlias, cultureOrLanguage, recursive, defaultValue);
		}

        public static object GetVortoValue(this IPublishedContent content, string propertyAlias, string cultureOrLanguage = null,
            bool recursive = false, object defaultValue = null, string fallbackCultureName = null)
        {
            var result = content.DoGetVortoValue(propertyAlias, cultureOrLanguage, recursive);
            if (result == null && !string.IsNullOrEmpty(fallbackCultureName) && !fallbackCultureName.Equals(cultureOrLanguage))
                result = content.DoGetVortoValue(propertyAlias, fallbackCultureName, recursive, defaultValue);

            return result;
        }

		private static T DoGetVortoValue<T>(this IPublishedContent content, string propertyAlias, string cultureOrLanguage = null, 
            bool recursive = false, T defaultValue = default(T))
		{
			if (cultureOrLanguage == null)
				cultureOrLanguage = Thread.CurrentThread.CurrentUICulture.Name;

			if (content.HasVortoValue(propertyAlias, cultureOrLanguage, recursive))
			{
				var prop = content.GetProperty(propertyAlias, recursive);
				if (prop.Value is VortoValue)
				{
					// Get the serialized value
					var vortoModel = prop.Value as VortoValue;
                    cultureOrLanguage = ValidateVortoCultureOrLanguage(cultureOrLanguage, vortoModel);

					var value = vortoModel.Values[cultureOrLanguage];

					// If the value is of type T, just return it
					//if (value is T)
					//	return (T)value;

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

					return default(T);
				}
				
				if (prop.Value is T)
				{
					return (T)prop.Value;
				}
				
				return default(T);
			}

			return defaultValue;
		}


        public static T GetVortoValue<T>(this IPublishedContent content, string propertyAlias, string cultureOrLanguage = null,
            bool recursive = false, T defaultValue = default(T), string fallbackCultureName = null)
        {
            var result = content.DoGetVortoValue<T>(propertyAlias, cultureOrLanguage, recursive);
            if (result == null && !string.IsNullOrEmpty(fallbackCultureName) && !fallbackCultureName.Equals(cultureOrLanguage))
                result = content.DoGetVortoValue<T>(propertyAlias, fallbackCultureName, recursive, defaultValue);

            return result;
        }

	    #endregion

        private static string ValidateVortoCultureOrLanguage(string cultureOrLanguage, VortoValue vortoModel)
        {
            if (cultureOrLanguage.Length == 2)
            {
                cultureOrLanguage = vortoModel.Values.Keys
                    .SingleOrDefault(x => x.Substring(0, 2).Equals(cultureOrLanguage, StringComparison.InvariantCultureIgnoreCase)) ?? string.Empty;
            }

            return cultureOrLanguage;
        }

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
