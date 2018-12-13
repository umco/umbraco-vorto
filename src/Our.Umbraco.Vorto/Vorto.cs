using System;
using System.Collections.Generic;
using System.Configuration;
using Our.Umbraco.Vorto.Models;
using Umbraco.Core;

namespace Our.Umbraco.Vorto
{
	public static class Vorto
	{
		private static string _defaultFallbackCultureName;
		public static string DefaultFallbackCultureName
		{
			get
			{
				return !_defaultFallbackCultureName.IsNullOrWhiteSpace()
					? _defaultFallbackCultureName
					: ConfigurationManager.AppSettings["Vorto:DefaultFallbackCultureName"];
			}
			set
			{
				_defaultFallbackCultureName = value;
			}
		}

		#region Event Handlers

		public static event EventHandler<FilterLanguagesEventArgs> FilterLanguages;

		internal static void CallFilterLanguages(FilterLanguagesEventArgs args)
		{
		    FilterLanguages?.Invoke(null, args);
		}

	    #endregion
	}

	#region Event Args

	public class FilterLanguagesEventArgs : EventArgs
	{
		public int CurrentPageId { get; set; }
		public int ParentPageId { get; set; }

		public IList<Language> Languages { get; set; }
	}

	#endregion
}
