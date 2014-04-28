using Umbraco.Core;
using Umbraco.Core.Services;

namespace Our.Umbraco.Vorto.Web.Events
{
	public class VortoCacheExpire : IApplicationEventHandler
	{
		#region Unused

		public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{ }

		public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{ }

		#endregion

		public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{
			DataTypeService.Saved += ExpireVortoCache;
		}

		private void ExpireVortoCache(IDataTypeService sender, global::Umbraco.Core.Events.SaveEventArgs<global::Umbraco.Core.Models.IDataTypeDefinition> e)
		{
			foreach (var dataType in e.SavedEntities)
			{
				ApplicationContext.Current.ApplicationCache.RuntimeCache.ClearCacheItem(
					Constants.CacheKey_GetTargetDataTypeDefinition + dataType.Id);
			}
		}
	}
}
