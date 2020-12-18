using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SG.WebBrowserCall
{

	/// <summary>
	/// Implementation to collect web browser information via windows registry StartMenuItenern
	/// </summary>
	/// <remarks>
	/// This is marked deprecated in Windows 10
	/// </remarks>
	partial class WebBrowser
	{

		/// <summary>
		/// Collects installed browser information from "StartMenuInternet"
		/// </summary>
		/// <remarks>
		/// https://stackoverflow.com/a/2370850
		/// https://docs.microsoft.com/en-us/windows/win32/shell/reg-middleware-apps#a-sample-browser
		/// This is marked deprecated in Windows 10
		/// </remarks>
		/// <param name="browsers">List of browsers</param>
		static private void collectFromStartMenuInternet(ref List<WebBrowser> browsers)
		{
			if (browsers == null) browsers = new List<WebBrowser>();
			callForAllStartMenuInternet(browsers, (List<WebBrowser> bs, RegistryKey key) =>
			{
				collectFromStartMenuInternet(bs, key);
				return true;
			});
		}

		/// <summary>
		/// Tries to mark one browser in the list of browsers as default browsers, based on "StartMenuInternet"
		/// </summary>
		/// <remarks>
		/// https://docs.microsoft.com/en-us/windows/win32/shell/reg-middleware-apps#a-sample-browser
		/// This is marked deprecated in Windows 10
		/// </remarks>
		/// <param name="browsers">List of browsers</param>
		private static void chooseDefaultFromStartMenuInternet(List<WebBrowser> browsers)
		{
			if (browsers == null) return;
			if (browsers.Count <= 0) return;
			if (browsers == null) browsers = new List<WebBrowser>();
			callForAllStartMenuInternet(browsers, (List<WebBrowser> bs, RegistryKey key) =>
			{
				string defName = key.GetValue("", "").ToString();
				if (string.IsNullOrEmpty(defName)) return true; // continue

				string defBN = "";
				callForAllStartMenuInternet(browsers, (List<WebBrowser> bs2, RegistryKey key2) =>
				{
					using (var key3 = key2.OpenSubKey(defName))
					{
						if (key3 != null)
							defBN = key3.GetValue("", "").ToString();
						if (!string.IsNullOrEmpty(defBN)) return false;
					}
					return true;
				});
				if (string.IsNullOrEmpty(defBN)) return true; // continue

				foreach (var b in browsers)
				{
					if (string.Equals(b.Name, defBN))
					{
						b.IsDefault = true;
						return false; // found
					}
				}
				return true; // continue
			});
		}

		/// <summary>
		/// Invokes `call` for all possible locations of "StartMenuInternet".
		/// Order:
		///  * HKCU
		///  * HKCU/WOW6432
		///  * HKLM
		///  * HKLM/WOW6432
		/// </summary>
		/// <param name="browsers">List of browsers</param>
		/// <param name="call">The call to invoke</param>
		static private void callForAllStartMenuInternet(List<WebBrowser> browsers, Func<List<WebBrowser>, RegistryKey, bool> call)
		{
			callForAllRegKeysSoftware(browsers, (List<WebBrowser> bs, RegistryKey swKey) =>
			{
				var key = swKey.OpenSubKey(@"Clients\StartMenuInternet");
				if (key == null) return true;
				return call(bs, key);
			});
		}

		/// <summary>
		/// Collects installed browser information from one specific registry key to "StartMenuInternet"
		/// </summary>
		/// <remarks>
		/// This is marked deprecated in Windows 10
		/// </remarks>
		/// <param name="browsers">List of browsers</param>
		/// <param name="key">An open registry key to "StartMenuInternet"</param>
		private static void collectFromStartMenuInternet(List<WebBrowser> browsers, RegistryKey key)
		{
			string[] keyNames = key.GetSubKeyNames();
			if (keyNames == null || keyNames.Length <= 0) return;
			foreach (string subkeyName in keyNames)
			{
				try
				{
					WebBrowser b = new WebBrowser();

					using (RegistryKey browserKey = key.OpenSubKey(subkeyName))
					{
						b.Name = browserKey.GetValue("", subkeyName).ToString();

						using (RegistryKey iconKey = browserKey.OpenSubKey(@"DefaultIcon"))
						{
							if (iconKey != null) b.IconInfo = unQuote(iconKey.GetValue("", "").ToString());
						}

						using (RegistryKey openCommandKey = browserKey.OpenSubKey(@"shell\open\command"))
						{
							if (openCommandKey != null) b.ExecutablePath = unQuote(openCommandKey.GetValue("", "").ToString());
						}
					}

					tryFixExecutablePath(b);
					ensureOpenAction(b);
					b.ProductFamily = guessProductFamily(b);
					insertBrowser(browsers, b);
				}
				catch { }
			}

		}

		/// <summary>
		/// Makes sure the open action is available, as long as an executable path is set
		/// </summary>
		/// <param name="b">The browser object</param>
		private static void ensureOpenAction(WebBrowser b)
		{
			if (b.open == null)
			{
				if (!string.IsNullOrEmpty(b.ExecutablePath))
				{
					b.open = (string url) =>
					{
						var psi = new ProcessStartInfo()
						{
							FileName = b.ExecutablePath,
							UseShellExecute = true
						};
						psi.ArgumentList.Add(url);
						Process.Start(psi);
					};
				}
			}
		}

	}
}
