using AutoKkutuLib;
using System;
using System.Windows.Documents;

namespace AutoKkutuGui;

public class Preference
{
	public bool AutoEnterEnabled
	{
		get; set;
	} = true;

	public bool AutoDBUpdateEnabled
	{
		get; set;
	} = true;

	public bool AutoDBWordAddEnabled
	{
		get; set;
	} = true;

	public bool AutoDBWordRemoveEnabled
	{
		get; set;
	} = true;

	public bool AutoDBAddEndEnabled
	{
		get; set;
	} = true;

	public WordPreference ActiveWordPreference
	{
		get; set;
	} = new WordPreference(WordPreference.GetDefault());

	public WordPreference InactiveWordPreference
	{
		get; set;
	} = new WordPreference();

	public bool EndWordEnabled
	{
		get; set;
	}

	public bool AttackWordEnabled
	{
		get; set;
	} = true;

	public bool ReturnModeEnabled
	{
		get; set;
	}

	public bool AutoFixEnabled
	{
		get; set;
	} = true;

	public bool MissionAutoDetectionEnabled
	{
		get; set;
	} = true;

	public bool DelayEnabled
	{
		get; set;
	}

	public bool DelayPerCharEnabled
	{
		get; set;
	} = true;

	public int DelayInMillis
	{
		get; set;
	} = 10;

	public bool DelayStartAfterWordEnterEnabled
	{
		get; set;
	} = true;

	public bool InputSimulate
	{
		get; set;
	}

	public int MaxDisplayedWordCount
	{
		get; set;
	} = 20;

	public bool FixDelayEnabled
	{
		get; set;
	}

	public bool FixDelayPerCharEnabled
	{
		get; set;
	} = true;

	public int FixDelayInMillis
	{
		get; set;
	} = 10;

	public bool RandomWordSelection
	{
		get; set;
	} = true;

	public int RandomWordSelectionCount
	{
		get; set;
	} = 5;

	public bool SendKeyEvents
	{
		get; set;
	} = true;

	public bool HijackACPackets
	{
		get; set;
	}

	public bool SimulateAntiCheat
	{
		get; set;
	}

	public bool DetectInputLogging
	{
		get; set;
	}

	public bool LogChatting
	{
		get; set;
	}

	public bool SelfAntiCheat
	{
		get; set;
	}

	public Preference()
	{
	}

	internal Preference(Settings config)
	{
		// 1. Copy ConfigWindow.xaml.cs L135-163
		// 2. Regex: /config\.([\w]+) = conf\.([\w]+)/     Replace: $1 = config.$2
		LogChatting = config.LogChatting;
		DelayEnabled = config.DelayEnabled;
		DelayInMillis = config.DelayInMillis;
		InputSimulate = config.InputSimulate;
		SendKeyEvents = config.SendKeyEvents;
		SelfAntiCheat = config.SelfAntiCheat;
		EndWordEnabled = config.EndWordEnabled;
		AutoFixEnabled = config.AutoFixEnabled;
		FixDelayEnabled = config.FixDelayEnabled;
		HijackACPackets = config.HijackACPackets;
		FixDelayInMillis = config.FixDelayInMillis;
		AutoEnterEnabled = config.AutoEnterEnabled;
		ReturnModeEnabled = config.ReturnModeEnabled;
		AttackWordEnabled = config.AttackWordEnabled;
		SimulateAntiCheat = config.SimulateAntiCheat;
		DetectInputLogging = config.DetectInputLogging;
		DelayPerCharEnabled = config.DelayPerCharEnabled;
		AutoDBUpdateEnabled = config.AutoDBUpdateEnabled;
		AutoDBAddEndEnabled = config.AutoDBAddEndEnabled;
		RandomWordSelection = config.RandomWordSelection;
		AutoDBWordAddEnabled = config.AutoDBWordAddEnabled;
		ActiveWordPreference = config.ActiveWordPreference;
		MaxDisplayedWordCount = config.MaxDisplayedWordCount;
		InactiveWordPreference = config.InactiveWordPreference;
		FixDelayPerCharEnabled = config.FixDelayPerCharEnabled;
		AutoDBWordRemoveEnabled = config.AutoDBWordRemoveEnabled;
		RandomWordSelectionCount = config.RandomWordSelectionCount;
		MissionAutoDetectionEnabled = config.MissionAutoDetectionEnabled;
		DelayStartAfterWordEnterEnabled = config.DelayStartAfterWordEnterEnabled;
	}

	public override int GetHashCode() => HashCode.Combine(HashCode.Combine(
		AutoEnterEnabled,
		AutoDBUpdateEnabled,
		ActiveWordPreference,
		EndWordEnabled,
		ReturnModeEnabled,
		AutoFixEnabled,
		MissionAutoDetectionEnabled), HashCode.Combine(
			DelayEnabled,
			DelayPerCharEnabled,
			DelayInMillis,
			DelayStartAfterWordEnterEnabled,
			MaxDisplayedWordCount,
			InputSimulate,
			HashCode.Combine(RandomWordSelection,
			RandomWordSelectionCount,
			SendKeyEvents,
			HijackACPackets,
			SimulateAntiCheat,
			DetectInputLogging,
			LogChatting,
			SelfAntiCheat)));

	public override bool Equals(object? obj)
	{
		if (obj is not Preference other)
			return false;

		return LogChatting == other.LogChatting
			&& DelayEnabled == other.DelayEnabled
			&& DelayInMillis == other.DelayInMillis
			&& InputSimulate == other.InputSimulate
			&& SendKeyEvents == other.SendKeyEvents
			&& SelfAntiCheat == other.SelfAntiCheat
			&& AutoFixEnabled == other.AutoFixEnabled
			&& EndWordEnabled == other.EndWordEnabled
			&& FixDelayEnabled == other.FixDelayEnabled
			&& HijackACPackets == other.HijackACPackets
			&& AutoEnterEnabled == other.AutoEnterEnabled
			&& FixDelayInMillis == other.FixDelayInMillis
			&& AttackWordEnabled == other.AttackWordEnabled
			&& ReturnModeEnabled == other.ReturnModeEnabled
			&& SimulateAntiCheat == other.SimulateAntiCheat
			&& DetectInputLogging == other.DetectInputLogging
			&& AutoDBUpdateEnabled == other.AutoDBUpdateEnabled
			&& DelayPerCharEnabled == other.DelayPerCharEnabled
			&& RandomWordSelection == other.RandomWordSelection
			&& ActiveWordPreference == other.ActiveWordPreference
			&& MaxDisplayedWordCount == other.MaxDisplayedWordCount
			&& InactiveWordPreference == other.InactiveWordPreference
			&& FixDelayPerCharEnabled == other.FixDelayPerCharEnabled
			&& RandomWordSelectionCount == other.RandomWordSelectionCount
			&& MissionAutoDetectionEnabled == other.MissionAutoDetectionEnabled
			&& DelayStartAfterWordEnterEnabled == other.DelayStartAfterWordEnterEnabled;
	}
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
