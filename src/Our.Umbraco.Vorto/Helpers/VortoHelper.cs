using System;
using Newtonsoft.Json;
using Our.Umbraco.Vorto.Models;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;

namespace Our.Umbraco.Vorto.Helpers
{
	internal static class VortoHelper
	{
		internal static IDataTypeDefinition GetTargetDataTypeDefinition(Guid myId)
		{
			return (IDataTypeDefinition)ApplicationContext.Current.ApplicationCache.RuntimeCache.GetCacheItem(
				Constants.CacheKey_GetTargetDataTypeDefinition + myId,
				() =>
				{
					// Get instance of our own datatype so we can lookup the actual datatype from prevalue
					var services = ApplicationContext.Current.Services;
					var dtd = services.DataTypeService.GetDataTypeDefinitionById(myId);
					if (dtd == null) return null;

					var preValues = services.DataTypeService.GetPreValuesCollectionByDataTypeId(dtd.Id)?.PreValuesAsDictionary;
					if (preValues == null || !preValues.ContainsKey("dataType")) return null;

					var dataType = JsonConvert.DeserializeObject<DataTypeInfo>(preValues["dataType"].Value);

					// Grab an instance of the target datatype
					return services.DataTypeService.GetDataTypeDefinitionById(dataType.Guid);
				});
		}

		internal static PublishedPropertyType GetInnerPublishedPropertyType(PublishedPropertyType propertyType)
		{
			return (PublishedPropertyType)ApplicationContext.Current.ApplicationCache.RuntimeCache.GetCacheItem(
				Constants.CacheKey_GetInnerPublishedPropertyType + propertyType.DataTypeId + "_"+ propertyType.ContentType.Id,
				() =>
				{
					var services = ApplicationContext.Current.Services;

					var preValues = services.DataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeId)?.PreValuesAsDictionary;
					if (preValues == null || !preValues.ContainsKey("dataType"))
						return null;

					var dataType = JsonConvert.DeserializeObject<DataTypeInfo>(preValues["dataType"].Value);
					var dtd = services.DataTypeService.GetDataTypeDefinitionById(dataType.Guid);

					return new PublishedPropertyType(propertyType.ContentType, new PropertyType(dtd) { Alias = propertyType.PropertyTypeAlias });
				});
		}

		internal static PublishedPropertyType CreateDummyPropertyType(int dataTypeId, string propertyEditorAlias, PublishedContentType contentType)
		{
			return new PublishedPropertyType(contentType,
				new PropertyType(new DataTypeDefinition(-1, propertyEditorAlias)
				{
					Id = dataTypeId
				}));
		}
	}
}
