using AutoKkutu.Constants;
using AutoKkutu.Modules;
using Dapper;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;

namespace AutoKkutu.Database.Extension
{
	public static class FindWordExtension
	{
		private static string GetIndexColumnName(GameMode mode, ResponsePresentedWord word)
		{
			switch (mode)
			{
				case GameMode.FirstAndLast:
					return DatabaseConstants.ReverseWordIndexColumnName;

				case GameMode.Kkutu:

					// TODO: 세 글자용 인덱스도 만들기
					if (word.Content.Length == 2 || word.CanSubstitution && word.Substitution!.Length == 2)
						return DatabaseConstants.KkutuWordIndexColumnName;
					break;
			}
			return DatabaseConstants.WordIndexColumnName;
		}

		public static void SelectWordEndAttackFlags(GameMode mode, out int endWordFlag, out int attackWordFlag)
		{
			switch (mode)
			{
				case GameMode.FirstAndLast:
					endWordFlag = (int)WordDbTypes.ReverseEndWord;
					attackWordFlag = (int)WordDbTypes.ReverseAttackWord;
					return;

				case GameMode.MiddleAndFirst:
					endWordFlag = (int)WordDbTypes.MiddleEndWord;
					attackWordFlag = (int)WordDbTypes.MiddleAttackWord;
					return;

				case GameMode.Kkutu:
					endWordFlag = (int)WordDbTypes.KkutuEndWord;
					attackWordFlag = (int)WordDbTypes.KkutuAttackWord;
					return;

				case GameMode.KungKungTta:
					endWordFlag = (int)WordDbTypes.KKTEndWord;
					attackWordFlag = (int)WordDbTypes.KKTAttackWord;
					return;
			}
			endWordFlag = (int)WordDbTypes.EndWord;
			attackWordFlag = (int)WordDbTypes.AttackWord;
		}

		private static WordCategories QueryWordCategories(
			string word,
			WordDbTypes wordFlags,
			string missionChar,
			WordDbTypes endWordFlag,
			WordDbTypes attackWordFlag,
			out int missionCharCount)
		{
			WordCategories category = WordCategories.None;
			if (wordFlags.HasFlag(endWordFlag))
				category |= WordCategories.EndWord;
			if (wordFlags.HasFlag(attackWordFlag))
				category |= WordCategories.AttackWord;

			if (!string.IsNullOrWhiteSpace(missionChar))
			{
				missionCharCount = word.Count(c => c == missionChar[0]);
				if (missionCharCount > 0)
					category |= WordCategories.MissionWord;
			}
			else
			{
				missionCharCount = 0;
			}

			return category;
		}

		public static IList<PathObject> FindWord(
			this AbstractDatabaseConnection connection,
			GameMode mode,
			ResponsePresentedWord word,
			string missionChar,
			WordPreference preference,
			PathFinderOptions options)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			var result = new List<PathObject>();
			SelectWordEndAttackFlags(mode, out int endWordFlag, out int attackWordFlag);
			FindQuery query = connection.CreateQuery(mode, word, missionChar, endWordFlag, attackWordFlag, preference, options);

			foreach (WordModel found in connection.Query<WordModel>(query.Sql, new DynamicParameters(query.Parameters)))
			{
				string wordString = found.Word.Trim();
				result.Add(new PathObject(wordString, QueryWordCategories(
					 wordString,
					(WordDbTypes)found.Flags,
					missionChar,
					(WordDbTypes)endWordFlag,
					(WordDbTypes)attackWordFlag,
					out int missionCharCount), missionCharCount));
			}

			return result;
		}

		private static FindQuery CreateQuery(
			this AbstractDatabaseConnection connection,
			GameMode mode,
			ResponsePresentedWord word,
			string missionChar,
			int endWordFlag,
			int attackWordFlag,
			WordPreference preference,
			PathFinderOptions options)
		{
			var param = new Dictionary<string, object>();

			string filter = "";

			if (mode != GameMode.All)
			{
				string wordIndexColumn = GetIndexColumnName(mode, word);
				param.Add("@PrimaryWord", word.Content);
				if (word.CanSubstitution)
				{
					filter = $" WHERE ({wordIndexColumn} = @PrimaryWord OR {wordIndexColumn} = @SecondaryWord)";
					param.Add("@SecondaryWord", word.Substitution!);
				}
				else
				{
					filter = $" WHERE ({wordIndexColumn} = @PrimaryWord)";
				}

				// 한방 단어
				ApplyFilter(options, PathFinderOptions.UseEndWord, endWordFlag, ref filter);

				// 공격 단어
				ApplyFilter(options, PathFinderOptions.UseAttackWord, attackWordFlag, ref filter);

				// 쿵쿵따 모드에서는 쿵쿵따 전용 단어들만 추천
				if (mode == GameMode.KungKungTta)
					filter += $" AND ({DatabaseConstants.FlagsColumnName} & {(int)WordDbTypes.KKT3} != 0)";
			}
			string orderPriority = $"({CreateWordPriorityFuncCall(connection, missionChar, preference, endWordFlag, attackWordFlag, param)} + LENGTH({DatabaseConstants.WordColumnName}))";

			return new FindQuery($"SELECT {DatabaseConstants.WordColumnName}, {DatabaseConstants.FlagsColumnName} FROM {DatabaseConstants.WordTableName}{filter} ORDER BY {orderPriority} DESC LIMIT {DatabaseConstants.QueryResultLimit}", param);
		}

		private static string CreateWordPriorityFuncCall(
			this AbstractDatabaseConnection connection,
			string missionChar,
			WordPreference wordPreference,
			int endWordFlag,
			int attackWordFlag,
			IDictionary<string, object> param)
		{
			if (string.IsNullOrWhiteSpace(missionChar))
			{
				// WordPriority
				return string.Format(
					CultureInfo.InvariantCulture,
					"{0}({1}, {2}, {3}, {4}, {5}, {6})",
					connection.GetWordPriorityFuncName(),
					DatabaseConstants.FlagsColumnName,
					endWordFlag,
					attackWordFlag,
					GetWordTypePriority(wordPreference, WordCategories.EndWord), // End word
					GetWordTypePriority(wordPreference, WordCategories.AttackWord), // Attack word
					GetWordTypePriority(wordPreference, WordCategories.None)); // Normal word
			}
			else
			{
				param.Add("@MissionChar", missionChar);
				return string.Format(
					CultureInfo.InvariantCulture,
					"{0}({1}, {2}, @MissionChar, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10})",
					connection.GetMissionWordPriorityFuncName(),
					DatabaseConstants.WordColumnName,
					DatabaseConstants.FlagsColumnName,
					endWordFlag,
					attackWordFlag,
					GetWordTypePriority(wordPreference, WordCategories.EndWord | WordCategories.MissionWord), // End mission word
					GetWordTypePriority(wordPreference, WordCategories.EndWord), // End word
					GetWordTypePriority(wordPreference, WordCategories.AttackWord | WordCategories.MissionWord), // Attack mission word
					GetWordTypePriority(wordPreference, WordCategories.AttackWord), // Attack word
					GetWordTypePriority(wordPreference, WordCategories.MissionWord), // Mission word
					GetWordTypePriority(wordPreference, WordCategories.None)); // Normal word
			}
		}

		private static int GetWordTypePriority(WordPreference preference, WordCategories attributes)
		{
			WordCategories[] fullAttribs = preference.GetAttributes();
			int index = Array.IndexOf(fullAttribs, attributes);
			return fullAttribs.Length - (index >= 0 ? index : fullAttribs.Length) - 1;
		}

		private static void ApplyFilter(
			PathFinderOptions haystack,
			PathFinderOptions needle,
			int flag,
			ref string filter)
		{
			if (!haystack.HasFlag(needle))
				filter += $" AND ({DatabaseConstants.FlagsColumnName} & {flag} = 0)";
		}

		private sealed record FindQuery(string Sql, IDictionary<string, object> Parameters);
	}
}
