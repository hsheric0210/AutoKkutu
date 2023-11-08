using AutoKkutuGui.Config;
using AutoKkutuLib;
using AutoKkutuLib.Game.Enterer;
using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using static AutoKkutuGui.GuiUtils;

namespace AutoKkutuGui;

/// <summary>
/// ConfigWindow.xaml에 대한 상호 작용 논리
/// </summary>
public partial class ConfigWindow : Window
{
	private readonly ChoosableReorderableList<PreferenceItem> preferenceReorderList;

	public event EventHandler<PreferenceUpdateEventArgs>? PreferenceUpdate;

	private string GetAutoEnterMode()
	{
		if (AutoEnterInputSimulateJavaScript.IsChecked ?? false)
			return JavaScriptInputSimulator.Name;
		if (AutoEnterInputSimulateWin32.IsChecked ?? false)
			return Win32InputSimulator.Name;
		if (AutoEnterInputSimulateArduino.IsChecked ?? false)
			return "ArduinoInputSimulator";
		return DelayedInstantEnterer.Name; // default
	}

	private void SetAutoEnterMode(string mode)
	{
		// Reset selection
		AutoEnterInstant.IsChecked = false;
		AutoEnterInputSimulateJavaScript.IsChecked = false;
		AutoEnterInputSimulateWin32.IsChecked = false;
		AutoEnterInputSimulateArduino.IsChecked = false;

		switch (mode)
		{
			case JavaScriptInputSimulator.Name:
				AutoEnterInputSimulateJavaScript.IsChecked = true;
				break;
			case Win32InputSimulator.Name:
				AutoEnterInputSimulateWin32.IsChecked = true;
				break;
			case "ArduinoInputSimulator":
				AutoEnterInputSimulateArduino.IsChecked = true;
				break;
			default:
				AutoEnterInstant.IsChecked = true;
				break;
		}
	}

	public ConfigWindow(Preference config)
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));

		InitializeComponent();

		// 단어 검색
		OnlySearchOnMyTurn.IsChecked = config.OnlySearchOnMyTurn;
		AttackWord.IsChecked = config.AttackWordEnabled;
		EndWord.IsChecked = config.EndWordEnabled;
		ReturnMode.IsChecked = config.ReturnModeEnabled;
		MissionDetection.IsChecked = config.MissionAutoDetectionEnabled;
		MaxWordCount.Text = config.MaxDisplayedWordCount.ToString(CultureInfo.InvariantCulture);

		// 자동 단어 입력
		AutoEnter.IsChecked = config.AutoEnterEnabled;
		EnterDelay.SetConfig(new EnterDelayConfig(config.AutoEnterDelayEnabled, config.AutoEnterStartDelay, config.AutoEnterStartDelayRandom, config.AutoEnterDelayPerChar, config.AutoEnterDelayPerCharRandom));

		SetAutoEnterMode(config.AutoEnterMode);
		DelayStartAfterWordEnter.IsChecked = config.AutoEnterDelayStartAfterWordEnterEnabled;
		AutoEnterInputSimulateJavaScriptSendKeys.IsChecked = config.AutoEnterInputSimulateJavaScriptSendKeys;

		ArduinoPort.Text = config.ArduinoPort;
		ArduinoBaudrate.Text = config.ArduinoBaudrate.ToString(CultureInfo.InvariantCulture);

		AutoFix.IsChecked = config.AutoFixEnabled;
		FixDelay.SetConfig(new EnterDelayConfig(config.FixDelayEnabled, config.FixStartDelay, config.FixStartDelayRandom, config.FixDelayPerChar, config.FixDelayPerCharRandom));

		RandomWordSelection.IsChecked = config.RandomWordSelection;
		RandomWordSelectionCount.Text = config.RandomWordSelectionCount.ToString(CultureInfo.InvariantCulture);

		// 데이터베이스 자동 업데이트
		DBAutoUpdate.IsChecked = config.AutoDBUpdateEnabled;
		DBAutoWordAdd.IsChecked = config.AutoDBWordAddEnabled;
		DBAutoWordRemove.IsChecked = config.AutoDBWordRemoveEnabled;
		DBAutoWordAddEnd.IsChecked = config.AutoDBAddEndEnabled;

		// 우회 관련
		HijackACPackets.IsChecked = config.HijackACPackets;

		SimulateAntiCheat.IsChecked = config.SimulateAntiCheat;
		DetectInputLogging.IsChecked = config.DetectInputLogging;
		LogChatting.IsChecked = config.LogChatting;
		SelfAntiCheat.IsChecked = config.SelfAntiCheat;

		EndWordColor.SelectedColor = config.EndWordColor;
		AttackWordColor.SelectedColor = config.AttackWordColor;
		MissionWordColor.SelectedColor = config.MissionWordColor;
		EndMissionWordColor.SelectedColor = config.EndMissionWordColor;
		AttackMissionWordColor.SelectedColor = config.AttackMissionWordColor;

		preferenceReorderList = new ChoosableReorderableList<PreferenceItem>(new ChoosableReorderableListUIElements(InactivePreferences, ActivePreferences, ActivatePreferenceButton, DeactivatePreferenceButton, MoveUpPreferenceButton, MoveDownPreferenceButton), "Name");

		if (config.ActiveWordPreference != null)
		{
			foreach (var attr in config.ActiveWordPreference.GetAttributes())
				preferenceReorderList.AddActive(new PreferenceItem(attr, WordPreference.GetName(attr)));
		}

		if (config.InactiveWordPreference != null)
		{
			foreach (var attr in config.InactiveWordPreference.GetAttributes())
				preferenceReorderList.AddInactive(new PreferenceItem(attr, WordPreference.GetName(attr)));
		}
	}

	private void OnApply(object sender, RoutedEventArgs e)
	{
		var maxWordCount = Parse("max word count", MaxWordCount.Text, 20);

		var enterDelay = EnterDelay.GetConfig();

		var arduinoBaudrate = Parse("arduino baudrate", ArduinoBaudrate.Text, 115200);

		var fixDelay = FixDelay.GetConfig();

		var rwsSelect = Parse("random word selection", RandomWordSelectionCount.Text, 5);

		var conf = new Preference
		{
			// 단어 검색
			OnlySearchOnMyTurn = OnlySearchOnMyTurn.IsChecked ?? false,
			EndWordEnabled = EndWord.IsChecked ?? false,
			AttackWordEnabled = AttackWord.IsChecked ?? false,
			ReturnModeEnabled = ReturnMode.IsChecked ?? false,
			MissionAutoDetectionEnabled = MissionDetection.IsChecked ?? false,
			MaxDisplayedWordCount = maxWordCount,

			// 자동 단어 입력
			AutoEnterEnabled = AutoEnter.IsChecked ?? false,
			AutoEnterDelayEnabled = enterDelay.IsEnabled,
			AutoEnterStartDelay = enterDelay.StartDelay,
			AutoEnterStartDelayRandom = enterDelay.StartDelayRandom,
			AutoEnterDelayPerChar = enterDelay.DelayPerChar,
			AutoEnterDelayPerCharRandom = enterDelay.DelayPerCharRandom,

			AutoEnterMode = GetAutoEnterMode(),
			AutoEnterDelayStartAfterWordEnterEnabled = DelayStartAfterWordEnter.IsChecked ?? false,
			AutoEnterInputSimulateJavaScriptSendKeys = AutoEnterInputSimulateJavaScriptSendKeys.IsChecked ?? false,

			ArduinoPort = ArduinoPort.Text,
			ArduinoBaudrate = arduinoBaudrate,

			AutoFixEnabled = AutoFix.IsChecked ?? false,
			FixDelayEnabled = fixDelay.IsEnabled,
			FixStartDelay = fixDelay.StartDelay,
			FixStartDelayRandom = fixDelay.StartDelayRandom,
			FixDelayPerChar = fixDelay.DelayPerChar,
			FixDelayPerCharRandom = fixDelay.DelayPerCharRandom,

			RandomWordSelection = RandomWordSelection.IsChecked ?? false,
			RandomWordSelectionCount = rwsSelect,

			// 데이터베이스 자동 업데이트
			AutoDBUpdateEnabled = DBAutoUpdate.IsChecked ?? false,
			AutoDBWordAddEnabled = DBAutoWordAdd.IsChecked ?? false,
			AutoDBWordRemoveEnabled = DBAutoWordRemove.IsChecked ?? false,
			AutoDBAddEndEnabled = DBAutoWordAddEnd.IsChecked ?? false,

			// 우회 관련
			HijackACPackets = HijackACPackets.IsChecked ?? false,
			SimulateAntiCheat = SimulateAntiCheat.IsChecked ?? false,
			DetectInputLogging = DetectInputLogging.IsChecked ?? false,
			LogChatting = LogChatting.IsChecked ?? false,
			SelfAntiCheat = SelfAntiCheat.IsChecked ?? false,

			// 단어 색
			EndWordColor = EndWordColor.SelectedColor ?? Preference.DefaultEndWordColor,
			AttackWordColor = AttackWordColor.SelectedColor ?? Preference.DefaultAttackWordColor,
			MissionWordColor = MissionWordColor.SelectedColor ?? Preference.DefaultMissionWordColor,
			EndMissionWordColor = EndMissionWordColor.SelectedColor ?? Preference.DefaultEndMissionWordColor,
			AttackMissionWordColor = AttackMissionWordColor.SelectedColor ?? Preference.DefaultAttackMissionWordColor,

			InactiveWordPreference = new WordPreference(preferenceReorderList.GetInactiveItemArray().Select(s => s.NodeType).ToArray()),
			ActiveWordPreference = new WordPreference(preferenceReorderList.GetActiveItemArray().Select(s => s.NodeType).ToArray()),
		};

		try
		{
			// FIXME: 프로그램 설정을 저장하면 색 강조 설정이 초기화됨(날라감). 그 반대도 마찬가지.
			var config = Settings.Default;

			// 단어 검색
			config.OnlySearchOnMyTurn = conf.OnlySearchOnMyTurn;
			config.EndWordEnabled = conf.EndWordEnabled;
			config.AttackWordEnabled = conf.AttackWordEnabled;
			config.ReturnModeEnabled = conf.ReturnModeEnabled;
			config.MissionAutoDetectionEnabled = conf.MissionAutoDetectionEnabled;
			config.MaxDisplayedWordCount = conf.MaxDisplayedWordCount;

			// 자동 단어 입력
			config.AutoEnterEnabled = conf.AutoEnterEnabled;
			config.DelayEnabled = conf.AutoEnterDelayEnabled;
			config.StartDelay = conf.AutoEnterStartDelay;
			config.StartDelayRandom = conf.AutoEnterStartDelayRandom;
			config.DelayPerChar = conf.AutoEnterDelayPerChar;
			config.DelayPerCharRandom = conf.AutoEnterDelayPerCharRandom;

			config.AutoEnterMode = conf.AutoEnterMode;
			config.DelayStartAfterWordEnterEnabled = conf.AutoEnterDelayStartAfterWordEnterEnabled;
			config.AutoEnterInputSimulateJavaScriptSendKeys = conf.AutoEnterInputSimulateJavaScriptSendKeys;

			config.ArduinoPort = conf.ArduinoPort;
			config.ArduinoBaudrate = conf.ArduinoBaudrate;

			config.AutoFixEnabled = conf.AutoFixEnabled;
			config.FixDelayEnabled = conf.FixDelayEnabled;
			config.FixStartDelay = conf.FixStartDelay;
			config.FixStartDelayRandom = conf.FixStartDelayRandom;
			config.FixDelayPerChar = conf.FixDelayPerChar;
			config.FixDelayPerCharRandom = conf.FixDelayPerCharRandom;

			config.RandomWordSelectionCount = conf.RandomWordSelectionCount;
			config.RandomWordSelection = conf.RandomWordSelection;

			// 데이터베이스 자동 업데이트
			config.AutoDBUpdateEnabled = conf.AutoDBUpdateEnabled;
			config.AutoDBWordAddEnabled = conf.AutoDBWordAddEnabled;
			config.AutoDBWordRemoveEnabled = conf.AutoDBWordRemoveEnabled;
			config.AutoDBAddEndEnabled = conf.AutoDBAddEndEnabled;

			// 우회 관련
			config.HijackACPackets = conf.HijackACPackets;
			config.SimulateAntiCheat = conf.SimulateAntiCheat;
			config.DetectInputLogging = conf.DetectInputLogging;
			config.LogChatting = conf.LogChatting;
			config.SelfAntiCheat = conf.SelfAntiCheat;

			// 단어 색
			config.EndWordColor = conf.EndWordColor.ToDrawingColor();
			config.AttackWordColor = conf.AttackWordColor.ToDrawingColor();
			config.MissionWordColor = conf.MissionWordColor.ToDrawingColor();
			config.EndMissionWordColor = conf.EndMissionWordColor.ToDrawingColor();
			config.AttackMissionWordColor = conf.AttackMissionWordColor.ToDrawingColor();

			// 단어 우선순외
			config.ActiveWordPreference = conf.ActiveWordPreference;
			config.InactiveWordPreference = conf.InactiveWordPreference;

			// 더 이상 사용되지 않는 옵션들
			config.DelayInMillis = -1;
			config.DelayPerCharEnabled = false;
			config.FixDelayInMillis = -1;
			config.FixDelayPerCharEnabled = false;

			config.Save();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to save the configuration.");
		}

		PreferenceUpdate?.Invoke(this, new PreferenceUpdateEventArgs(conf));
		Close();
	}

	private void OnCancel(object sender, RoutedEventArgs e) => Close();

	//https://stackoverflow.com/a/10238715
	private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
	{
		Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
		e.Handled = true;
	}
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

public class PreferenceUpdateEventArgs : EventArgs
{
	public Preference Preference { get; }
	public PreferenceUpdateEventArgs(Preference preference) => Preference = preference;
}