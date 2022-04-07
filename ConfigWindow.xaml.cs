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
using System.Windows.Shapes;

namespace AutoKkutu
{
	/// <summary>
	/// ConfigWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ConfigWindow : Window
	{
		public ConfigWindow(Config config)
		{
			InitializeComponent();
			DBAutoUpdateModeCB.ItemsSource = new string[] { Config.DBAUTOUPDATE_GAME_END, Config.DBAUTOUPDATE_ROUND_END };
			WordPreference.ItemsSource = new string[] { Config.WORDPREFERENCE_BY_DAMAGE, Config.WORDPREFERENCE_BY_LENGTH };

			AutoEnter.IsChecked = config.AutoEnter;
			DBAutoUpdate.IsChecked = config.AutoDBUpdate;
			DBAutoUpdateModeCB.SelectedIndex = config.AutoDBUpdateMode;
			WordPreference.SelectedIndex = config.WordPreference;
			EndWord.IsChecked = config.UseEndWord;
			ReturnMode.IsChecked = config.ReturnMode;
			AutoFix.IsChecked = config.AutoFix;
			MissionDetection.IsChecked = config.MissionDetection;
			ReverseMode.IsChecked = config.ReverseMode;
		}

		private void Submit_Click(object sender, RoutedEventArgs e)
		{
			Dispatcher.Invoke(() => MainWindow.UpdateConfig(new Config
			{
				AutoEnter = AutoEnter.IsChecked ?? false,
				AutoDBUpdate = DBAutoUpdate.IsChecked ?? false,
				AutoDBUpdateMode = DBAutoUpdateModeCB.SelectedIndex,
				WordPreference = WordPreference.SelectedIndex,
				UseEndWord = EndWord.IsChecked ?? false,
				ReturnMode = ReturnMode.IsChecked ?? false,
				AutoFix = AutoFix.IsChecked ?? false,
				MissionDetection = MissionDetection.IsChecked ?? false,
				ReverseMode = ReverseMode.IsChecked ?? false
			}));
			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
