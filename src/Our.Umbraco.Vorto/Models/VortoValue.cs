using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Our.Umbraco.Vorto.Models
{
	public class VortoValue
	{
	    internal VortoValue()
	    { }

		[JsonProperty("values")]
		public IDictionary<string, object> Values { get; set; }

		[JsonProperty("dtdGuid")]
		public Guid DtdGuid { get; set; }
	}
}
