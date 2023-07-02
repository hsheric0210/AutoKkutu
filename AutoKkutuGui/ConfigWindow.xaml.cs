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
		DBAutoWordAdd.IsChecked = config.AutoDBWordAddEnabled;
		DBAutoWordRemove.IsChecked = config.AutoDBWordRemoveEnabled;
		DBAutoWordAddEnd.IsChecked = config.AutoDBAddEndEnabled;
		AttackWord.IsChecked = config.AttackWordEnabled;
		EndWord.IsChecked = config.EndWordEnabled;
		ReturnMode.IsChecked = config.ReturnModeEnabled;
		AutoFix.IsChecked = config.AutoFixEnabled;
		MissionDetection.IsChecked = config.MissionAutoDetectionEnabled;
		Delay.IsChecked = config.DelayEnabled;
		DelayPerWord.IsChecked = config.DelayPerCharEnabled;
		DelayNumber.Text = config.DelayInMillis.ToString(CultureInfo.InvariantCulture);
		DelayStartAfterWordEnter.IsChecked = config.DelayStartAfterWordEnterEnabled;
		InputSimulate.IsChecked = config.InputSimulate;
		MaxWordCount.Text = config.MaxDisplayedWordCount.ToString(CultureInfo.InvariantCulture);
		FixDelay.IsChecked = config.FixDelayEnabled;
		FixDelayPerWord.IsChecked = config.FixDelayPerCharEnabled;
		FixDelayNumber.Text = config.FixDelayInMillis.ToString(CultureInfo.InvariantCulture);
		RandomWordSelection.IsChecked = config.RandomWordSelection;
		RandomWordSelectionCount.Text = config.RandomWordSelectionCount.ToString(CultureInfo.InvariantCulture);
		SendKeyEvents.IsChecked = config.SendKeyEvents;
		HijackACPackets.IsChecked = config.HijackACPackets;
		SimulateAntiCheat.IsChecked = config.SimulateAntiCheat;
		DetectInputLogging.IsChecked = config.DetectInputLogging;
		LogChatting.IsChecked = config.LogChatting;
		SelfAntiCheat.IsChecked = config.SelfAntiCheat;

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

		var rwsNumber = FixDelayNumber.Text;
		if (!int.TryParse(rwsNumber, out var _rwsCount))
		{
			_rwsCount = 5;
			Log.Warning("Can't parse random word selection count number {string}; will be reset to {default:l}.", rwsNumber, _fixdelay);
		}

		// :)
		var conf = new Preference
		{
			InactiveWordPreference = new WordPreference(PreferenceReorderList.GetInactiveItemArray().Select(s => s.NodeType).ToArray()),
			ActiveWordPreference = new WordPreference(PreferenceReorderList.GetActiveItemArray().Select(s => s.NodeType).ToArray()),
			DelayStartAfterWordEnterEnabled = DelayStartAfterWordEnter.IsChecked ?? false,
			MissionAutoDetectionEnabled = MissionDetection.IsChecked ?? false,
			AutoDBWordRemoveEnabled = DBAutoWordRemove.IsChecked ?? false,
			RandomWordSelection = RandomWordSelection.IsChecked ?? false,
			FixDelayPerCharEnabled = FixDelayPerWord.IsChecked ?? false,
			DetectInputLogging = DetectInputLogging.IsChecked ?? false,
			AutoDBAddEndEnabled = DBAutoWordAddEnd.IsChecked ?? false,
			SimulateAntiCheat = SimulateAntiCheat.IsChecked ?? false,
			AutoDBWordAddEnabled = DBAutoWordAdd.IsChecked ?? false,
			AutoDBUpdateEnabled = DBAutoUpdate.IsChecked ?? false,
			DelayPerCharEnabled = DelayPerWord.IsChecked ?? false,
			HijackACPackets = HijackACPackets.IsChecked ?? false,
			ReturnModeEnabled = ReturnMode.IsChecked ?? false,
			AttackWordEnabled = AttackWord.IsChecked ?? false,
			SelfAntiCheat = SelfAntiCheat.IsChecked ?? false,
			SendKeyEvents = SendKeyEvents.IsChecked ?? false,
			InputSimulate = InputSimulate.IsChecked ?? false,
			AutoEnterEnabled = AutoEnter.IsChecked ?? false,
			FixDelayEnabled = FixDelay.IsChecked ?? false,
			LogChatting = LogChatting.IsChecked ?? false,
			EndWordEnabled = EndWord.IsChecked ?? false,
			AutoFixEnabled = AutoFix.IsChecked ?? false,
			DelayEnabled = Delay.IsChecked ?? false,
			RandomWordSelectionCount = _rwsCount,
			MaxDisplayedWordCount = MaxWords,
			FixDelayInMillis = _fixdelay,
			DelayInMillis = _delay,
		};

		try
		{
			Settings config = Settings.Default;
			config.LogChatting = conf.LogChatting;
			config.DelayEnabled = conf.DelayEnabled;
			config.DelayInMillis = conf.DelayInMillis;
			config.InputSimulate = conf.InputSimulate;
			config.SendKeyEvents = conf.SendKeyEvents;
			config.SelfAntiCheat = conf.SelfAntiCheat;
			config.EndWordEnabled = conf.EndWordEnabled;
			config.AutoFixEnabled = conf.AutoFixEnabled;
			config.FixDelayEnabled = conf.FixDelayEnabled;
			config.HijackACPackets = conf.HijackACPackets;
			config.FixDelayInMillis = conf.FixDelayInMillis;
			config.AutoEnterEnabled = conf.AutoEnterEnabled;
			config.ReturnModeEnabled = conf.ReturnModeEnabled;
			config.AttackWordEnabled = conf.AttackWordEnabled;
			config.SimulateAntiCheat = conf.SimulateAntiCheat;
			config.DetectInputLogging = conf.DetectInputLogging;
			config.DelayPerCharEnabled = conf.DelayPerCharEnabled;
			config.AutoDBUpdateEnabled = conf.AutoDBUpdateEnabled;
			config.AutoDBAddEndEnabled = conf.AutoDBAddEndEnabled;
			config.RandomWordSelection = conf.RandomWordSelection;
			config.AutoDBWordAddEnabled = conf.AutoDBWordAddEnabled;
			config.ActiveWordPreference = conf.ActiveWordPreference;
			config.MaxDisplayedWordCount = conf.MaxDisplayedWordCount;
			config.InactiveWordPreference = conf.InactiveWordPreference;
			config.FixDelayPerCharEnabled = conf.FixDelayPerCharEnabled;
			config.AutoDBWordRemoveEnabled = conf.AutoDBWordRemoveEnabled;
			config.RandomWordSelectionCount = conf.RandomWordSelectionCount;
			config.MissionAutoDetectionEnabled = conf.MissionAutoDetectionEnabled;
			config.DelayStartAfterWordEnterEnabled = conf.DelayStartAfterWordEnterEnabled;
			config.Save();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to save the configuration.");
		}

		Main.Prefs = conf;
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
