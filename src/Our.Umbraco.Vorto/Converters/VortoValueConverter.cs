using System;
using System.Reflection;
using Newtonsoft.Json;
using Our.Umbraco.Vorto.Helpers;
using Our.Umbraco.Vorto.Models;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;

namespace Our.Umbraco.Vorto.Converters
{
	public class VortoValueConverter : PropertyValueConverterBase, IPropertyValueConverterMeta
	{
		public override bool IsConverter(PublishedPropertyType propertyType)
		{
			return propertyType.PropertyEditorAlias.Equals("Our.Umbraco.Vorto");
		}

		public override object ConvertDataToSource(PublishedPropertyType propertyType, object source, bool preview)
		{
			try
			{
				if (source == null || source.ToString().IsNullOrWhiteSpace())
					return null;

				var model = JsonConvert.DeserializeObject<VortoValue>(source.ToString());
				if (model.Values == null)
					return null;

				var innerPropType = VortoHelper.GetInnerPublishedPropertyType(propertyType);
				if (innerPropType == null)
					return null;

				var modelKeys = model.Values.Keys.ToArray();
				foreach (var key in modelKeys)
				{
					model.Values[key] = innerPropType.ConvertDataToSource(model.Values[key], preview);
				}

				return model;
			}
			catch (Exception e)
			{
				LogHelper.Error<VortoValueConverter>("Error converting Vorto value", e);
			}

			return null;
		}

		public override object ConvertSourceToObject(PublishedPropertyType propertyType, object source, bool preview)
		{
			if (source is VortoValue vortoValue)
			{
				var innerPropType = VortoHelper.GetInnerPublishedPropertyType(propertyType); 
				if (innerPropType != null) {

					var type = GetPropertyValueType(propertyType);
					var model = Activator.CreateInstance(type);

					var dtdGuidProp = type.GetProperty("DtdGuid", BindingFlags.Instance | BindingFlags.Public);
					if (dtdGuidProp != null && dtdGuidProp.CanWrite) dtdGuidProp.SetValue(model, vortoValue.DtdGuid);

					var valuesProp = type.GetProperty("Values", BindingFlags.Instance | BindingFlags.Public);
					var valuesAdd = valuesProp.PropertyType.GetMethod("Add", new[] { typeof(string), innerPropType.ClrType });

					var modelKeys = vortoValue.Values.Keys.ToArray();
					foreach (var key in modelKeys)
					{
						var value = innerPropType.ConvertSourceToObject(vortoValue.Values[key], preview);
						if (innerPropType.ClrType.IsAssignableFrom(value.GetType()))
						{
							valuesAdd.Invoke(valuesProp.GetValue(model), new[] { key, value });
						}
						else
						{
							var attempt = value.TryConvertTo(innerPropType.ClrType);
							if (attempt.Success)
								valuesAdd.Invoke(valuesProp.GetValue(model), new[] { key, attempt.Result });
						}
					}

					return model;
				}
			}

			return base.ConvertSourceToObject(propertyType, source, preview);
		}

		public Type GetPropertyValueType(PublishedPropertyType propertyType)
		{
			var innerPropType = VortoHelper.GetInnerPublishedPropertyType(propertyType);

			return innerPropType != null
                ? typeof(VortoValue<>).MakeGenericType(innerPropType.ClrType)
				: typeof(VortoValue<object>);
		}

		public PropertyCacheLevel GetPropertyCacheLevel(PublishedPropertyType propertyType, PropertyCacheValue cacheValue)
		{
			return PropertyCacheLevel.Content;
		}
	}
}
