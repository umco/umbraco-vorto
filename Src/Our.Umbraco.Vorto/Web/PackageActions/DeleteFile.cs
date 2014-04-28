using System.IO;
using System.Web;
using System.Xml;
using umbraco.cms.businesslogic.packager.standardPackageActions;
using umbraco.interfaces;

namespace Our.Umbraco.Vorto.Web.PackageActions
{
	public class DeleteFilePackageAction : IPackageAction
	{
		public string Alias()
		{
			return "Vorto_DeleteFile";
		}

		public bool Execute(string packageName, XmlNode xmlData)
		{
			var node = xmlData.SelectSingleNode("//Action[@alias='" + Alias() + "']");
			var filePath = node.Attributes["path"].Value;
			var absoluteFilePath = HttpContext.Current.Server.MapPath(filePath);

			if(File.Exists(absoluteFilePath))
				File.Delete(absoluteFilePath);

			return true;
		}

		public bool Undo(string packageName, XmlNode xmlData)
		{
			return true;
		}

		public XmlNode SampleXml()
		{
			return helper.parseStringToXmlNode("<Action runat=\"install\" undo=\"true/false\" alias=\"Vorto_DeleteFile\" path=\"path\" />");
		}
	}
}
