using log4net;
using System;
using System.Linq;
using System.Windows;

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
			AttackWord.IsChecked = config.UseAttackWord;
			EndWord.IsChecked = config.UseEndWord;
			ReturnMode.IsChecked = config.ReturnMode;
			AutoFix.IsChecked = config.AutoFix;
			MissionDetection.IsChecked = config.MissionDetection;
			GameMode.SelectedIndex = (int)config.Mode;
			Delay.IsChecked = config.DelayEnabled;
			DelayPerWord.IsChecked = config.DelayPerWord;
			DelayNumber.Text = config.Delay.ToString();
			DelayStartAfterWordEnter.IsChecked = config.DelayStartAfterWordEnter;
			GameModeAutoDetect.IsChecked = config.GameModeAutoDetect;
			MaxWordCount.Text = config.MaxWords.ToString();
			FixDelay.IsChecked = config.FixDelayEnabled;
			FixDelayPerWord.IsChecked = config.FixDelayPerWord;
			FixDelayNumber.Text = config.FixDelay.ToString();
		}

		private void Submit_Click(object sender, RoutedEventArgs e)
		{
			string delayNumber = DelayNumber.Text;
			if (!int.TryParse(delayNumber, out int _delay))
			{
				_delay = 10;
				Logger.WarnFormat("Can't parse delay number '{0}'; reset to {1}", delayNumber, _delay);
			}

			string maxWordNumber = MaxWordCount.Text;
			if (!int.TryParse(maxWordNumber, out int MaxWords))
			{
				MaxWords = 20;
				Logger.WarnFormat("Can't parse maxWordCount number '{0}'; reset to {1}", maxWordNumber, MaxWords);
			}

			string fixDelayNumber = FixDelayNumber.Text;
			if (!int.TryParse(fixDelayNumber, out int _fixdelay))
			{
				_fixdelay = 10;
				Logger.WarnFormat("Can't parse fix delay number '{0}'; reset to {1}", fixDelayNumber, _fixdelay);
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
						UseAttackWord = AttackWord.IsChecked ?? false,
						UseEndWord = EndWord.IsChecked ?? false,
						ReturnMode = ReturnMode.IsChecked ?? false,
						AutoFix = AutoFix.IsChecked ?? false,
						MissionDetection = MissionDetection.IsChecked ?? false,
						Mode = ConfigEnums.GameModeValues[GameMode.SelectedIndex],
						DelayEnabled = Delay.IsChecked ?? false,
						DelayPerWord = DelayPerWord.IsChecked ?? false,
						Delay = _delay,
						DelayStartAfterWordEnter = DelayStartAfterWordEnter.IsChecked ?? false,
						GameModeAutoDetect = GameModeAutoDetect.IsChecked ?? false,
						MaxWords = MaxWords,
						FixDelayEnabled = FixDelay.IsChecked ?? false,
						FixDelayPerWord = FixDelayPerWord.IsChecked ?? false,
						FixDelay = _fixdelay
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
