using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SG.WebBrowserCall
{

	/// <summary>
	/// Represents a web browser
	/// </summary>
	/// <remarks>
	///
	/// https://stackoverflow.com/a/21520493
	///
	///
	/// </remarks>
	public class WebBrowser
	{

		#region Instance Fields

		/// <summary>
		/// Flag whether or not this web browser is the system default to open https links
		/// </summary>
		/// <remarks>
		/// If none of the known web browsers are detected as default, an generic unknown web browser object will be created.
		/// This represents the fact, that the operating system will do something when trying to open https links.
		/// </remarks>
		public bool IsDefault { get; private set; } = false;

		/// <summary>
		/// Product family of this web browser
		/// </summary>
		public ProductFamily ProductFamily { get; private set; } = ProductFamily.Unknown;

		/// <summary>
		/// Human-readable name
		/// </summary>
		/// <remarks>This can be null or empty, in case the browser is unknown.</remarks>
		public string Name { get; private set; } = null;

		/// <summary>
		/// Path to the installed main executable of the web browser
		/// </summary>
		/// <remarks>This can be null or empty, in case the installation path could not be determined.</remarks>
		public string ExecutablePath { get; private set; } = null;

		/// <summary>
		/// Path to the icon representing this web browser
		/// </summary>
		/// <remarks>This can be null or empty, in case the icon path could not be determined.</remarks>
		public string IconInfo { get; private set; } = null;

		/// <summary>
		/// Action to open an url in this web browser
		/// </summary>
		private Action<string> open = null;

		#endregion

		#region Instance functions

		/// <summary>
		/// Tries to open an url in this web browser
		/// </summary>
		/// <param name="url">The url to be opened</param>
		/// <exception>In case of an error</exception>
		/// <remarks>
		/// This functions succeeds as if the call could be delegated to the web browser application.
		/// There is no way to tell if opening the url within the web browser actually succeeded.
		/// </remarks>
		public void Open(string url)
		{
			if (open == null) throw new InvalidOperationException();
			open.Invoke(url);
		}

		/// <summary>
		/// Tries to open an url in this web browser
		/// </summary>
		/// <param name="url">The url to be opened</param>
		/// <exception>In case of an error</exception>
		/// <remarks>
		/// This functions succeeds as if the call could be delegated to the web browser application.
		/// There is no way to tell if opening the url within the web browser actually succeeded.
		/// </remarks>
		public void Open(Uri url)
		{
			Open(url.OriginalString);
		}

		#endregion

		#region Static Factory Functions

		/// <summary>
		/// Returns the list of installed web browsers
		/// </summary>
		/// <returns>
		/// An array of all detected, known, installed web browsers.
		///
		/// Returns `null` in case of an error.
		///
		/// If the function succeeds but detects no web browsers, one dummy object is returned representing that the operating system will do something when trying to open https links.
		/// </returns>
		static public WebBrowser[] GetInstalledBrowsers()
		{
			List<WebBrowser> browsers = new List<WebBrowser>();

			collectFromStartMenuInternet(ref browsers);

			collectFromHttpAssociation(ref browsers);

			if (!hasDefaultBrowser(browsers)) chooseDefaultFromStartMenuInternet(browsers);

			assertOneDefaultBrowser(ref browsers);
			return browsers.ToArray();
		}

		/// <summary>
		/// Returns the installed default web browser
		/// </summary>
		/// <returns>
		/// Returns `null` in case of an error.
		/// If the function succeeds but detects no web browsers, one dummy object is returned representing that the operating system will do something when trying to open https links.
		/// </returns>
		/// <remarks>
		/// Identical to: `GetDefaultBrowser(GetInstalledBrowsers())`
		/// </remarks>
		static public WebBrowser GetDefaultBrowser()
		{
			return GetDefaultBrowser(GetInstalledBrowsers());
		}

		/// <summary>
		/// Returns the first default web browser for an array of web browsers
		/// </summary>
		/// <param name="browsers">An array of web browsers</param>
		/// <returns>The first entry of the array with `IsDefault == true` or null, if no entry is marked as default</returns>
		static public WebBrowser GetDefaultBrowser(WebBrowser[] browsers)
		{
			foreach (var b in browsers)
				if (b.IsDefault) return b;
			return null;
		}

		#endregion

		#region Private static factory implementations

		/// <summary>
		/// Asserts that the list of browsers contains at least one browser.
		/// If it does not, it adds a generic default browser
		/// </summary>
		/// <param name="browsers">List of browsers</param>
		static private void assertOneDefaultBrowser(ref List<WebBrowser> browsers)
		{
			if (browsers == null) browsers = new List<WebBrowser>();
			if (browsers.Count <= 0) browsers.Add(new WebBrowser()
			{
				IsDefault = true,
				open = (string url) => {
					var psi = new ProcessStartInfo()
					{
						FileName = url,
						UseShellExecute = true
					};
					Process.Start(psi);
				}
			});
		}

		/// <summary>
		/// Checks if one of the browsers is marked as default browser
		/// </summary>
		/// <param name="browsers">List of browsers</param>
		/// <returns>True if at least one browser is marked as default browser</returns>
		private static bool hasDefaultBrowser(List<WebBrowser> browsers)
		{
			if (browsers == null) return false;
			if (browsers.Count <= 0) return false;
			foreach (var b in browsers)
				if (b.IsDefault) return true;
			return false;
		}

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

					WebBrowser b = new WebBrowser() {
						IsDefault = true,
						Name = progId
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

		/// <summary>
		/// Splits a command line string from the windows registry
		/// </summary>
		/// <param name="cmd">The command line string</param>
		/// <returns>The array of command line arguments</returns>
		private static string[] splitCmdLine(string cmd)
		{
			List<string> strs = new List<string>();
			string s = "";
			bool quoted = false;
			bool space = true;
			foreach (char c in cmd)
			{
				if (char.IsWhiteSpace(c))
					if (quoted)
						s += c;
					else if (space) continue;
					else
					{
						if (!string.IsNullOrWhiteSpace(s)) strs.Add(s);
						s = "";
						space = true;
					}
				else
				{
					space = false;
					s += c;
					if (c == '"')
						quoted = !quoted;
				}
			}
			if (!string.IsNullOrWhiteSpace(s)) strs.Add(s);

			var ar = strs.ToArray();
			for (int i = 0; i < ar.Length; ++i)
			{
				if (ar[i].Length > 1 && ar[i][0] == '"' && ar[i][ar[i].Length - 1] == '"')
				{
					ar[i] = ar[i].Substring(1, ar[i].Length - 2).Replace("\"\"", "\"");
				}
			}

			return ar;
		}

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
					using (var key3 = key2.OpenSubKey(defName)) {
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
		/// Invokes `call` for all possible "Software".
		/// Order:
		///  * HKCU
		///  * HKCU/WOW6432
		///  * HKLM
		///  * HKLM/WOW6432
		/// </summary>
		/// <param name="browsers">List of browsers</param>
		/// <param name="call">The call to invoke</param>
		static private void callForAllRegKeysSoftware(List<WebBrowser> browsers, Func<List<WebBrowser>, RegistryKey, bool> call)
		{
			RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software");
			if (key != null) if (!call(browsers, key)) return;
			key = Registry.CurrentUser.OpenSubKey(@"Software\WOW6432Node");
			if (key != null) if (!call(browsers, key)) return;
			key = Registry.LocalMachine.OpenSubKey(@"Software");
			if (key != null) if (!call(browsers, key)) return;
			key = Registry.LocalMachine.OpenSubKey(@"Software\WOW6432Node");
			if (key != null) if (!call(browsers, key)) return;
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

		/// <summary>
		/// If the executable path is empty, tries to guess it from other fields
		/// </summary>
		/// <param name="b">The browser object</param>
		private static void tryFixExecutablePath(WebBrowser b)
		{
			if (string.IsNullOrEmpty(b.ExecutablePath))
			{
				// try to recover executable path from other entries
				if (!string.IsNullOrEmpty(b.IconInfo))
				{
					Match m = Regex.Match(b.IconInfo, "^(.*\\.exe)(?:,[-0-9]+)?");
					if (m.Success)
					{
						b.ExecutablePath = m.Groups[1].Value;
					}
				}
			}
		}

		/// <summary>
		/// Based on the name, guesses the product family
		/// </summary>
		/// <param name="b">The browsers to guess the product family from</param>
		/// <returns>The guessed product family</returns>
		private static ProductFamily guessProductFamily(WebBrowser b)
		{
			ProductFamily pf = guessProductFamily(b.Name);
			if (pf == ProductFamily.Unknown) pf = guessProductFamily(b.ExecutablePath);
			if (pf == ProductFamily.Unknown) pf = guessProductFamily(b.IconInfo);
			return pf;
		}

		/// <summary>
		/// Based on the name, guesses the product family
		/// </summary>
		/// <param name="s">The string to guess the product family from</param>
		/// <returns>The guessed product family</returns>
		private static ProductFamily guessProductFamily(string s)
		{
			s = s.ToLower();
			if (s.Contains("mozilla firefox")) return ProductFamily.Firefox;
			if (s.Contains("google chrome")) return ProductFamily.Chrome;
			if (s.Contains("internet explorer")) return ProductFamily.InternetExplorer;
			if (s.Contains("microsoft edge")) return ProductFamily.Edge;
			if (s.Contains("firefox")) return ProductFamily.Firefox;
			if (s.Contains("chrome")) return ProductFamily.Chrome;
			if (s.Contains("iexplorer")) return ProductFamily.InternetExplorer;
			if (s.Contains("msedge")) return ProductFamily.Edge;
			if (s.Contains("edge")) return ProductFamily.Edge;
			return ProductFamily.Unknown;
		}

		/// <summary>
		/// Removes starting and ending quotes, if they exist
		/// </summary>
		/// <param name="v">Input string</param>
		/// <returns>Output string without enclosing quotes</returns>
		private static string unQuote(string v)
		{
			var s = v.Trim();
			if (s.Length > 2)
			{
				if (s[0] == '"' && s[s.Length - 1] == '"')
					return s.Substring(1, s.Length - 2);
			}
			return v;
		}

		/// <summary>
		/// Inserts a browser to the list of browsers, if it is not a duplicate
		/// </summary>
		/// <param name="browsers">List of browsers</param>
		/// <param name="b">The browser to insert</param>
		private static void insertBrowser(List<WebBrowser> browsers, WebBrowser b)
		{
			if (!string.IsNullOrEmpty(b.ExecutablePath))
			{
				foreach (var ob in browsers)
				{
					if (string.Equals(ob.ExecutablePath, b.ExecutablePath, StringComparison.InvariantCultureIgnoreCase))
					{
						if (b.IsDefault && !ob.IsDefault)
						{
							// let's assume the new browser is the better detection!
							ob.Name = b.Name;
							ob.IsDefault = true;
							ob.ProductFamily = b.ProductFamily;
							ob.open = b.open;
							if (!string.IsNullOrEmpty(b.IconInfo)) ob.IconInfo = b.IconInfo;
						}
						return;
					}
				}
			}

			browsers.Add(b);
		}

		#endregion
	}

}
