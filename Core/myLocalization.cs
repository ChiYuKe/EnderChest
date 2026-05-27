using System;
using System.Reflection;
using System.IO;

namespace EnderChest.Core
{
	internal class myLocalization
	{
		private static string modPath;
		public static void SetModPath(string path)
		{
			myLocalization.modPath = path;
		}
		public static void Translate(Type root, bool generateTemplate = false)
		{
			Localization.RegisterForTranslation(root);
			myLocalization.LoadStrings();
			LocString.CreateLocStringKeys(root, null);
			if (generateTemplate)
			{
				Localization.GenerateStringsTemplate(root, Path.Combine(myLocalization.GetModPath(), "translations"));
			}
		}

		private static void LoadStrings()
		{
			try
			{
				Localization.Locale locale = Localization.GetLocale();
				string localeCode = (locale != null) ? locale.Code : "en";
				bool flag = localeCode.IsNullOrWhiteSpace();
				if (!flag)
				{
					string translationsPath = Path.Combine(myLocalization.GetModPath(), "translations");
					string poPath = Path.Combine(translationsPath, localeCode + ".po");
					bool flag2 = !File.Exists(poPath) && localeCode.StartsWith("en", StringComparison.OrdinalIgnoreCase);
					if (flag2)
					{
						poPath = Path.Combine(translationsPath, "en.po");
					}
					bool flag3 = !File.Exists(poPath);
					if (flag3)
					{
						Debug.LogWarning("[EnderChest] Missing localization file: " + poPath);
					}
					else
					{
						Debug.Log("[EnderChest] Loading localization file: " + poPath);
						Localization.OverloadStrings(Localization.LoadStringsFile(poPath, false));
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[EnderChest] Failed to load localization: " + ex.Message);
			}
		}

		private static string GetModPath()
		{
			return (!string.IsNullOrEmpty(myLocalization.modPath)) ? myLocalization.modPath : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		}
	}
}
