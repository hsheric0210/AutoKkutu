using AutoKkutuGui.Config;
using AutoKkutuLib;
using Serilog;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace AutoKkutuGui;

/// <summary>
/// ConfigWindow.xaml에 대한 상호 작용 논리
/// </summary>
public partial class ConfigWindow : Window
{
	private readonly ChoosableReorderableList<PreferenceItem> PreferenceReorderList;

	public ConfigWindow(Preference config)
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));

		InitializeComponent();

		AutoEnter.IsChecked = config.AutoEnterEnabled;
		DBAutoUpdate.IsChecked = config.AutoDBUpdateEnabled;
		AttackWord.IsChecked = config.AttackWordAllowed;
		EndWord.IsChecked = config.EndWordEnabled;
		ReturnMode.IsChecked = config.ReturnModeEnabled;
		AutoFix.IsChecked = config.AutoFixEnabled;
		MissionDetection.IsChecked = config.MissionAutoDetectionEnabled;
		Delay.IsChecked = config.DelayEnabled;
		DelayPerWord.IsChecked = config.DelayPerCharEnabled;
		DelayNumber.Text = config.DelayInMillis.ToString(CultureInfo.InvariantCulture);
		DelayStartAfterWordEnter.IsChecked = config.DelayStartAfterCharEnterEnabled;
		InputSimulate.IsChecked = config.InputSimulate;
		MaxWordCount.Text = config.MaxDisplayedWordCount.ToString(CultureInfo.InvariantCulture);
		FixDelay.IsChecked = config.FixDelayEnabled;
		FixDelayPerWord.IsChecked = config.FixDelayPerCharEnabled;
		FixDelayNumber.Text = config.FixDelayInMillis.ToString(CultureInfo.InvariantCulture);

		PreferenceReorderList = new ChoosableReorderableList<PreferenceItem>(new ChoosableReorderableListUIElements(InactivePreferences, ActivePreferences, ActivatePreferenceButton, DeactivatePreferenceButton, MoveUpPreferenceButton, MoveDownPreferenceButton), "Name");

		if (config.ActiveWordPreference != null)
		{
			foreach (WordCategories attr in config.ActiveWordPreference.GetAttributes())
				PreferenceReorderList.AddActive(new PreferenceItem(attr, WordPreference.GetName(attr)));
		}

		if (config.InactiveWordPreference != null)
		{
			foreach (WordCategories attr in config.InactiveWordPreference.GetAttributes())
				PreferenceReorderList.AddInactive(new PreferenceItem(attr, WordPreference.GetName(attr)));
		}
	}

	private void OnApply(object sender, RoutedEventArgs e)
	{
		var delayNumber = DelayNumber.Text;
		if (!int.TryParse(delayNumber, out var _delay))
		{
			_delay = 10;
			Log.Warning("Can't parse delay number {string}, will be reset to {default:l}.", delayNumber, _delay);
		}

		var maxWordNumber = MaxWordCount.Text;
		if (!int.TryParse(maxWordNumber, out var MaxWords))
		{
			MaxWords = 20;
			Log.Warning("Can't parse maxWordCount number {string}; will be reset to {default:l}.", maxWordNumber, MaxWords);
		}

		var fixDelayNumber = FixDelayNumber.Text;
		if (!int.TryParse(fixDelayNumber, out var _fixdelay))
		{
			_fixdelay = 10;
			Log.Warning("Can't parse fix delay number {string}; will be reset to {default:l}.", fixDelayNumber, _fixdelay);
		}

		// :)
		var conf = new Preference
		{
			ActiveWordPreference = new WordPreference(PreferenceReorderList.GetActiveItemArray().Select(s => s.NodeType).ToArray()),
			InactiveWordPreference = new WordPreference(PreferenceReorderList.GetInactiveItemArray().Select(s => s.NodeType).ToArray()),
			DelayStartAfterCharEnterEnabled = DelayStartAfterWordEnter.IsChecked ?? false,
			MissionAutoDetectionEnabled = MissionDetection.IsChecked ?? false,
			FixDelayPerCharEnabled = FixDelayPerWord.IsChecked ?? false,
			AutoDBUpdateEnabled = DBAutoUpdate.IsChecked ?? false,
			DelayPerCharEnabled = DelayPerWord.IsChecked ?? false,
			ReturnModeEnabled = ReturnMode.IsChecked ?? false,
			AttackWordAllowed = AttackWord.IsChecked ?? false,
			InputSimulate = InputSimulate.IsChecked ?? false,
			AutoEnterEnabled = AutoEnter.IsChecked ?? false,
			FixDelayEnabled = FixDelay.IsChecked ?? false,
			EndWordEnabled = EndWord.IsChecked ?? false,
			AutoFixEnabled = AutoFix.IsChecked ?? false,
			DelayEnabled = Delay.IsChecked ?? false,
			MaxDisplayedWordCount = MaxWords,
			FixDelayInMillis = _fixdelay,
			DelayInMillis = _delay,
		};

		try
		{
			Settings config = Settings.Default;
			config.DelayEnabled = conf.DelayEnabled;
			config.DelayInMillis = conf.DelayInMillis;
			config.InputSimulate = conf.InputSimulate;
			config.EndWordEnabled = conf.EndWordEnabled;
			config.AutoFixEnabled = conf.AutoFixEnabled;
			config.FixDelayEnabled = conf.FixDelayEnabled;
			config.FixDelayInMillis = conf.FixDelayInMillis;
			config.AutoEnterEnabled = conf.AutoEnterEnabled;
			config.ReturnModeEnabled = conf.ReturnModeEnabled;
			config.AttackWordEnabled = conf.AttackWordAllowed;
			config.DelayPerCharEnabled = conf.DelayPerCharEnabled;
			config.AutoDBUpdateEnabled = conf.AutoDBUpdateEnabled;
			config.ActiveWordPreference = conf.ActiveWordPreference;
			config.MaxDisplayedWordCount = conf.MaxDisplayedWordCount;
			config.InactiveWordPreference = conf.InactiveWordPreference;
			config.FixDelayPerCharEnabled = conf.FixDelayPerCharEnabled;
			config.MissionAutoDetectionEnabled = conf.MissionAutoDetectionEnabled;
			config.DelayStartAfterWordEnterEnabled = conf.DelayStartAfterCharEnterEnabled;
			Settings.Default.Save();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to save the configuration.");
		}

		Main.Configuration = conf;
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

	public WordCategories NodeType
	{
		get;
	}

	public PreferenceItem(WordCategories nodeType, string name)
	{
		NodeType = nodeType;
		Name = name;
	}

	public override int GetHashCode() => HashCode.Combine(Name.GetHashCode(StringComparison.OrdinalIgnoreCase), NodeType.GetHashCode());

	public override bool Equals(object? obj) => obj is PreferenceItem other && Equals(other);

	public bool Equals(PreferenceItem? other) => other != null && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) && NodeType == other.NodeType;
}
