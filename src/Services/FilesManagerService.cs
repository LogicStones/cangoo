using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Services
{
	public static class FilesManagerService
	{
		public static string SaveFile(HttpPostedFile sourceFile, string filePath, string fileName)
		{
			filePath = AppDomain.CurrentDomain.BaseDirectory + filePath;// "Uploads\\Logos\\";
																		
			//fileName = model.CompanyID.ToString().Replace(' ', '_').Trim() + "_Logo";
			DeleteFile(filePath, fileName);

			string savedFileName = fileName + Path.GetExtension(sourceFile.FileName);
			sourceFile.SaveAs(Path.Combine(filePath, savedFileName));
			return savedFileName;
		}

		public static void DeleteFile(string path, string fileName)
		{
			string[] files = Directory.GetFiles(path, fileName + ".*");

			foreach (string item in files)
			{
				File.Delete(item);
			}
		}
	}
}
