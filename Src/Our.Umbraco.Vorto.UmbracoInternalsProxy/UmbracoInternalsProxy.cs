using Umbraco.Core.PropertyEditors;

namespace Our.Umbraco.Vorto
{
    public static class UmbracoInternalsProxy
    {
	    public static PropertyEditor GetPropertyEditorByAlias(string alias)
	    {
		    return PropertyEditorResolver.Current.GetByAlias(alias);
	    }
    }
}
