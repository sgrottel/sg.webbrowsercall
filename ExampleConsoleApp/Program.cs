﻿using System;

namespace SG.WBC.ExampleConsoleApp
{

	/// <summary>
	/// Minimal test program, to print info about detected browsers to the console
	/// </summary>
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("SG.WebBrowserCall -- Example Console App");

			Console.WriteLine();
			Console.WriteLine("Empty Object:");
			try
			{
				var o = new WebBrowserCall.WebBrowser();
				printInfo(o);
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception: {0}", e);
			}

			Console.WriteLine();
			Console.WriteLine("Found Web Browsers:");
			try
			{
				var bs = WebBrowserCall.WebBrowser.GetInstalledBrowsers();
				if (bs != null)
				{
					foreach (var b in bs)
					{
						printInfo(b);
					}
				}
				else
				{
					Console.WriteLine("Returned Null Array");
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception: {0}", e);
			}

			Console.WriteLine();
			Console.WriteLine("Default Browser:");
			try
			{
				var o = WebBrowserCall.WebBrowser.GetDefaultBrowser();
				printInfo(o);
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception: {0}", e);
			}

			// Optional test to open URLs in all browsers
			// This is skipped on default, because it is annoying
			Console.WriteLine();
			Console.WriteLine("Opening pages in all browsers:");
			bool skipOpenTest = true;
			try
			{
				if (skipOpenTest)
				{
					Console.WriteLine("Skipped");
				}
				else
				{
					var bs = WebBrowserCall.WebBrowser.GetInstalledBrowsers();
					if (bs != null)
					{
						foreach (var b in bs)
						{
							printInfo(b);
							try
							{
								b.Open("https://www.sgrottel.de");
							}
							catch (Exception e)
							{
								Console.WriteLine("Exception: {0}", e);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception: {0}", e);
			}

			Console.WriteLine();
			Console.WriteLine("End");
		}

		/// <summary>
		/// Print info about a browser object
		/// </summary>
		/// <param name="o">A browser object</param>
		private static void printInfo(WebBrowserCall.WebBrowser o)
		{
			if (o == null)
			{
				Console.WriteLine("null object");
				return;
			}

			Console.WriteLine("\"{3}\" {0}{1} ({2}; {4})",
				new object[] {
					o.ProductFamily,
					o.IsDefault ? " [Default]" : "",
					o.ExecutablePath,
					o.Name,
					o.IconInfo
				});
		}
	}
}
