using AutoKkutuLib;
using System;

namespace AutoKkutuGui;

public class Configuration
{
	public bool AutoEnterEnabled
	{
		get; set;
	} = true;

	public bool AutoDBUpdateEnabled
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

	public bool AttackWordAllowed
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

	public bool DelayStartAfterCharEnterEnabled
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

	public Configuration()
	{
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
			DelayStartAfterCharEnterEnabled,
			MaxDisplayedWordCount,
			InputSimulate));

	public override bool Equals(object? obj)
	{
		if (obj is not Configuration other)
			return false;

		return DelayEnabled == other.DelayEnabled
			&& DelayInMillis == other.DelayInMillis
			&& InputSimulate == other.InputSimulate
			&& AutoFixEnabled == other.AutoFixEnabled
			&& EndWordEnabled == other.EndWordEnabled
			&& FixDelayEnabled == other.FixDelayEnabled
			&& AutoEnterEnabled == other.AutoEnterEnabled
			&& FixDelayInMillis == other.FixDelayInMillis
			&& AttackWordAllowed == other.AttackWordAllowed
			&& ReturnModeEnabled == other.ReturnModeEnabled
			&& AutoDBUpdateEnabled == other.AutoDBUpdateEnabled
			&& DelayPerCharEnabled == other.DelayPerCharEnabled
			&& ActiveWordPreference == other.ActiveWordPreference
			&& MaxDisplayedWordCount == other.MaxDisplayedWordCount
			&& InactiveWordPreference == other.InactiveWordPreference
			&& FixDelayPerCharEnabled == other.FixDelayPerCharEnabled
			&& MissionAutoDetectionEnabled == other.MissionAutoDetectionEnabled
			&& DelayStartAfterCharEnterEnabled == other.DelayStartAfterCharEnterEnabled;
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
