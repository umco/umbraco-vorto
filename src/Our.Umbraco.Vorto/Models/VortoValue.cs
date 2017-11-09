using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Our.Umbraco.Vorto.Models
{
    /// <summary>
    /// Represents a multilingual property value
    /// </summary>
    public class VortoValue
    {
        /// <summary>
        /// Gets or sets the collection of language independent values
        /// </summary>
        [JsonProperty("values")]
        public IDictionary<string, object> Values { get; set; }

        /// <summary>
        /// Gets or sets the data type definition id
        /// </summary>
        [JsonProperty("dtdGuid")]
        public Guid DtdGuid { get; set; }
    }
}
