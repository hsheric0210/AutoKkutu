using log4net;
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
			DBAutoUpdateModeCB.ItemsSource = ConfigEnums.DBAutoUpdateModeValues.Select(ConfigEnums.GetDBAutoUpdateModeName);
			WordPreference.ItemsSource = ConfigEnums.WordPreferenceValues.Select(ConfigEnums.GetWordPreferenceName);
			GameMode.ItemsSource = ConfigEnums.GameModeValues.Select(ConfigEnums.GetGameModeName);

			AutoEnter.IsChecked = config.AutoEnter;
			DBAutoUpdate.IsChecked = config.AutoDBUpdate;
			DBAutoUpdateModeCB.SelectedIndex = (int)config.AutoDBUpdateMode;
			WordPreference.SelectedIndex = (int)config.WordPreference;
			EndWord.IsChecked = config.UseEndWord;
			ReturnMode.IsChecked = config.ReturnMode;
			AutoFix.IsChecked = config.AutoFix;
			MissionDetection.IsChecked = config.MissionDetection;
			GameMode.SelectedIndex = (int)config.Mode;
			Delay.IsChecked = config.Delay;
			DelayPerWord.IsChecked = config.DelayPerWord;
			DelayNumber.Text = config.nDelay.ToString();
			DelayStartAfterWordEnter.IsChecked = config.DelayStartAfterWordEnter;
		}

		private void Submit_Click(object sender, RoutedEventArgs e)
		{
			if (!int.TryParse(DelayNumber.Text, out int nDelay))
				nDelay = 10;
			Dispatcher.Invoke(() =>
			{
				try
				{
					MainWindow.UpdateConfig(new Config
					{
						AutoEnter = AutoEnter.IsChecked ?? false,
						AutoDBUpdate = DBAutoUpdate.IsChecked ?? false,
						AutoDBUpdateMode = ConfigEnums.DBAutoUpdateModeValues[DBAutoUpdateModeCB.SelectedIndex],
						WordPreference = ConfigEnums.WordPreferenceValues[WordPreference.SelectedIndex],
						UseEndWord = EndWord.IsChecked ?? false,
						ReturnMode = ReturnMode.IsChecked ?? false,
						AutoFix = AutoFix.IsChecked ?? false,
						MissionDetection = MissionDetection.IsChecked ?? false,
						Mode = ConfigEnums.GameModeValues[GameMode.SelectedIndex],
						Delay = Delay.IsChecked ?? false,
						DelayPerWord = DelayPerWord.IsChecked ?? false,
						nDelay = nDelay,
						DelayStartAfterWordEnter = DelayStartAfterWordEnter.IsChecked ?? false
					});
				}
				catch (Exception ex)
				{
					LogManager.GetLogger("Config").Error("Failed to apply configuration", ex);
				}
			});
			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
