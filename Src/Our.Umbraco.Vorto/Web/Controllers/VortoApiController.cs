using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Http;
using AutoMapper;
using umbraco;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web.Editors;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.Mvc;
using Language = Our.Umbraco.Vorto.Models.Language;

namespace Our.Umbraco.Vorto.Web.Controllers
{
	[PluginController("VortoApi")]
	public class VortoApiController : UmbracoAuthorizedJsonController
	{
		public IEnumerable<object> GetNonVortoDataTypes()
		{
			return Services.DataTypeService.GetAllDataTypeDefinitions()
				.Where(x => x.PropertyEditorAlias != "Our.Umbraco.Vorto")
				.OrderBy(x => x.SortOrder)
				.Select(x => new
				{
					guid = x.Key,
					name = x.Name,
					propertyEditorAlias = x.PropertyEditorAlias
				});
		}

		public object GetDataTypeById(Guid id)
		{
			var dtd = Services.DataTypeService.GetDataTypeDefinitionById(id);
			return FormatDataType(dtd);
		}

		public object GetDataTypeByAlias(string contentTypeAlias, string propertyAlias)
		{
			var contentType = Services.ContentTypeService.GetContentType(contentTypeAlias);
			var prop = contentType.CompositionPropertyTypes.SingleOrDefault(x => x.Alias == propertyAlias);
			if (prop == null)
				return null;

			var dtd = Services.DataTypeService.GetDataTypeDefinitionById(prop.DataTypeDefinitionId);
			return FormatDataType(dtd);
		}

		protected object FormatDataType(IDataTypeDefinition dtd)
		{
			if (dtd == null)
				throw new HttpResponseException(HttpStatusCode.NotFound);

			var dataTypeDisplay = Mapper.Map<IDataTypeDefinition, DataTypeDisplay>(dtd);
			var propEditor = PropertyEditorResolver.Current.GetByAlias(dtd.PropertyEditorAlias);

			return new
			{
				guid = dtd.Key,
				propertyEditorAlias = dtd.PropertyEditorAlias,
				preValues = dataTypeDisplay.PreValues,
				view = propEditor.ValueEditor.View
			};
		}

		public IEnumerable<object> GetLanguages(int id, int parentId, Guid dtdguid)
		{
			var dtd = Services.DataTypeService.GetDataTypeDefinitionById(dtdguid);
			var preValues = Services.DataTypeService.GetPreValuesCollectionByDataTypeId(dtd.Id).PreValuesAsDictionary;
			var languageSource = preValues.ContainsKey("languageSource") ? preValues["languageSource"].Value : "";

			var languages = new List<Language>();

			if (languageSource == "inuse")
			{
				var xpath = preValues.ContainsKey("xpath") ? preValues["xpath"].Value : "";

				// Grab languages
				if (!string.IsNullOrWhiteSpace(xpath))
				{
					xpath = xpath.Replace("$currentPage",
						string.Format("//*[@id={0} and @isDoc]", id)).Replace("$parentPage",
							string.Format("//*[@id={0} and @isDoc]", parentId)).Replace("$ancestorOrSelf",
								string.Format("//*[@id={0} and @isDoc]", id != 0 ? id : parentId));

					// Lookup language nodes
					var nodeIds = uQuery.GetNodesByXPath(xpath).Select(x => x.Id).ToArray();
					if (nodeIds.Any())
					{
						var db = ApplicationContext.Current.DatabaseContext.Database;
						languages.AddRange(db.Query<string>(
							string.Format(
								"SELECT DISTINCT [languageISOCode] FROM [umbracoLanguage] JOIN [umbracoDomains] ON [umbracoDomains].[domainDefaultLanguage] = [umbracoLanguage].[id] WHERE [umbracoDomains].[domainRootStructureID] in ({0})",
								string.Join(",", nodeIds)))
							.Select(CultureInfo.GetCultureInfo)
							.Select(x => new Language
							{
								IsoCode = x.Name,
								Name = x.DisplayName,
								NativeName = x.NativeName
							}));
					}
				}
				else
				{
					// No language node xpath so just return a list of all languages in use
					var db = ApplicationContext.Current.DatabaseContext.Database;
					languages.AddRange(
						db.Query<string>(
							"SELECT [languageISOCode] FROM [umbracoLanguage] WHERE EXISTS(SELECT 1 FROM [umbracoDomains] WHERE [umbracoDomains].[domainDefaultLanguage] = [umbracoLanguage].[id])")
							.Select(CultureInfo.GetCultureInfo)
							.Select(x => new Language
							{
								IsoCode = x.Name,
								Name = x.DisplayName,
								NativeName = x.NativeName
							}));
				}
			}
			else
			{
				languages.AddRange(umbraco.cms.businesslogic.language.Language.GetAllAsList()
					.Select(x => CultureInfo.GetCultureInfo(x.CultureAlias))
					.Select(x => new Language
					{
						IsoCode = x.Name,
						Name = x.DisplayName,
						NativeName = x.NativeName
					}));
			}

			// Raise event to allow for further filtering
			var args = new FilterLanguagesEventArgs
			{
				CurrentPageId = id,
				ParentPageId = parentId,
				Languages = languages
			};

			Vorto.CallFilterLanguages(args);

			// Set active language
			var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;

			// See if one has already been set via the event handler
			var activeLanguage = args.Languages.FirstOrDefault(x => x.IsDefault);

			// Try settings to exact match of current culture
			if (activeLanguage == null)
				activeLanguage = args.Languages.FirstOrDefault(x => x.IsoCode == currentCulture);

			// Try setting to nearest match
			if (activeLanguage == null)
				activeLanguage = args.Languages.FirstOrDefault(x => x.IsoCode.Contains(currentCulture));

			// Try setting to nearest match
			if (activeLanguage == null)
				activeLanguage = args.Languages.FirstOrDefault(x => currentCulture.Contains(x.IsoCode));

			// Couldn't find a good enough match, just select the first language
			if (activeLanguage == null)
				activeLanguage = args.Languages.FirstOrDefault();

			if (activeLanguage != null)
				activeLanguage.IsDefault = true;

			// Return results
			return args.Languages;
		}
	}
}
