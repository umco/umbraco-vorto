using System;
using Newtonsoft.Json;
using Our.Umbraco.Vorto.Models;
using Umbraco.Core;
using Umbraco.Core.Models;

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
					var preValues = services.DataTypeService.GetPreValuesCollectionByDataTypeId(dtd.Id).PreValuesAsDictionary;
					var dataType = JsonConvert.DeserializeObject<DataTypeInfo>(preValues["dataType"].Value);

					// Grab an instance of the target datatype
					return services.DataTypeService.GetDataTypeDefinitionById(dataType.Guid);
				});
		}
	}
}
