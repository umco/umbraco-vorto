using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using Umbraco.Core;

namespace Our.Umbraco.Vorto.Models
{
    /// <summary>
    /// Represents a multilingual property value
    /// </summary>
    public partial class VortoValue<T> : IVortoValue<T>
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

        /// <summary>
        /// Attempts to cast the vorto value instance to another generic type.
        /// </summary>
        /// <returns>The cast vorto value instance, or null if no values have a value.</returns>
        public VortoValue<TAs> CastToVortoValue<TAs>()
        {
            var newVortoValue = new VortoValue<TAs>()
            {
                Values = Values.ToDictionary(x => x.Key, x => { var v = x.Value.TryConvertTo<TAs>(); return v.Success ? v.Result : default(TAs); })
                    .Where(x => !EqualityComparer<TAs>.Default.Equals(x.Value, default(TAs)))
                    .ToDictionary(x => x.Key, x => x.Value)
            };

            return newVortoValue.Values.Count > 0 ? newVortoValue : null;
        }
    }
}
