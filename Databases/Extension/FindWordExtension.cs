using AutoKkutu.Constants;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;

namespace AutoKkutu.Databases.Extension
{
	public static class FindWordExtension
	{
		private static readonly Logger Logger = LogManager.GetLogger("Database Word Finder");

		private static CommonDatabaseCommand? CachedCommand;
		private static string? PreviousQuery;

		private static string GetIndexColumnName(FindWordInfo opts)
		{
			switch (opts.Mode)
			{
				case GameMode.FirstAndLast:
					return DatabaseConstants.ReverseWordIndexColumnName;

				case GameMode.Kkutu:

					// TODO: 세 글자용 인덱스도 만들기
					ResponsePresentedWord word = opts.Word;
					if (word.Content.Length == 2 || word.CanSubstitution && word.Substitution!.Length == 2)
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

				case GameMode.KungKungTta:
					endWordFlag = (int)WordDatabaseAttributes.KKTEndWord;
					attackWordFlag = (int)WordDatabaseAttributes.KKTAttackWord;
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

		public static IList<PathObject> FindWord(this CommonDatabaseConnection connection, FindWordInfo info)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			var result = new List<PathObject>();
			GetOptimalWordDatabaseAttributes(info.Mode, out int endWordFlag, out int attackWordFlag);
			Query query = connection.CreateQuery(info, endWordFlag, attackWordFlag);

			if (CachedCommand == null || PreviousQuery?.Equals(query.Command, StringComparison.Ordinal) != true)
			{
				CachedCommand = connection.CreateCommand(query.Command);
				Logger.Debug(CultureInfo.CurrentCulture, "Query command : {command}", query.Command);

				int paramCount = query.Parameters.Length;
				var paramArray = new CommonDatabaseParameter[paramCount];
				for (int i = 0; i < paramCount; i++)
				{
					(string paramName, object? paramValue) = query.Parameters[i];
					paramArray[i] = connection.CreateParameter(paramName, paramValue);
					Logger.Debug(CultureInfo.CurrentCulture, "Query parameter : {paramName} = {paramValue}", paramName, paramValue);
				}

				CachedCommand.AddParameters(paramArray);
				CachedCommand.TryPrepare();
				PreviousQuery = query.Command;
			}
			else
			{
				foreach ((string paramName, object? paramValue) in query.Parameters)
				{
					CachedCommand.UpdateParameter(paramName, paramValue);
					Logger.Debug(CultureInfo.CurrentCulture, "Cached query with parameter : {paramName} = {paramValue}", paramName, paramValue);
				}
			}

			using (DbDataReader reader = CachedCommand.ExecuteReader())
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

		private static Query CreateQuery(this CommonDatabaseConnection connection, FindWordInfo info, int endWordFlag, int attackWordFlag)
		{
			var paramList = new List<(string, object?)>();

			string condition;
			ResponsePresentedWord data = info.Word;
			string indexColumnName = GetIndexColumnName(info);
			paramList.Add(("@primaryWord", data.Content));
			if (data.CanSubstitution)
			{
				condition = $"WHERE ({indexColumnName} = @primaryWord OR {indexColumnName} = @secondaryWord)";
				paramList.Add(("@secondaryWord", data.Substitution!));
			}
			else
			{
				condition = $"WHERE ({indexColumnName} = @primaryWord)";
			}

			var opt = new PreferenceInfo
			{
				PathFinderFlags = info.PathFinderFlags,
				WordPreference = info.WordPreference,
				Condition = ""
			};

			// 한방 단어
			ApplyFilter(PathFinderOptions.UseEndWord, endWordFlag, ref opt);

			// 공격 단어
			ApplyFilter(PathFinderOptions.UseAttackWord, attackWordFlag, ref opt);

			// 쿵쿵따 모드에서는 쿵쿵따 전용 단어들만 추천
			if (info.Mode == GameMode.KungKungTta)
				condition += $" AND ({DatabaseConstants.FlagsColumnName} & {(int)WordDatabaseAttributes.KKT3} != 0)";

			string orderCondition = $"({CreateRearrangeCondition(connection, info.MissionChar, info.WordPreference, endWordFlag, attackWordFlag, paramList)} + LENGTH({DatabaseConstants.WordColumnName}))";

			if (info.Mode == GameMode.All)
				condition = opt.Condition = "";

			return new Query($"SELECT * FROM {DatabaseConstants.WordListTableName} {condition}{opt.Condition} ORDER BY {orderCondition} DESC LIMIT {DatabaseConstants.QueryResultLimit}", paramList.ToArray());
		}

		private static string CreateRearrangeCondition(this CommonDatabaseConnection connection, string missionChar, WordPreference wordPreference, int endWordFlag, int attackWordFlag, ICollection<(string, object?)> paramList)
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
				paramList.Add(("@missionChar", missionChar));
				return string.Format(
					CultureInfo.InvariantCulture,
					"{0}({1}, {2}, @missionChar, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10})",
					connection.GetRearrangeMissionFuncName(),
					DatabaseConstants.WordColumnName,
					DatabaseConstants.FlagsColumnName,
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

		private static void ApplyFilter(PathFinderOptions targetFlag, int flag, ref PreferenceInfo info)
		{
			if (!info.PathFinderFlags.HasFlag(targetFlag))
				info.Condition += $" AND ({DatabaseConstants.FlagsColumnName} & {flag} = 0)";
		}

		private sealed record Query(string Command, params (string, object?)[] Parameters);

		private struct PreferenceInfo
		{
			public PathFinderOptions PathFinderFlags;

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
