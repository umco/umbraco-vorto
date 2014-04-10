using System;
using System.Collections.Generic;

namespace Our.Umbraco.Vorto.Models
{
	internal class VortoValue
	{
		public IDictionary<string, object> Values { get; set; }
		public Guid DtdGuid { get; set; }
	}
}
