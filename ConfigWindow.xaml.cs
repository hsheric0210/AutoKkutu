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

			AutoEnter.IsChecked = config.AutoEnterEnabled;
			DBAutoUpdate.IsChecked = config.AutoDBUpdateEnabled;
			DBAutoUpdateModeCB.SelectedIndex = (int)config.AutoDBUpdateMode;
			WordPreference.SelectedIndex = (int)config.WordPreference;
			AttackWord.IsChecked = config.AttackWordAllowed;
			EndWord.IsChecked = config.EndWordEnabled;
			ReturnMode.IsChecked = config.ReturnModeEnabled;
			AutoFix.IsChecked = config.AutoFixEnabled;
			MissionDetection.IsChecked = config.MissionAutoDetectionEnabled;
			GameMode.SelectedIndex = (int)config.GameMode;
			Delay.IsChecked = config.DelayEnabled;
			DelayPerWord.IsChecked = config.DelayPerWordEnabled;
			DelayNumber.Text = config.DelayInMillis.ToString();
			DelayStartAfterWordEnter.IsChecked = config.DelayStartAfterWordEnterEnabled;
			GameModeAutoDetect.IsChecked = config.GameModeAutoDetectEnabled;
			MaxWordCount.Text = config.MaxDisplayedWordCount.ToString();
			FixDelay.IsChecked = config.FixDelayEnabled;
			FixDelayPerWord.IsChecked = config.FixDelayPerWordEnabled;
			FixDelayNumber.Text = config.FixDelayInMillis.ToString();
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
						AutoEnterEnabled = AutoEnter.IsChecked ?? false,
						AutoDBUpdateEnabled = DBAutoUpdate.IsChecked ?? false,
						AutoDBUpdateMode = ConfigEnums.DBAutoUpdateModeValues[DBAutoUpdateModeCB.SelectedIndex],
						WordPreference = ConfigEnums.WordPreferenceValues[WordPreference.SelectedIndex],
						AttackWordAllowed = AttackWord.IsChecked ?? false,
						EndWordEnabled = EndWord.IsChecked ?? false,
						ReturnModeEnabled = ReturnMode.IsChecked ?? false,
						AutoFixEnabled = AutoFix.IsChecked ?? false,
						MissionAutoDetectionEnabled = MissionDetection.IsChecked ?? false,
						GameMode = ConfigEnums.GameModeValues[GameMode.SelectedIndex],
						DelayEnabled = Delay.IsChecked ?? false,
						DelayPerWordEnabled = DelayPerWord.IsChecked ?? false,
						DelayInMillis = _delay,
						DelayStartAfterWordEnterEnabled = DelayStartAfterWordEnter.IsChecked ?? false,
						GameModeAutoDetectEnabled = GameModeAutoDetect.IsChecked ?? false,
						MaxDisplayedWordCount = MaxWords,
						FixDelayEnabled = FixDelay.IsChecked ?? false,
						FixDelayPerWordEnabled = FixDelayPerWord.IsChecked ?? false,
						FixDelayInMillis = _fixdelay
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
