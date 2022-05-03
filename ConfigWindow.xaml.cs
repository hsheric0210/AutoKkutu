using AutoKkutu.Config;
using AutoKkutu.Constants;
using log4net;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AutoKkutu
{
	/// <summary>
	/// ConfigWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ConfigWindow : Window
	{
		private static readonly ILog Logger = LogManager.GetLogger(nameof(ConfigWindow));
		private readonly ReorderableList<PreferenceItem> PreferenceReorderList;

		public ConfigWindow(AutoKkutuConfiguration config)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));

			InitializeComponent();

			PreferenceReorderList = new ReorderableList<PreferenceItem>(Preference, "Name", new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x37)), new SolidColorBrush(Color.FromRgb(0x23, 0x23, 0x27)));
			foreach (WordAttributes attr in config.WordPreference.GetAttributes())
				PreferenceReorderList.Add(new PreferenceItem(attr, WordPreference.GetName(attr)));

			DBAutoUpdateModeCB.ItemsSource = ConfigEnums.GetDBAutoUpdateModeValues().Select(ConfigEnums.GetDBAutoUpdateModeName);
			GameMode.ItemsSource = ConfigEnums.GetGameModeValues().Select(ConfigEnums.GetGameModeName);

			AutoEnter.IsChecked = config.AutoEnterEnabled;
			DBAutoUpdate.IsChecked = config.AutoDBUpdateEnabled;
			DBAutoUpdateModeCB.SelectedIndex = (int)config.AutoDBUpdateMode;
			AttackWord.IsChecked = config.AttackWordAllowed;
			EndWord.IsChecked = config.EndWordEnabled;
			ReturnMode.IsChecked = config.ReturnModeEnabled;
			AutoFix.IsChecked = config.AutoFixEnabled;
			MissionDetection.IsChecked = config.MissionAutoDetectionEnabled;
			GameMode.SelectedIndex = (int)config.GameMode;
			Delay.IsChecked = config.DelayEnabled;
			DelayPerWord.IsChecked = config.DelayPerWordEnabled;
			DelayNumber.Text = config.DelayInMillis.ToString(CultureInfo.InvariantCulture);
			DelayStartAfterWordEnter.IsChecked = config.DelayStartAfterWordEnterEnabled;
			GameModeAutoDetect.IsChecked = config.GameModeAutoDetectEnabled;
			MaxWordCount.Text = config.MaxDisplayedWordCount.ToString(CultureInfo.InvariantCulture);
			FixDelay.IsChecked = config.FixDelayEnabled;
			FixDelayPerWord.IsChecked = config.FixDelayPerWordEnabled;
			FixDelayNumber.Text = config.FixDelayInMillis.ToString(CultureInfo.InvariantCulture);
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
						AutoDBUpdateMode = ConfigEnums.GetDBAutoUpdateModeValues()[DBAutoUpdateModeCB.SelectedIndex],
						WordPreference = new WordPreference(PreferenceReorderList.ToArray().Select(s => s.NodeType).ToArray()), // Ugly solution. Should be fixed
						AttackWordAllowed = AttackWord.IsChecked ?? false,
						EndWordEnabled = EndWord.IsChecked ?? false,
						ReturnModeEnabled = ReturnMode.IsChecked ?? false,
						AutoFixEnabled = AutoFix.IsChecked ?? false,
						MissionAutoDetectionEnabled = MissionDetection.IsChecked ?? false,
						GameMode = ConfigEnums.GetGameModeValues()[GameMode.SelectedIndex],
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

		private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
	}
	public class PreferenceItem
	{
		public string Name
		{
			get;
		}

		public WordAttributes NodeType
		{
			get;
		}

		public PreferenceItem(WordAttributes nodeType, string name)
		{
			NodeType = nodeType;
			Name = name;
		}
	}
}
