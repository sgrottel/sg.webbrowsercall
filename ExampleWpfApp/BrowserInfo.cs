using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace ExampleWpfApp
{
	public class BrowserInfo
	{
		public string Name { get; set; } = "Dummy";

		public ImageSource Icon { get; set; } = (ImageSource)App.Current.Resources["Icon_Web_x32"];

		public bool IsDefault { get; set; } = false;

		public Action<string> Open { get; set; } = null;

		internal bool LoadIcon(string iconInfo)
		{
			Match iim = Regex.Match(iconInfo, "^(.*)(?:,[-0-9]+)?$");
			if (!iim.Success) return false;
			if (!iim.Groups[1].Success) return false;
			string filename = iim.Groups[1].Value;
			if (!System.IO.File.Exists(filename)) return false;

			using (var icon = System.Drawing.Icon.ExtractAssociatedIcon(filename))
			{
				Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
					icon.Handle,
					System.Windows.Int32Rect.Empty,
					System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
			}
			return true;
		}
	}
}
