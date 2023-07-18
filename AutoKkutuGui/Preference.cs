using AutoKkutuLib;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace AutoKkutuGui;

public sealed class Preference : IEquatable<Preference?>
{
	public static Color DefaultEndWordColor { get; } = Color.FromRgb(0xFF, 0x11, 0x00);
	public static Color DefaultAttackWordColor { get; } = Color.FromRgb(0xFF, 0x80, 0x00);
	public static Color DefaultMissionWordColor { get; } = Color.FromRgb(0x40, 0xFF, 0x40);
	public static Color DefaultEndMissionWordColor { get; } = Color.FromRgb(0x20, 0xC0, 0xA8);
	public static Color DefaultAttackMissionWordColor { get; } = Color.FromRgb(0xFF, 0xFF, 0x40);

	// 단어 검색

	public bool OnlySearchOnMyTurn { get; set; }

	public bool EndWordEnabled { get; set; }

	public bool AttackWordEnabled { get; set; } = true;

	public bool ReturnModeEnabled { get; set; }

	public bool MissionAutoDetectionEnabled { get; set; } = true;

	public int MaxDisplayedWordCount { get; set; } = 20;

	// 자동 단어 입력

	public bool AutoEnterEnabled { get; set; } = true;

	public bool AutoEnterDelayEnabled { get; set; }

	public int AutoEnterStartDelay { get; set; } = 10;

	public int AutoEnterStartDelayRandom { get; set; } = 10;

	public int AutoEnterDelayPerChar { get; set; } = 10;

	public int AutoEnterDelayPerCharRandom { get; set; } = 10;

	public bool AutoEnterDelayStartAfterWordEnterEnabled { get; set; } = true;

	public bool AutoEnterInputSimulateJavaScriptSendKeys { get; set; } = true;

	public string ArduinoPort { get; set; }

	public int ArduinoBaudrate { get; set; }

	public string AutoEnterMode { get; set; }

	public bool AutoFixEnabled { get; set; } = true;

	public bool FixDelayEnabled { get; set; }

	public int FixStartDelay { get; set; } = 10;

	public int FixStartDelayRandom { get; set; } = 10;

	public int FixDelayPerChar { get; set; } = 10;

	public int FixDelayPerCharRandom { get; set; } = 10;

	public bool RandomWordSelection { get; set; } = true;

	public int RandomWordSelectionCount { get; set; } = 5;

	// 데이터베이스 자동 업데이트

	public bool AutoDBUpdateEnabled { get; set; } = true;

	public bool AutoDBWordAddEnabled { get; set; } = true;

	public bool AutoDBWordRemoveEnabled { get; set; } = true;

	public bool AutoDBAddEndEnabled { get; set; } = true;

	// 우회 관련

	public bool HijackACPackets { get; set; }

	public bool SimulateAntiCheat { get; set; }

	public bool DetectInputLogging { get; set; }

	public bool LogChatting { get; set; }

	public bool SelfAntiCheat { get; set; }

	public Color EndWordColor { get; set; }

	public Color AttackWordColor { get; set; }

	public Color MissionWordColor { get; set; }

	public Color EndMissionWordColor { get; set; }

	public Color AttackMissionWordColor { get; set; }

	public WordPreference ActiveWordPreference { get; set; } = new WordPreference(WordPreference.GetDefault());

	public WordPreference InactiveWordPreference { get; set; } = new WordPreference();

	public Preference()
	{
	}

	internal Preference(Settings config)
	{
		// 1. Copy ConfigWindow.xaml.cs OnApply try-catch block part
		// 2. Regex: /config\.([\w]+) = conf\.([\w]+)/     Replace: $1 = config.$2

		// 단어 검색
		OnlySearchOnMyTurn = config.OnlySearchOnMyTurn;
		EndWordEnabled = config.EndWordEnabled;
		AttackWordEnabled = config.AttackWordEnabled;
		ReturnModeEnabled = config.ReturnModeEnabled;
		MissionAutoDetectionEnabled = config.MissionAutoDetectionEnabled;
		MaxDisplayedWordCount = config.MaxDisplayedWordCount;

		// 자동 단어 입력
		AutoEnterEnabled = config.AutoEnterEnabled;
		AutoEnterDelayEnabled = config.DelayEnabled;
		AutoEnterStartDelay = (config.DelayInMillis > 0) ? config.DelayInMillis : config.StartDelay; // Backward compatibility
		AutoEnterStartDelayRandom = config.StartDelayRandom;
		AutoEnterDelayPerChar = (config.DelayInMillis > 0 && config.DelayPerCharEnabled) ? config.DelayInMillis : config.DelayPerChar; // Backward compatibility
		AutoEnterDelayPerCharRandom = config.DelayPerCharRandom;

		AutoEnterMode = config.AutoEnterMode;
		AutoEnterDelayStartAfterWordEnterEnabled = config.DelayStartAfterWordEnterEnabled;
		AutoEnterInputSimulateJavaScriptSendKeys = config.AutoEnterInputSimulateJavaScriptSendKeys;

		ArduinoPort = config.ArduinoPort;
		ArduinoBaudrate = config.ArduinoBaudrate;

		AutoFixEnabled = config.AutoFixEnabled;
		FixDelayEnabled = config.FixDelayEnabled;
		FixStartDelay = (config.FixDelayInMillis > 0) ? config.FixDelayInMillis : config.FixStartDelay; // Backward compatibility
		FixStartDelayRandom = config.FixStartDelayRandom;
		FixDelayPerChar = (config.FixDelayInMillis > 0 && config.FixDelayPerCharEnabled) ? config.FixDelayInMillis : config.FixDelayPerChar; // Backward compatibility
		FixDelayPerCharRandom = config.FixDelayPerCharRandom;

		RandomWordSelectionCount = config.RandomWordSelectionCount;
		RandomWordSelection = config.RandomWordSelection;

		// 데이터베이스 자동 업데이트
		AutoDBUpdateEnabled = config.AutoDBUpdateEnabled;
		AutoDBWordAddEnabled = config.AutoDBWordAddEnabled;
		AutoDBWordRemoveEnabled = config.AutoDBWordRemoveEnabled;
		AutoDBAddEndEnabled = config.AutoDBAddEndEnabled;

		// 우회 관련
		HijackACPackets = config.HijackACPackets;
		SimulateAntiCheat = config.SimulateAntiCheat;
		DetectInputLogging = config.DetectInputLogging;
		LogChatting = config.LogChatting;
		SelfAntiCheat = config.SelfAntiCheat;

		// 단어 색
		EndWordColor = config.EndWordColor.ToMediaColor();
		AttackWordColor = config.AttackWordColor.ToMediaColor();
		MissionWordColor = config.MissionWordColor.ToMediaColor();
		EndMissionWordColor = config.EndMissionWordColor.ToMediaColor();
		AttackMissionWordColor = config.AttackMissionWordColor.ToMediaColor();

		// 단어 우선순외
		ActiveWordPreference = config.ActiveWordPreference;
		InactiveWordPreference = config.InactiveWordPreference;
	}

	public override bool Equals(object? obj) => Equals(obj as Preference);
	public bool Equals(Preference? other) => other is not null && OnlySearchOnMyTurn == other.OnlySearchOnMyTurn && EndWordEnabled == other.EndWordEnabled && AttackWordEnabled == other.AttackWordEnabled && ReturnModeEnabled == other.ReturnModeEnabled && MissionAutoDetectionEnabled == other.MissionAutoDetectionEnabled && MaxDisplayedWordCount == other.MaxDisplayedWordCount && AutoEnterEnabled == other.AutoEnterEnabled && AutoEnterDelayEnabled == other.AutoEnterDelayEnabled && AutoEnterStartDelay == other.AutoEnterStartDelay && AutoEnterStartDelayRandom == other.AutoEnterStartDelayRandom && AutoEnterDelayPerChar == other.AutoEnterDelayPerChar && AutoEnterDelayPerCharRandom == other.AutoEnterDelayPerCharRandom && AutoEnterDelayStartAfterWordEnterEnabled == other.AutoEnterDelayStartAfterWordEnterEnabled && AutoEnterInputSimulateJavaScriptSendKeys == other.AutoEnterInputSimulateJavaScriptSendKeys && ArduinoPort == other.ArduinoPort && ArduinoBaudrate == other.ArduinoBaudrate && AutoEnterMode == other.AutoEnterMode && AutoFixEnabled == other.AutoFixEnabled && FixDelayEnabled == other.FixDelayEnabled && FixStartDelay == other.FixStartDelay && FixStartDelayRandom == other.FixStartDelayRandom && FixDelayPerChar == other.FixDelayPerChar && FixDelayPerCharRandom == other.FixDelayPerCharRandom && RandomWordSelection == other.RandomWordSelection && RandomWordSelectionCount == other.RandomWordSelectionCount && AutoDBUpdateEnabled == other.AutoDBUpdateEnabled && AutoDBWordAddEnabled == other.AutoDBWordAddEnabled && AutoDBWordRemoveEnabled == other.AutoDBWordRemoveEnabled && AutoDBAddEndEnabled == other.AutoDBAddEndEnabled && HijackACPackets == other.HijackACPackets && SimulateAntiCheat == other.SimulateAntiCheat && DetectInputLogging == other.DetectInputLogging && LogChatting == other.LogChatting && SelfAntiCheat == other.SelfAntiCheat && EndWordColor.Equals(other.EndWordColor) && AttackWordColor.Equals(other.AttackWordColor) && MissionWordColor.Equals(other.MissionWordColor) && EndMissionWordColor.Equals(other.EndMissionWordColor) && AttackMissionWordColor.Equals(other.AttackMissionWordColor) && EqualityComparer<WordPreference>.Default.Equals(ActiveWordPreference, other.ActiveWordPreference) && EqualityComparer<WordPreference>.Default.Equals(InactiveWordPreference, other.InactiveWordPreference);

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(OnlySearchOnMyTurn);
		hash.Add(EndWordEnabled);
		hash.Add(AttackWordEnabled);
		hash.Add(ReturnModeEnabled);
		hash.Add(MissionAutoDetectionEnabled);
		hash.Add(MaxDisplayedWordCount);
		hash.Add(AutoEnterEnabled);
		hash.Add(AutoEnterDelayEnabled);
		hash.Add(AutoEnterStartDelay);
		hash.Add(AutoEnterStartDelayRandom);
		hash.Add(AutoEnterDelayPerChar);
		hash.Add(AutoEnterDelayPerCharRandom);
		hash.Add(AutoEnterDelayStartAfterWordEnterEnabled);
		hash.Add(AutoEnterInputSimulateJavaScriptSendKeys);
		hash.Add(ArduinoPort);
		hash.Add(ArduinoBaudrate);
		hash.Add(AutoEnterMode);
		hash.Add(AutoFixEnabled);
		hash.Add(FixDelayEnabled);
		hash.Add(FixStartDelay);
		hash.Add(FixStartDelayRandom);
		hash.Add(FixDelayPerChar);
		hash.Add(FixDelayPerCharRandom);
		hash.Add(RandomWordSelection);
		hash.Add(RandomWordSelectionCount);
		hash.Add(AutoDBUpdateEnabled);
		hash.Add(AutoDBWordAddEnabled);
		hash.Add(AutoDBWordRemoveEnabled);
		hash.Add(AutoDBAddEndEnabled);
		hash.Add(HijackACPackets);
		hash.Add(SimulateAntiCheat);
		hash.Add(DetectInputLogging);
		hash.Add(LogChatting);
		hash.Add(SelfAntiCheat);
		hash.Add(EndWordColor);
		hash.Add(AttackWordColor);
		hash.Add(MissionWordColor);
		hash.Add(EndMissionWordColor);
		hash.Add(AttackMissionWordColor);
		hash.Add(ActiveWordPreference);
		hash.Add(InactiveWordPreference);
		return hash.ToHashCode();
	}

	public static bool operator ==(Preference? left, Preference? right) => EqualityComparer<Preference>.Default.Equals(left, right);
	public static bool operator !=(Preference? left, Preference? right) => !(left == right);
}

public static class ConfigEnums
{
	public static WordCategories[] GetWordPreferenceValues() => (WordCategories[])Enum.GetValues(typeof(WordCategories));

	public static GameMode[] GetGameModeValues() => (GameMode[])Enum.GetValues(typeof(GameMode));

	public static string? GetGameModeName(GameMode key) => key switch
	{
		GameMode.LastAndFirst => I18n.GameMode_LastAndFirst,
		GameMode.FirstAndLast => I18n.GameMode_FirstAndLast,
		GameMode.MiddleAndFirst => I18n.GameMode_MiddleAndFirst,
		GameMode.Kkutu => I18n.GameMode_Kkutu,
		GameMode.KungKungTta => I18n.GameMode_KungKungTta,
		GameMode.TypingBattle => I18n.GameMode_TypingBattle,
		GameMode.All => I18n.GameMode_All,
		GameMode.Free => I18n.GameMode_Free,
		GameMode.LastAndFirstFree => I18n.GameMode_LastAndFirstFree,
		_ => null,
	};
}
