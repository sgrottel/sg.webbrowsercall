using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace SG.WebBrowserCall
{
	/// <summary>
	/// Generic utility functions for the WebBrowser static factory function implementations
	/// </summary>
	partial class WebBrowser
	{

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
							ob.Name = selectBetterName(ob.Name, b.Name);
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

		/// <summary>
		/// Heuristic to select the better name out of two
		/// </summary>
		/// <param name="name1">The first name option</param>
		/// <param name="name2">The second name option</param>
		/// <returns>The better name</returns>
		private static string selectBetterName(string name1, string name2)
		{
			if (string.IsNullOrEmpty(name1)) return name2;
			if (string.IsNullOrEmpty(name2)) return name1;

			if (name1.Contains(name2, StringComparison.CurrentCultureIgnoreCase)) return name1;
			if (name2.Contains(name1, StringComparison.CurrentCultureIgnoreCase)) return name2;

			return name1; // unclear, does not matter
		}
	}
}
