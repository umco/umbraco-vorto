using Newtonsoft.Json;

namespace Our.Umbraco.Vorto.Models
{
	public class Language
	{
		[JsonProperty("isoCode")]
		public string IsoCode { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("nativeName")]
		public string NativeName { get; set; }

		[JsonProperty("isDefault")]
		public bool IsDefault { get; set; }

        [JsonProperty("isRightToLeft")]
        public bool IsRightToLeft { get; set; }
	}
}
