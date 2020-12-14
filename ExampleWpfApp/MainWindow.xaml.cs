using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ExampleWpfApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		public MainWindow()
		{
			InitializeComponent();

			List<BrowserInfo> browsers
				= Array.ConvertAll<SG.WebBrowserCall.WebBrowser, BrowserInfo>(
					SG.WebBrowserCall.WebBrowser.GetInstalledBrowsers(),
					(SG.WebBrowserCall.WebBrowser b) =>
					{
						var bi = new BrowserInfo()
						{
							Name = b.Name,
							IsDefault = b.IsDefault,
							Open = (string s) => { b.Open(s); }
						};
						if (!bi.LoadIcon(b.IconInfo))
						{
							bi.LoadIcon(b.ExecutablePath);
						}
						return bi;
					}
					).ToList();
			browsers.Sort((BrowserInfo a, BrowserInfo b) => { return a.Name.CompareTo(b.Name); });

			BrowserButtons.ItemsSource = browsers;
		}

		private void BrowserButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				BrowserInfo browser = (BrowserInfo)((Button)sender).DataContext;
				browser.Open.Invoke(TextBoxUrl.Text);
			}
			catch(Exception ex)
			{
				Message.Text = "Failed to invoke WebBrowser: " + ex.ToString();
			}

		}
	}
}
