using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Our.Umbraco.Vorto.Models
{
	/// <summary>
	/// Represents a multilingual property value
	/// </summary>
	public partial class VortoValue<T>
	{
		/// <summary>
		/// Gets or sets the collection of language independent values
		/// </summary>
		[JsonProperty("values")]
		public IDictionary<string, T> Values { get; set; }

		/// <summary>
		/// Gets or sets the data type definition id
		/// </summary>
		[JsonProperty("dtdGuid")]
		public Guid DtdGuid { get; set; }

		public VortoValue()
		{
			Values = new Dictionary<string, T>();
		}
	}
}
