using System.Collections.Generic;

namespace Our.Umbraco.Vorto.Models
{
    public interface IVortoValue<T> : IVortoValue
    {
        /// <summary>
		/// Gets or sets the data type definition id
		/// </summary>
        IDictionary<string, T> Values { get; set; }
    }
}
