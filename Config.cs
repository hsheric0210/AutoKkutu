using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu
{
	public class Config
	{
		public const string DBAUTOUPDATE_GAME_END = "게임이 끝났을 때";
		public const int DBAUTOUPDATE_GAME_END_INDEX = 0;
		public const string DBAUTOUPDATE_ROUND_END = "라운드가 끝났을 때";
		public const int DBAUTOUPDATE_GAME_ROUND_INDEX = 1;

		public const string WORDPREFERENCE_BY_DAMAGE = "단어의 공격력 우선";
		public const int WORDPREFERENCE_BY_DAMAGE_INDEX = 0;
		public const string WORDPREFERENCE_BY_LENGTH = "단어의 길이 우선";
		public const int WORDPREFERENCE_BY_LENGTH_INDEX = 1;

		public bool AutoEnter = true;
		public bool AutoDBUpdate = true;
		public int AutoDBUpdateMode = DBAUTOUPDATE_GAME_END_INDEX;
		public int WordPreference = WORDPREFERENCE_BY_DAMAGE_INDEX;
		public bool UseEndWord = false;
		public bool ReturnMode = false;
		public bool AutoFix = true;
		public bool MissionDetection = true;

		public Config()
		{
		}
	}
}
