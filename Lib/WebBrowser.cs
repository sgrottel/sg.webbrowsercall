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
	public partial class WebBrowser
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

	}

}
