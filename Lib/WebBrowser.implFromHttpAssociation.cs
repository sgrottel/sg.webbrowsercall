using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SG.WebBrowserCall
{

	/// <summary>
	/// Implementation to collect web browser info via windows registry on http(s) url association
	/// </summary>
	partial class WebBrowser
	{

		/// <summary>
		/// Collects the default browser from the "http" url association
		/// </summary>
		/// <param name="browsers">List of browsers</param>
		private static void collectFromHttpAssociation(ref List<WebBrowser> browsers)
		{
			if (browsers == null) browsers = new List<WebBrowser>();
			callForAllRegKeysSoftware(browsers, (List<WebBrowser> bs, RegistryKey swKey) =>
			{
				var key = swKey.OpenSubKey(@"Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice");
				if (key == null)
				{
					key = swKey.OpenSubKey(@"Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice");
				}
				if (key == null) return true; // continue
				string progId = key.GetValue("ProgId", "").ToString();
				if (string.IsNullOrEmpty(progId)) return true; // continue

				using (var appKey = Registry.ClassesRoot.OpenSubKey(progId))
				{
					if (appKey == null) return true;

					WebBrowser b = new WebBrowser()
					{
						IsDefault = true,
						Name = cleanupAssociationName(appKey.GetValue("", progId).ToString())
					};

					using (var infoKey = appKey.OpenSubKey("Application"))
					{
						if (infoKey != null)
						{
							string an = infoKey.GetValue("ApplicationName", "").ToString();
							if (!string.IsNullOrEmpty(an)) b.Name = an;
							string ai = infoKey.GetValue("ApplicationIcon", "").ToString();
							if (!string.IsNullOrEmpty(ai)) b.IconInfo = ai;
						}
					}

					using (var defIconKey = appKey.OpenSubKey("DefaultIcon"))
					{
						if (defIconKey != null)
						{
							string ai = defIconKey.GetValue("", "").ToString();
							if (!string.IsNullOrEmpty(ai)) b.IconInfo = ai;
						}
					}

					using (var cmdOpenKey = appKey.OpenSubKey(@"shell\open\command"))
					{
						if (cmdOpenKey != null)
						{
							string cmd = cmdOpenKey.GetValue("", "").ToString();
							if (!string.IsNullOrEmpty(cmd))
							{
								string[] cmds = splitCmdLine(cmd);
								if (cmds != null && cmds.Length > 0)
								{
									b.ExecutablePath = cmds[0];
									if (cmds.Length > 1)
									{
										b.open = (string url) =>
										{
											var psi = new ProcessStartInfo()
											{
												FileName = cmds[0],
												UseShellExecute = true
											};
											for (int i = 1; i < cmds.Length; ++i)
												if (cmds[i] == "%1")
													psi.ArgumentList.Add(url);
												else
													psi.ArgumentList.Add(cmds[i]);
											Process.Start(psi);
										};

									}
								}
							}
						}
					}

					tryFixExecutablePath(b);
					ensureOpenAction(b);
					b.ProductFamily = guessProductFamily(b);
					insertBrowser(bs, b);
				}

				return false; // found
			});
		}

		private static string cleanupAssociationName(string v)
		{
			if (v.EndsWith(" URL", StringComparison.CurrentCultureIgnoreCase)) return v.Substring(0, v.Length - 4);
			return v;
		}

	}
}
