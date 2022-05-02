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
		} = DBAutoUpdateMode.GAME_END;

		public WordPreference WordPreference
		{
			get; set;
		} = WordPreference.ATTACK_DAMAGE;

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
		} = GameMode.Last_and_First;

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
			if (!(obj is AutoKkutuConfiguration))
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
		public static DBAutoUpdateMode[] DBAutoUpdateModeValues => (DBAutoUpdateMode[])Enum.GetValues(typeof(DBAutoUpdateMode));

		public static WordPreference[] WordPreferenceValues => (WordPreference[])Enum.GetValues(typeof(WordPreference));

		public static GameMode[] GameModeValues => (GameMode[])Enum.GetValues(typeof(GameMode));

		public static string GetDBAutoUpdateModeName(DBAutoUpdateMode key)
		{
			switch (key)
			{
				case DBAutoUpdateMode.GAME_END:
					return "게임이 끝났을 때";

				case DBAutoUpdateMode.ROUND_END:
					return "라운드가 끝났을 때";

				default:
					return null;
			}
		}

		public static string GetWordPreferenceName(WordPreference key)
		{
			switch (key)
			{
				case WordPreference.ATTACK_DAMAGE:
					return "단어의 공격력 우선";

				case WordPreference.WORD_LENGTH:
					return "단어의 길이 우선";

				default:
					return null;
			}
		}

		public static string GetGameModeName(GameMode key)
		{
			switch (key)
			{
				case GameMode.Last_and_First:
					return "끝말잇기";

				case GameMode.First_and_Last:
					return "앞말잇기";

				case GameMode.Middle_and_First:
					return "가운뎃말잇기";

				case GameMode.Kkutu:
					return "끄투";

				case GameMode.Kung_Kung_Tta:
					return "쿵쿵따";

				case GameMode.Typing_Battle:
					return "타자 대결";

				case GameMode.All:
					return "전체";

				case GameMode.Free:
					return "자유";

				case GameMode.Free_Last_and_First:
					return "자유 끝말잇기";

				default:
					return null;
			}
		}

		public static bool IsFreeMode(GameMode mode)
		{
			return mode == GameMode.Free || mode == GameMode.Free_Last_and_First;
		}
	}

	public enum DBAutoUpdateMode
	{
		GAME_END,
		ROUND_END
	}

	public enum WordPreference
	{
		ATTACK_DAMAGE,
		WORD_LENGTH
	}

	public enum GameMode
	{
		/// <summary>
		/// 끝말잇기
		/// </summary>
		Last_and_First,

		/// <summary>
		/// 앞말잇기
		/// </summary>
		First_and_Last,

		/// <summary>
		/// 가운뎃말잇기
		/// </summary>
		Middle_and_First,

		/// <summary>
		/// 끄투
		/// </summary>
		Kkutu,

		/// <summary>
		/// 쿵쿵따
		/// </summary>
		Kung_Kung_Tta,

		/// <summary>
		/// 타자 대결
		/// </summary>
		Typing_Battle,

		/// <summary>
		/// 전체
		/// </summary>
		All,

		/// <summary>
		/// 자유
		/// </summary>
		Free,

		/// <summary>
		/// 자유 끝말잇기
		/// </summary>
		Free_Last_and_First
	}
}
