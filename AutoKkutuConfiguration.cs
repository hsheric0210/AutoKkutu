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

		public DBAutoUpdateMode AutoDBUpdateMode
		{
			get; set;
		} = DBAutoUpdateMode.OnGameEnd;

		public WordPreference WordPreference
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

		public bool DelayPerWordEnabled
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

		public bool FixDelayPerWordEnabled
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

		public override int GetHashCode() => HashCode.Combine(HashCode.Combine(AutoEnterEnabled, AutoDBUpdateEnabled, AutoDBUpdateMode, WordPreference, EndWordEnabled, ReturnModeEnabled, AutoFixEnabled, MissionAutoDetectionEnabled), HashCode.Combine(GameMode, DelayEnabled, DelayPerWordEnabled, DelayInMillis, DelayStartAfterWordEnterEnabled, GameModeAutoDetectEnabled, MaxDisplayedWordCount));

		public override bool Equals(object obj)
		{
			if (obj is not AutoKkutuConfiguration)
				return false;
			var other = obj as AutoKkutuConfiguration;

			// 편-안
			return GameMode == other.GameMode
				&& DelayEnabled == other.DelayEnabled
				&& DelayInMillis == other.DelayInMillis
				&& AutoFixEnabled == other.AutoFixEnabled
				&& WordPreference == other.WordPreference
				&& EndWordEnabled == other.EndWordEnabled
				&& FixDelayEnabled == other.FixDelayEnabled
				&& AutoDBUpdateMode == other.AutoDBUpdateMode
				&& AutoEnterEnabled == other.AutoEnterEnabled
				&& FixDelayInMillis == other.FixDelayInMillis
				&& AttackWordAllowed == other.AttackWordAllowed
				&& ReturnModeEnabled == other.ReturnModeEnabled
				&& AutoDBUpdateEnabled == other.AutoDBUpdateEnabled
				&& DelayPerWordEnabled == other.DelayPerWordEnabled
				&& MaxDisplayedWordCount == other.MaxDisplayedWordCount
				&& FixDelayPerWordEnabled == other.FixDelayPerWordEnabled
				&& GameModeAutoDetectEnabled == other.GameModeAutoDetectEnabled
				&& MissionAutoDetectionEnabled == other.MissionAutoDetectionEnabled
				&& DelayStartAfterWordEnterEnabled == other.DelayStartAfterWordEnterEnabled;
		}
	}

	public static class ConfigEnums
	{
		public static DBAutoUpdateMode[] GetDBAutoUpdateModeValues() => (DBAutoUpdateMode[])Enum.GetValues(typeof(DBAutoUpdateMode));

		public static WordAttributes[] GetWordPreferenceValues() => (WordAttributes[])Enum.GetValues(typeof(WordAttributes));

		public static GameMode[] GetGameModeValues() => (GameMode[])Enum.GetValues(typeof(GameMode));

		public static string GetDBAutoUpdateModeName(DBAutoUpdateMode key)
		{
			return key switch
			{
				DBAutoUpdateMode.OnGameEnd => "게임이 끝났을 때",
				DBAutoUpdateMode.OnRoundEnd => "라운드가 끝났을 때",
				_ => null,
			};
		}

		public static string GetGameModeName(GameMode key)
		{
			return key switch
			{
				GameMode.LastAndFirst => "끝말잇기",
				GameMode.FirstAndLast => "앞말잇기",
				GameMode.MiddleAddFirst => "가운뎃말잇기",
				GameMode.Kkutu => "끄투",
				GameMode.KungKungTta => "쿵쿵따",
				GameMode.TypingBattle => "타자 대결",
				GameMode.All => "전체",
				GameMode.Free => "자유",
				GameMode.LastAndFirstFree => "자유 끝말잇기",
				_ => null,
			};
		}

		public static bool IsFreeMode(GameMode mode) => mode is GameMode.Free or GameMode.LastAndFirstFree;
	}

	public enum DBAutoUpdateMode
	{
		OnGameEnd,
		OnRoundEnd
	}
}
