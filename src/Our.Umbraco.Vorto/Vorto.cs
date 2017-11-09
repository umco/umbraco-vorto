using System;
using System.Collections.Generic;
using Our.Umbraco.Vorto.Models;

namespace Our.Umbraco.Vorto
{
	public static class Vorto
	{
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
