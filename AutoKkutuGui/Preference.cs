using AutoKkutuLib;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace AutoKkutuGui;

public sealed class Preference : IEquatable<Preference?>
{
	// 단어 검색

	public bool EndWordEnabled { get; set; }

	public bool AttackWordEnabled { get; set; } = true;

	public bool ReturnModeEnabled { get; set; }

	public bool MissionAutoDetectionEnabled { get; set; } = true;

	public int MaxDisplayedWordCount { get; set; } = 20;

	// 자동 단어 입력

	public bool AutoEnterEnabled { get; set; } = true;

	public bool DelayEnabled { get; set; }

	public int StartDelay { get; set; } = 10;

	public int StartDelayRandom { get; set; } = 10;

	public int DelayPerChar { get; set; } = 10;

	public int DelayPerCharRandom { get; set; } = 10;

	public bool DelayStartAfterWordEnterEnabled { get; set; } = true;

	public bool InputSimulate { get; set; }

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

	public bool SendKeyEvents { get; set; } = true;

	public bool HijackACPackets { get; set; }

	public bool SimulateAntiCheat { get; set; }

	public bool DetectInputLogging { get; set; }

	public bool LogChatting { get; set; }

	public bool SelfAntiCheat { get; set; }

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
		EndWordEnabled = config.EndWordEnabled;
		AttackWordEnabled = config.AttackWordEnabled;
		ReturnModeEnabled = config.ReturnModeEnabled;
		MissionAutoDetectionEnabled = config.MissionAutoDetectionEnabled;
		MaxDisplayedWordCount = config.MaxDisplayedWordCount;

		// 자동 단어 입력
		AutoEnterEnabled = config.AutoEnterEnabled;
		DelayEnabled = config.DelayEnabled;
		StartDelay = (config.DelayInMillis > 0) ? config.DelayInMillis : config.StartDelay; // Backward compatibility
		StartDelayRandom = config.StartDelayRandom;
		DelayPerChar = (config.DelayInMillis > 0 && config.DelayPerCharEnabled) ? config.DelayInMillis : config.DelayPerChar; // Backward compatibility
		DelayPerCharRandom = config.DelayPerCharRandom;

		DelayStartAfterWordEnterEnabled = config.DelayStartAfterWordEnterEnabled;
		InputSimulate = config.InputSimulate;

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
		SendKeyEvents = config.SendKeyEvents;
		HijackACPackets = config.HijackACPackets;
		SimulateAntiCheat = config.SimulateAntiCheat;
		DetectInputLogging = config.DetectInputLogging;
		LogChatting = config.LogChatting;
		SelfAntiCheat = config.SelfAntiCheat;

		// 단어 우선순외
		ActiveWordPreference = config.ActiveWordPreference;
		InactiveWordPreference = config.InactiveWordPreference;
	}

	public override bool Equals(object? obj) => Equals(obj as Preference);

	public bool Equals(Preference? other) => other is not null && EndWordEnabled == other.EndWordEnabled && AttackWordEnabled == other.AttackWordEnabled && ReturnModeEnabled == other.ReturnModeEnabled && MissionAutoDetectionEnabled == other.MissionAutoDetectionEnabled && MaxDisplayedWordCount == other.MaxDisplayedWordCount && AutoEnterEnabled == other.AutoEnterEnabled && DelayEnabled == other.DelayEnabled && StartDelay == other.StartDelay && StartDelayRandom == other.StartDelayRandom && DelayPerChar == other.DelayPerChar && DelayPerCharRandom == other.DelayPerCharRandom && DelayStartAfterWordEnterEnabled == other.DelayStartAfterWordEnterEnabled && InputSimulate == other.InputSimulate && AutoFixEnabled == other.AutoFixEnabled && FixDelayEnabled == other.FixDelayEnabled && FixStartDelay == other.FixStartDelay && FixStartDelayRandom == other.FixStartDelayRandom && FixDelayPerChar == other.FixDelayPerChar && FixDelayPerCharRandom == other.FixDelayPerCharRandom && RandomWordSelection == other.RandomWordSelection && RandomWordSelectionCount == other.RandomWordSelectionCount && AutoDBUpdateEnabled == other.AutoDBUpdateEnabled && AutoDBWordAddEnabled == other.AutoDBWordAddEnabled && AutoDBWordRemoveEnabled == other.AutoDBWordRemoveEnabled && AutoDBAddEndEnabled == other.AutoDBAddEndEnabled && SendKeyEvents == other.SendKeyEvents && HijackACPackets == other.HijackACPackets && SimulateAntiCheat == other.SimulateAntiCheat && DetectInputLogging == other.DetectInputLogging && LogChatting == other.LogChatting && SelfAntiCheat == other.SelfAntiCheat && EqualityComparer<WordPreference>.Default.Equals(ActiveWordPreference, other.ActiveWordPreference) && EqualityComparer<WordPreference>.Default.Equals(InactiveWordPreference, other.InactiveWordPreference);

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(EndWordEnabled);
		hash.Add(AttackWordEnabled);
		hash.Add(ReturnModeEnabled);
		hash.Add(MissionAutoDetectionEnabled);
		hash.Add(MaxDisplayedWordCount);
		hash.Add(AutoEnterEnabled);
		hash.Add(DelayEnabled);
		hash.Add(StartDelay);
		hash.Add(StartDelayRandom);
		hash.Add(DelayPerChar);
		hash.Add(DelayPerCharRandom);
		hash.Add(DelayStartAfterWordEnterEnabled);
		hash.Add(InputSimulate);
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
		hash.Add(SendKeyEvents);
		hash.Add(HijackACPackets);
		hash.Add(SimulateAntiCheat);
		hash.Add(DetectInputLogging);
		hash.Add(LogChatting);
		hash.Add(SelfAntiCheat);
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
