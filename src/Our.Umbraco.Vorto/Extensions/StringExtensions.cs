namespace Our.Umbraco.Vorto.Extensions
{
	internal static class StringExtensions
	{
		public static bool DetectIsJson(this string input)
		{
			input = input.Trim();
			return (input.StartsWith("{") && input.EndsWith("}"))
				   || (input.StartsWith("[") && input.EndsWith("]"));
		}
	}
}
