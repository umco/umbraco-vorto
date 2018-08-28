using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ClientDependency.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Our.Umbraco.Vorto.Extensions;
using Our.Umbraco.Vorto.Helpers;
using Our.Umbraco.Vorto.Models;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Editors;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Web.PropertyEditors;

namespace Our.Umbraco.Vorto.Web.PropertyEditors
{
    [PropertyEditorAsset(ClientDependencyType.Javascript, "~/App_Plugins/Vorto/js/jquery.hoverIntent.minified.js", Priority = 1)]
    [PropertyEditorAsset(ClientDependencyType.Javascript, "~/App_Plugins/Vorto/js/vorto.js", Priority = 2)]
    [PropertyEditorAsset(ClientDependencyType.Css, "~/App_Plugins/Vorto/css/vorto.css", Priority = 2)]
    [PropertyEditor(PropertyEditorAlias, "Vorto", "~/App_Plugins/Vorto/Views/vorto.html", ValueType = "JSON")]
	public class VortoPropertyEditor : PropertyEditor
	{
        public const string PropertyEditorAlias = "Our.Umbraco.Vorto";

		private IDictionary<string, object> _defaultPreValues;
		public override IDictionary<string, object> DefaultPreValues
		{
			get { return _defaultPreValues; }
			set { _defaultPreValues = value; }
		}

		public VortoPropertyEditor()
		{
			// Setup default values
			_defaultPreValues = new Dictionary<string, object>
			{
				{"languageSource", "installed"},
				{"mandatoryBehaviour", "ignore"},
				{"rtlBehaviour", "ignore"},
			};
		}

		#region Pre Value Editor

		protected override PreValueEditor CreatePreValueEditor()
		{
			return new VortoPreValueEditor();
		}

		internal class VortoPreValueEditor : PreValueEditor
		{
            [PreValueField("dataType", "Data Type", "~/App_Plugins/Vorto/views/vorto.propertyEditorPicker.html", Description = "Select the data type to wrap.")]
			public string DataType { get; set; }

            [PreValueField("languageSource", "Language Source", "~/App_Plugins/Vorto/views/vorto.languageSourceRadioList.html", Description = "Select where Vorto should lookup the languages from.")]
			public string LanguageSource { get; set; }

			[PreValueField("xpath", "Language Nodes XPath", "textstring", Description = "If using in-use language source, enter an XPath statement to locate nodes containing language settings.")]
			public string XPath { get; set; }

			[PreValueField("displayNativeNames", "Display Native Language Names", "boolean", Description = "Set whether to display language names in their native form.")]
			public string DisplayNativeNames { get; set; }

            [PreValueField("primaryLanguage", "Primary Language", "~/App_Plugins/Vorto/views/vorto.languagePicker.html", Description = "Select the primary language for this field.")]
			public string PrimaryLanguage { get; set; }

            [PreValueField("mandatoryBehaviour", "Mandatory Field Behaviour", "~/App_Plugins/Vorto/views/vorto.mandatoryBehaviourPicker.html", Description = "Select how Vorto should handle mandatory fields.")]
			public string MandatoryBehaviour { get; set; }

            [PreValueField("rtlBehaviour", "RTL Behaviour", "~/App_Plugins/Vorto/views/vorto.rtlBehaviourPicker.html", Description = "[EXPERIMENTAL] Select how Vorto should handle Right-to-left languages. This feature is experimental so depending on the property being wrapped, results may vary.")]
            public string RtlBehaviour { get; set; }

            [PreValueField("showFilledLanguages", "Highlight Languages With Content", "boolean", Description = "Show a tick-mark next to the language name when there is content for that language.")]
            public bool ShowFilledLanguages { get; set; }

            [PreValueField("hideLabel", "Hide Label", "boolean", Description = "Hide the Umbraco property title and description, making the Vorto span the entire page width")]
            public bool HideLabel { get; set; }
        }

		#endregion

		#region Value Editor

		protected override PropertyValueEditor CreateValueEditor()
		{
			return new VortoPropertyValueEditor(base.CreateValueEditor());
		}

		internal class VortoPropertyValueEditor : PropertyValueEditorWrapper
		{
			public VortoPropertyValueEditor(PropertyValueEditor wrapped) 
				: base(wrapped)
			{ }

			public override string ConvertDbToString(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
			{
				if (property.Value == null || property.Value.ToString().IsNullOrWhiteSpace())
                    return string.Empty;

                // Something weird is happening in core whereby ConvertDbToString is getting
                // called loads of times on publish, forcing the property value to get converted
                // again, which in tern screws up the values. To get round it, we create a 
                // dummy property copying the original properties value, this way not overwriting
                // the original property value allowing it to be re-converted again later
                var prop2 = new Property(propertyType, property.Value);

				try
				{
					var value = JsonConvert.DeserializeObject<VortoValue>(property.Value.ToString());
				    if (value.Values != null)
				    {
                        // If the DTD Guid isn't set (probably because someone has made the value manually)
                        // then do a lookup and store it
					    if(value.DtdGuid == Guid.Empty)
					    {
						    var vortoDtd = dataTypeService.GetDataTypeDefinitionById(propertyType.DataTypeDefinitionId);
						    value.DtdGuid = vortoDtd.Key;
					    }

				        var dtd = VortoHelper.GetTargetDataTypeDefinition(value.DtdGuid);
				        var propEditor = PropertyEditorResolver.Current.GetByAlias(dtd.PropertyEditorAlias);
				        var propType = new PropertyType(dtd);

				        var keys = value.Values.Keys.ToArray();
				        foreach (var key in keys)
				        {
				            var prop = new Property(propType, value.Values[key] == null ? null : value.Values[key].ToString());
				            var newValue = propEditor.ValueEditor.ConvertDbToString(prop, propType, dataTypeService);
				            value.Values[key] = newValue;
				        }

                        prop2.Value = JsonConvert.SerializeObject(value);
				    }
				}
				catch (Exception ex)
				{
					LogHelper.Error<VortoPropertyValueEditor>("Error converting DB value to String", ex);
				}

                return base.ConvertDbToString(prop2, propertyType, dataTypeService);
			}
	
			public override object ConvertDbToEditor(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
			{
				var propertyValue = property?.Value?.ToString();
				if (propertyValue.IsNullOrWhiteSpace())
                    return string.Empty;

				// Something weird is happening in core whereby ConvertDbToEditor is getting
				// called loads of times on publish, forcing the property value to get converted
				// again, which in tern screws up the values. To get round it, we create a 
				// dummy property copying the original properties value, this way not overwriting
				// the original property value allowing it to be re-converted again later
				var prop2 = new Property(propertyType, property.Value);

				try
				{
					VortoValue value = null;

					// Does the value look like JSON and does it look like a vorto value?
					if (propertyValue.DetectIsJson() && propertyValue.IndexOf("dtdGuid") != -1)
					{
						value = JsonConvert.DeserializeObject<VortoValue>(propertyValue);
					}
					else
					{
						// Doesn't look like a vorto value so we are going to assume it got converted
						// from a normal prop editor to a vorto editor, so lets construct a VortoValue
						var dataTypeDef = dataTypeService.GetDataTypeDefinitionById(propertyType.DataTypeDefinitionId);

						string primaryLanguage = null;

						// Look for primary language in prevalues
						var preValues = dataTypeService.GetPreValuesCollectionByDataTypeId(dataTypeDef.Id)?.PreValuesAsDictionary;
						if (preValues != null)
						{
							// We need to store the current value inder a language key so try and find the best key to store it under
							primaryLanguage = preValues.ContainsKey("primaryLanguage") && !preValues["primaryLanguage"].Value.IsNullOrWhiteSpace()
								? preValues["primaryLanguage"].Value
								: null;
						}

						// No explicit primary language set, so try and work out the best match
						if (primaryLanguage.IsNullOrWhiteSpace())
						{
							var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;
							var languages = umbraco.cms.businesslogic.language.Language.GetAllAsList()
								.Select(x => x.CultureAlias)
								.ToList();

							// Check for an exact culture match
							primaryLanguage = languages.FirstOrDefault(x => x == currentCulture);

							// Check for a close match
							if (primaryLanguage.IsNullOrWhiteSpace())
								primaryLanguage = languages.FirstOrDefault(x => x.Contains(currentCulture));

							// Check for a close match
							if (primaryLanguage.IsNullOrWhiteSpace())
								primaryLanguage = languages.FirstOrDefault(x => currentCulture.Contains(x));

							// Couldn't find a good enough match, just select the first language
							if (primaryLanguage.IsNullOrWhiteSpace())
								primaryLanguage = languages.FirstOrDefault();
						}

						if (!primaryLanguage.IsNullOrWhiteSpace())
						{
							value = new VortoValue
							{
								DtdGuid = dataTypeDef.Key,
								Values = new Dictionary<string, object>
									{
										{ primaryLanguage, property.Value }
									}
							};
						}
					}
					
				    if (value?.Values != null)
				    {
				        var dtd = VortoHelper.GetTargetDataTypeDefinition(value.DtdGuid);
				        var propEditor = PropertyEditorResolver.Current.GetByAlias(dtd.PropertyEditorAlias);
				        var propType = new PropertyType(dtd);

				        var keys = value.Values.Keys.ToArray();
				        foreach (var key in keys)
				        {
				            var prop = new Property(propType, value.Values[key] == null ? null : value.Values[key].ToString());
				            var newValue = propEditor.ValueEditor.ConvertDbToEditor(prop, propType, dataTypeService);
				            value.Values[key] = (newValue == null) ? null : JToken.FromObject(newValue);
				        }

                        prop2.Value = JsonConvert.SerializeObject(value);
				    }
				}
				catch (Exception ex)
				{
					LogHelper.Error<VortoPropertyValueEditor>("Error converting DB value to Editor", ex);
				}

                return base.ConvertDbToEditor(prop2, propertyType, dataTypeService);
			}

			public override object ConvertEditorToDb(ContentPropertyData editorValue, object currentValue)
			{
				if (editorValue.Value == null || editorValue.Value.ToString().IsNullOrWhiteSpace())
					return string.Empty;

				try
				{
					var value = JsonConvert.DeserializeObject<VortoValue>(editorValue.Value.ToString());
				    if (value.Values != null)
				    {
				        var dtd = VortoHelper.GetTargetDataTypeDefinition(value.DtdGuid);
				        var preValues = ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(dtd.Id);
				        var propEditor = PropertyEditorResolver.Current.GetByAlias(dtd.PropertyEditorAlias);

				        var keys = value.Values.Keys.ToArray();
				        foreach (var key in keys)
				        {
				            var propData = new ContentPropertyData(value.Values[key], preValues, new Dictionary<string, object>());
				            var newValue = propEditor.ValueEditor.ConvertEditorToDb(propData, value.Values[key]);
				            value.Values[key] = (newValue == null) ? null : JToken.FromObject(newValue);
				        }
				    }
				    return JsonConvert.SerializeObject(value);
				}
				catch (Exception ex)
				{
					LogHelper.Error<VortoPropertyValueEditor>("Error converting DB value to Editor", ex);
				}

				return base.ConvertEditorToDb(editorValue, currentValue);
			}
		}

		#endregion 
	}
}
