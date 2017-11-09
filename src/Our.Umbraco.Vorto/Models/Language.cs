using Newtonsoft.Json;

namespace Our.Umbraco.Vorto.Models
{
    /// <summary>
    /// Represents a Vorto language containing relevant cultral and textual information
    /// </summary>
    public class Language
    {
        /// <summary>
        /// Gets or sets the iso code in the format languagecode2-country/regioncode2
        /// </summary>
        [JsonProperty("isoCode")]
        public string IsoCode { get; set; }

        /// <summary>
        /// Gets or sets the cultural name in English
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the cultural name in the native language
        /// </summary>
        [JsonProperty("nativeName")]
        public string NativeName { get; set; }

        /// <summary>
        /// Gets a value indicating whether the language is set as default
        /// </summary>
        [JsonProperty("isDefault")]
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets a value indicating whether the Gets a value indicating whether the current language represents a writing
        /// system where text flows from right to left.
        /// </summary>
        [JsonProperty("isRightToLeft")]
        public bool IsRightToLeft { get; set; }
    }
}
