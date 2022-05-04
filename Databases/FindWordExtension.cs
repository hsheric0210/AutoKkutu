using AutoKkutu.Constants;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;

namespace AutoKkutu.Databases
{
	public static class FindWordExtension
	{
		private static readonly ILog Logger = LogManager.GetLogger("Database Word Finder");

		private static string GetIndexColumnName(FindWordInfo opts)
		{
			switch (opts.Mode)
			{
				case GameMode.FirstAndLast:
					return DatabaseConstants.ReverseWordIndexColumnName;

				case GameMode.Kkutu:

					// TODO: 세 글자용 인덱스도 만들기
					ResponsePresentedWord word = opts.Word;
					if (word.Content.Length == 2 || word.CanSubstitution && word.Substitution.Length == 2)
						return DatabaseConstants.KkutuWordIndexColumnName;
					break;
			}
			return DatabaseConstants.WordIndexColumnName;
		}

		public static void GetOptimalWordDatabaseAttributes(GameMode mode, out int endWordFlag, out int attackWordFlag)
		{
			switch (mode)
			{
				case GameMode.FirstAndLast:
					endWordFlag = (int)WordDatabaseAttributes.ReverseEndWord;
					attackWordFlag = (int)WordDatabaseAttributes.ReverseAttackWord;
					return;

				case GameMode.MiddleAddFirst:
					endWordFlag = (int)WordDatabaseAttributes.MiddleEndWord;
					attackWordFlag = (int)WordDatabaseAttributes.MiddleAttackWord;
					return;

				case GameMode.Kkutu:
					endWordFlag = (int)WordDatabaseAttributes.KkutuEndWord;
					attackWordFlag = (int)WordDatabaseAttributes.KkutuAttackWord;
					return;
			}
			endWordFlag = (int)WordDatabaseAttributes.EndWord;
			attackWordFlag = (int)WordDatabaseAttributes.AttackWord;
		}

		private static WordAttributes GetPathObjectFlags(GetPathObjectFlagsInfo info, out int missionCharCount)
		{
			WordDatabaseAttributes WordDatabaseAttributes = info.WordDatabaseAttributes;
			WordAttributes wordAttributes = WordAttributes.None;
			if (WordDatabaseAttributes.HasFlag(info.EndWordFlag))
				wordAttributes |= WordAttributes.EndWord;
			if (WordDatabaseAttributes.HasFlag(info.AttackWordFlag))
				wordAttributes |= WordAttributes.AttackWord;

			string missionChar = info.MissionChar;
			if (!string.IsNullOrWhiteSpace(missionChar))
			{
				missionCharCount = info.Word.Count(c => c == missionChar[0]);
				if (missionCharCount > 0)
					wordAttributes |= WordAttributes.MissionWord;
			}
			else
			{
				missionCharCount = 0;
			}

			return wordAttributes;
		}

		public static ICollection<PathObject> FindWord(this CommonDatabaseConnection connection, FindWordInfo info)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			var result = new List<PathObject>();
			GetOptimalWordDatabaseAttributes(info.Mode, out int endWordFlag, out int attackWordFlag);
			string query = connection.CreateQuery(info, endWordFlag, attackWordFlag);
			Logger.DebugFormat("Execute query : {0}", query);
			using (DbDataReader reader = connection.ExecuteReader(query))
			{
				int wordOrdinal = reader.GetOrdinal(DatabaseConstants.WordColumnName);
				int flagsOrdinal = reader.GetOrdinal(DatabaseConstants.FlagsColumnName);
				while (reader.Read())
				{
					string word = reader.GetString(wordOrdinal).Trim();
					result.Add(new PathObject(word, GetPathObjectFlags(new GetPathObjectFlagsInfo
					{
						Word = word,
						WordDatabaseAttributes = (WordDatabaseAttributes)reader.GetInt32(flagsOrdinal),
						MissionChar = info.MissionChar,
						EndWordFlag = (WordDatabaseAttributes)endWordFlag,
						AttackWordFlag = (WordDatabaseAttributes)attackWordFlag
					}, out int missionCharCount), missionCharCount));
				}
			}

			return result;
		}

		private static string CreateQuery(this CommonDatabaseConnection connection, FindWordInfo info, int endWordFlag, int attackWordFlag)
		{
			string condition;
			ResponsePresentedWord data = info.Word;
			string indexColumnName = GetIndexColumnName(info);
			if (data.CanSubstitution)
				condition = $"WHERE ({indexColumnName} = '{data.Content}' OR {indexColumnName} = '{data.Substitution}')";
			else
				condition = $"WHERE ({indexColumnName} = '{data.Content}')";

			var opt = new PreferenceInfo { PathFinderFlags = info.PathFinderFlags, WordPreference = info.WordPreference, Condition = "" };

			// 한방 단어
			ApplyFilter(PathFinderInfo.UseEndWord, endWordFlag, ref opt);

			// 공격 단어
			ApplyFilter(PathFinderInfo.UseAttackWord, attackWordFlag, ref opt);

			string orderCondition = $"({CreateRearrangeCondition(connection, info.MissionChar, info.WordPreference, endWordFlag, attackWordFlag)} + LENGTH({DatabaseConstants.WordColumnName}))";

			if (info.Mode == GameMode.All)
				condition = opt.Condition = "";

			return $"SELECT * FROM {DatabaseConstants.WordListTableName} {condition}{opt.Condition} ORDER BY {orderCondition} DESC LIMIT {DatabaseConstants.QueryResultLimit}";
		}

		private static string CreateRearrangeCondition(this CommonDatabaseConnection connection, string missionChar, WordPreference wordPreference, int endWordFlag, int attackWordFlag)
		{
			if (string.IsNullOrWhiteSpace(missionChar))
			{
				return string.Format(
					CultureInfo.InvariantCulture,
					"{0}({1}, {2}, {3}, {4}, {5}, {6})",
					connection.GetRearrangeFuncName(),
					DatabaseConstants.FlagsColumnName,
					endWordFlag,
					attackWordFlag,
					GetAttributeOrdinal(wordPreference, WordAttributes.EndWord),
					GetAttributeOrdinal(wordPreference, WordAttributes.AttackWord),
					GetAttributeOrdinal(wordPreference, WordAttributes.None));
			}
			else
			{
				return string.Format(
					CultureInfo.InvariantCulture,
					"{0}({1}, {2}, '{3}', {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11})",
					connection.GetRearrangeMissionFuncName(),
					DatabaseConstants.WordColumnName,
					DatabaseConstants.FlagsColumnName,
					missionChar,
					endWordFlag,
					attackWordFlag,
					GetAttributeOrdinal(wordPreference, WordAttributes.EndWord | WordAttributes.MissionWord),
					GetAttributeOrdinal(wordPreference, WordAttributes.EndWord),
					GetAttributeOrdinal(wordPreference, WordAttributes.AttackWord | WordAttributes.MissionWord),
					GetAttributeOrdinal(wordPreference, WordAttributes.AttackWord),
					GetAttributeOrdinal(wordPreference, WordAttributes.MissionWord),
					GetAttributeOrdinal(wordPreference, WordAttributes.None));
			}
		}

		private static int GetAttributeOrdinal(WordPreference preference, WordAttributes attributes)
		{
			WordAttributes[] fullAttribs = preference.GetAttributes();
			int index = Array.IndexOf(fullAttribs, attributes);
			return fullAttribs.Length - (index >= 0 ? index : fullAttribs.Length) - 1;
		}

		private static void ApplyFilter(PathFinderInfo targetFlag, int flag, ref PreferenceInfo info)
		{
			if (!info.PathFinderFlags.HasFlag(targetFlag))
				info.Condition += $" AND ({DatabaseConstants.FlagsColumnName} & {flag} = 0)";
		}

		private struct PreferenceInfo
		{
			public PathFinderInfo PathFinderFlags;

			public WordPreference WordPreference;

			public string Condition;
		}

		private struct GetPathObjectFlagsInfo
		{
			public string Word;

			public string MissionChar;

			public WordDatabaseAttributes WordDatabaseAttributes;

			public WordDatabaseAttributes EndWordFlag;

			public WordDatabaseAttributes AttackWordFlag;
		}
	}
}
