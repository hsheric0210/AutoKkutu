using System;

namespace AutoKkutu
{
	public class AutoKkutuConfiguration
	{
		public bool AutoEnter = true;
		public bool AutoDBUpdate = true;
		public DBAutoUpdateMode AutoDBUpdateMode = DBAutoUpdateMode.GAME_END;
		public WordPreference WordPreference = WordPreference.ATTACK_DAMAGE;
		public bool UseEndWord = false;
		public bool UseAttackWord = true;
		public bool ReturnMode = false;
		public bool AutoFix = true;
		public bool MissionDetection = true;
		public GameMode Mode = GameMode.Last_and_First;
		public bool DelayEnabled = false;
		public bool DelayPerWord = true;
		public int Delay = 10;
		public bool DelayStartAfterWordEnter = true;
		public bool GameModeAutoDetect = true;
		public int MaxWords = 20;
		public bool FixDelayEnabled = false;
		public bool FixDelayPerWord = true;
		public int FixDelay = 10;

		public AutoKkutuConfiguration()
		{
		}

		public override int GetHashCode() => HashCode.Combine(HashCode.Combine(AutoEnter, AutoDBUpdate, AutoDBUpdateMode, WordPreference, UseEndWord, ReturnMode, AutoFix, MissionDetection), HashCode.Combine(Mode, DelayEnabled, DelayPerWord, Delay, DelayStartAfterWordEnter, GameModeAutoDetect, MaxWords));
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
					return "중간말잇기";
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
		Last_and_First,
		First_and_Last,
		Middle_and_First,
		Kkutu,
		Kung_Kung_Tta,
		Typing_Battle,
		All,
		Free,
		Free_Last_and_First
	}
}
