using System;

namespace Our.Umbraco.Vorto.Models
{
    public interface IVortoValue
    {
        /// <summary>
        /// Gets or sets the data type definition id
        /// </summary>
        Guid DtdGuid { get; set; }

        /// <summary>
        /// Attempts to cast the vorto value instance to another generic type.
        /// </summary>
        /// <returns>The cast vorto value instance, or null if no values have a value.</returns>
        VortoValue<T> CastToVortoValue<T>();
    }
}
