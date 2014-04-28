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

		public static bool HasVortoValue(this IPublishedContent content, string propertyAlias, 
			string cultureName = null, bool recursive = false)
		{
			if (cultureName == null)
				cultureName = Thread.CurrentThread.CurrentUICulture.Name;

			if (!content.HasValue(propertyAlias, recursive))
				return false;

			var prop = content.GetProperty(propertyAlias, recursive);
			if (prop.Value is VortoValue)
			{
				var vortoModel = prop.Value as VortoValue;
				if (!vortoModel.Values.ContainsKey(cultureName) || vortoModel.Values[cultureName] == null
					|| vortoModel.Values[cultureName].ToString().IsNullOrWhiteSpace())
						return false;
			}

			return true;
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

		public static object GetVortoValue(this IPublishedContent content, string propertyAlias, string cultureName = null,
			bool recursive = false, object defaultValue = null)
		{
			return content.GetVortoValue<object>(propertyAlias, cultureName, recursive, defaultValue);
		}

		public static T GetVortoValue<T>(this IPublishedContent content, string propertyAlias, string cultureName = null, bool recursive = false, T defaultValue = default(T))
			where T : class 
		{
			if (cultureName == null)
				cultureName = Thread.CurrentThread.CurrentUICulture.Name;

			if (content.HasVortoValue(propertyAlias, cultureName, recursive))
			{
				var prop = content.GetProperty(propertyAlias, recursive);
				if (prop.Value is VortoValue)
				{
					// Get the serialized value
					var vortoModel = prop.Value as VortoValue;
					var value = vortoModel.Values[cultureName];

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
					var properyType = CreateDummyPropertyType(targetDataType.Id, targetDataType.PropertyEditorAlias);
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
					}

					// Value is not final value type, so try a regular type conversion
					var convertAttempt2 = value.TryConvertTo<T>();
					if (convertAttempt2.Success)
						return convertAttempt2.Result;

					return default(T);
				}
				
				if (prop.Value is T)
				{
					return prop.Value as T;
				}
				
				return default(T);
			}

			return defaultValue;
		}
		
		#endregion

		private static PublishedPropertyType CreateDummyPropertyType(int dataTypeId, string propertyEditorAlias)
		{
			return new PublishedPropertyType(null,
				new PropertyType(new DataTypeDefinition(-1, propertyEditorAlias)
				{
					Id = dataTypeId
				}));
		}
	}
}
