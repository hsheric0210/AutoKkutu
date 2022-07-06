using AutoKkutu.Constants;
using System;

namespace AutoKkutu
{
	public class AutoKkutuConfiguration
	{
		public bool AutoEnterEnabled
		{
			get; set;
		} = true;

		public bool AutoDBUpdateEnabled
		{
			get; set;
		} = true;

		public AutoDBUpdateMode AutoDBUpdateMode
		{
			get; set;
		} = AutoDBUpdateMode.OnGameEnd;

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

		public GameMode GameMode
		{
			get; set;
		} = GameMode.LastAndFirst;

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

		public bool GameModeAutoDetectEnabled
		{
			get; set;
		} = true;

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

		public AutoKkutuConfiguration()
		{
		}

		public override int GetHashCode() => HashCode.Combine(HashCode.Combine(AutoEnterEnabled, AutoDBUpdateEnabled, AutoDBUpdateMode, ActiveWordPreference, EndWordEnabled, ReturnModeEnabled, AutoFixEnabled, MissionAutoDetectionEnabled), HashCode.Combine(GameMode, DelayEnabled, DelayPerCharEnabled, DelayInMillis, DelayStartAfterCharEnterEnabled, GameModeAutoDetectEnabled, MaxDisplayedWordCount, InputSimulate));

		public override bool Equals(object? obj)
		{
			if (obj is not AutoKkutuConfiguration other)
				return false;

			// 편-안
			return GameMode == other.GameMode
				&& DelayEnabled == other.DelayEnabled
				&& DelayInMillis == other.DelayInMillis
				&& InputSimulate == other.InputSimulate
				&& AutoFixEnabled == other.AutoFixEnabled
				&& EndWordEnabled == other.EndWordEnabled
				&& FixDelayEnabled == other.FixDelayEnabled
				&& AutoDBUpdateMode == other.AutoDBUpdateMode
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
				&& GameModeAutoDetectEnabled == other.GameModeAutoDetectEnabled
				&& MissionAutoDetectionEnabled == other.MissionAutoDetectionEnabled
				&& DelayStartAfterCharEnterEnabled == other.DelayStartAfterCharEnterEnabled;
		}
	}

	public static class ConfigEnums
	{
		public static AutoDBUpdateMode[] GetDBAutoUpdateModeValues() => (AutoDBUpdateMode[])Enum.GetValues(typeof(AutoDBUpdateMode));

		public static WordAttributes[] GetWordPreferenceValues() => (WordAttributes[])Enum.GetValues(typeof(WordAttributes));

		public static GameMode[] GetGameModeValues() => (GameMode[])Enum.GetValues(typeof(GameMode));

		public static string? GetDBAutoUpdateModeName(AutoDBUpdateMode key) => key switch
		{
			AutoDBUpdateMode.OnGameEnd => I18n.AutoDBUpdate_OnGameEnd,
			AutoDBUpdateMode.OnRoundEnd => I18n.AutoDBUpdate_OnRoundEnd,
			_ => null,
		};

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

		public static bool IsFreeMode(GameMode mode) => mode is GameMode.Free or GameMode.LastAndFirstFree;
	}
}
