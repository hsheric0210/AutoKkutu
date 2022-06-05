using AutoKkutu.Config;
using AutoKkutu.Constants;
using NLog;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace AutoKkutu
{
	/// <summary>
	/// ConfigWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ConfigWindow : Window
	{
		private static readonly Logger Logger = LogManager.GetLogger(nameof(ConfigWindow));
		private readonly ChoosableReorderableList<PreferenceItem> PreferenceReorderList;

		public ConfigWindow(AutoKkutuConfiguration config)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));

			InitializeComponent();

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
			DelayPerWord.IsChecked = config.DelayPerCharEnabled;
			DelayNumber.Text = config.DelayInMillis.ToString(CultureInfo.InvariantCulture);
			DelayStartAfterWordEnter.IsChecked = config.DelayStartAfterCharEnterEnabled;
			GameModeAutoDetect.IsChecked = config.GameModeAutoDetectEnabled;
			MaxWordCount.Text = config.MaxDisplayedWordCount.ToString(CultureInfo.InvariantCulture);
			FixDelay.IsChecked = config.FixDelayEnabled;
			FixDelayPerWord.IsChecked = config.FixDelayPerCharEnabled;
			FixDelayNumber.Text = config.FixDelayInMillis.ToString(CultureInfo.InvariantCulture);

			PreferenceReorderList = new ChoosableReorderableList<PreferenceItem>(new ChoosableReorderableListUIElements(InactivePreferences, ActivePreferences, ActivatePreferenceButton, DeactivatePreferenceButton, MoveUpPreferenceButton, MoveDownPreferenceButton), "Name");

			if (config.ActiveWordPreference != null)
			{
				foreach (WordAttributes attr in config.ActiveWordPreference.GetAttributes())
					PreferenceReorderList.AddActive(new PreferenceItem(attr, WordPreference.GetName(attr)));
			}

			if (config.InactiveWordPreference != null)
			{
				foreach (WordAttributes attr in config.InactiveWordPreference.GetAttributes())
					PreferenceReorderList.AddInactive(new PreferenceItem(attr, WordPreference.GetName(attr)));
			}
		}

		private void OnApply(object sender, RoutedEventArgs e)
		{
			string delayNumber = DelayNumber.Text;
			if (!int.TryParse(delayNumber, out int _delay))
			{
				_delay = 10;
				Logger.Warn(CultureInfo.CurrentCulture, "Can't parse delay number {string}, will be reset to {default:l}.", delayNumber, _delay);
			}

			string maxWordNumber = MaxWordCount.Text;
			if (!int.TryParse(maxWordNumber, out int MaxWords))
			{
				MaxWords = 20;
				Logger.Warn(CultureInfo.CurrentCulture, "Can't parse maxWordCount number {string}; will be reset to {default:l}.", maxWordNumber, MaxWords);
			}

			string fixDelayNumber = FixDelayNumber.Text;
			if (!int.TryParse(fixDelayNumber, out int _fixdelay))
			{
				_fixdelay = 10;
				Logger.Warn(CultureInfo.CurrentCulture, "Can't parse fix delay number {string}; will be reset to {default:l}.", fixDelayNumber, _fixdelay);
			}

			var conf = new AutoKkutuConfiguration
			{
				AutoEnterEnabled = AutoEnter.IsChecked ?? false,
				AutoDBUpdateEnabled = DBAutoUpdate.IsChecked ?? false,
				AutoDBUpdateMode = ConfigEnums.GetDBAutoUpdateModeValues()[DBAutoUpdateModeCB.SelectedIndex],
				ActiveWordPreference = new WordPreference(PreferenceReorderList.GetActiveItemArray().Select(s => s.NodeType).ToArray()),
				InactiveWordPreference = new WordPreference(PreferenceReorderList.GetInactiveItemArray().Select(s => s.NodeType).ToArray()),
				AttackWordAllowed = AttackWord.IsChecked ?? false,
				EndWordEnabled = EndWord.IsChecked ?? false,
				ReturnModeEnabled = ReturnMode.IsChecked ?? false,
				AutoFixEnabled = AutoFix.IsChecked ?? false,
				MissionAutoDetectionEnabled = MissionDetection.IsChecked ?? false,
				GameMode = ConfigEnums.GetGameModeValues()[GameMode.SelectedIndex],
				DelayEnabled = Delay.IsChecked ?? false,
				DelayPerCharEnabled = DelayPerWord.IsChecked ?? false,
				DelayInMillis = _delay,
				DelayStartAfterCharEnterEnabled = DelayStartAfterWordEnter.IsChecked ?? false,
				GameModeAutoDetectEnabled = GameModeAutoDetect.IsChecked ?? false,
				MaxDisplayedWordCount = MaxWords,
				FixDelayEnabled = FixDelay.IsChecked ?? false,
				FixDelayPerCharEnabled = FixDelayPerWord.IsChecked ?? false,
				FixDelayInMillis = _fixdelay
			};

			try
			{
				Settings config = Settings.Default;
				config.AutoEnterEnabled = conf.AutoEnterEnabled;
				config.AutoDBUpdateEnabled = conf.AutoDBUpdateEnabled;
				config.AutoDBUpdateMode = conf.AutoDBUpdateMode;
				config.ActiveWordPreference = conf.ActiveWordPreference;
				config.InactiveWordPreference = conf.InactiveWordPreference;
				config.AttackWordEnabled = conf.AttackWordAllowed;
				config.EndWordEnabled = conf.EndWordEnabled;
				config.ReturnModeEnabled = conf.ReturnModeEnabled;
				config.AutoFixEnabled = conf.AutoFixEnabled;
				config.MissionAutoDetectionEnabled = conf.MissionAutoDetectionEnabled;
				config.DelayEnabled = conf.DelayEnabled;
				config.DelayPerCharEnabled = conf.DelayPerCharEnabled;
				config.DelayInMillis = conf.DelayInMillis;
				config.DelayStartAfterWordEnterEnabled = conf.DelayStartAfterCharEnterEnabled;
				config.GameModeAutoDetectionEnabled = conf.GameModeAutoDetectEnabled;
				config.MaxDisplayedWordCount = conf.MaxDisplayedWordCount;
				config.FixDelayEnabled = conf.FixDelayEnabled;
				config.FixDelayPerCharEnabled = conf.FixDelayPerCharEnabled;
				config.FixDelayInMillis = conf.FixDelayInMillis;
				Settings.Default.Save();
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to save the configuration.");
			}

			Dispatcher.Invoke(() =>
			{
				try
				{
					MainWindow.UpdateConfig(conf);
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Failed to apply the configuration.");
				}
			});
			Close();
		}

		private void OnCancel(object sender, RoutedEventArgs e) => Close();
	}

	public sealed class PreferenceItem : IEquatable<PreferenceItem>
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

		public override int GetHashCode() => HashCode.Combine(Name.GetHashCode(StringComparison.OrdinalIgnoreCase), NodeType.GetHashCode());

		public override bool Equals(object? obj) => obj is PreferenceItem other && Equals(other);

		public bool Equals(PreferenceItem? other) => other != null && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) && NodeType == other.NodeType;
	}
}
