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
		private static readonly ILog Logger = LogManager.GetLogger(nameof(ConfigWindow));

		public ConfigWindow(AutoKkutuConfiguration config)
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
			GameModeAutoDetect.IsChecked = config.GameModeAutoDetect;
			MaxWordCount.Text = config.MaxWords.ToString();
		}

		private void Submit_Click(object sender, RoutedEventArgs e)
		{
			string delayNumber = DelayNumber.Text;
			if (!int.TryParse(delayNumber, out int nDelay))
			{
				nDelay = 10;
				Logger.WarnFormat("Can't parse delay number '{0}'; reset to {1}", delayNumber, nDelay);
			}

			string maxWordNumber = MaxWordCount.Text;
			if (!int.TryParse(maxWordNumber, out int MaxWords))
			{
				MaxWords = 20;
				Logger.WarnFormat("Can't parse maxWordCount number '{0}'; reset to {1}", maxWordNumber, MaxWords);
			}

			Dispatcher.Invoke(() =>
			{
				try
				{
					MainWindow.UpdateConfig(new AutoKkutuConfiguration
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
						DelayStartAfterWordEnter = DelayStartAfterWordEnter.IsChecked ?? false,
						GameModeAutoDetect = GameModeAutoDetect.IsChecked ?? false,
						MaxWords = MaxWords
					});
				}
				catch (Exception ex)
				{
					Logger.Error("Failed to apply configuration", ex);
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
