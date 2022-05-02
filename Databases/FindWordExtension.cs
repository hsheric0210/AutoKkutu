using System;
using System.Collections.Generic;
using System.Linq;
using static AutoKkutu.Constants;

namespace AutoKkutu.Databases
{
	public static class FindWordExtension
	{
		private static string GetIndexColumnName(FindWordInfo opts)
		{
			switch (opts.Mode)
			{
				case GameMode.First_and_Last:
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

		public static void GetOptimalWordFlags(GameMode mode, out int endWordFlag, out int attackWordFlag)
		{
			switch (mode)
			{
				case GameMode.First_and_Last:
					endWordFlag = (int)WordFlags.ReverseEndWord;
					attackWordFlag = (int)WordFlags.ReverseAttackWord;
					return;

				case GameMode.Middle_and_First:
					endWordFlag = (int)WordFlags.MiddleEndWord;
					attackWordFlag = (int)WordFlags.MiddleAttackWord;
					return;

				case GameMode.Kkutu:
					endWordFlag = (int)WordFlags.KkutuEndWord;
					attackWordFlag = (int)WordFlags.KkutuAttackWord;
					return;
			}
			endWordFlag = (int)WordFlags.EndWord;
			attackWordFlag = (int)WordFlags.AttackWord;
		}

		private static PathObjectOptions GetPathObjectFlags(GetPathObjectFlagsInfo info, out int missionCharCount)
		{
			WordFlags wordFlags = info.WordFlags;
			PathObjectOptions pathFlags = PathObjectOptions.None;
			if (wordFlags.HasFlag(info.EndWordFlag))
				pathFlags |= PathObjectOptions.EndWord;
			if (wordFlags.HasFlag(info.AttackWordFlag))
				pathFlags |= PathObjectOptions.AttackWord;

			var missionChar = info.MissionChar;
			if (!string.IsNullOrWhiteSpace(missionChar))
			{
				missionCharCount = info.Word.Count(c => c == missionChar.First());
				if (missionCharCount > 0)
					pathFlags |= PathObjectOptions.MissionWord;
			}
			else
				missionCharCount = 0;
			return pathFlags;
		}

		public static ICollection<PathObject> FindWord(this CommonDatabaseConnection connection, FindWordInfo info)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			var result = new List<PathObject>();
			GetOptimalWordFlags(info.Mode, out int endWordFlag, out int attackWordFlag);
			string query = connection.CreateQuery(info, endWordFlag, attackWordFlag);

			//Logger.InfoFormat("Query: {0}", query);
			using (CommonDatabaseReader reader = connection.ExecuteReader(query))
				while (reader.Read())
				{
					string word = reader.GetString(DatabaseConstants.WordColumnName).ToString().Trim();
					result.Add(new PathObject(word, GetPathObjectFlags(new GetPathObjectFlagsInfo
					{
						Word = word,
						WordFlags = (WordFlags)reader.GetInt32(DatabaseConstants.FlagsColumnName),
						MissionChar = info.MissionChar,
						EndWordFlag = (WordFlags)endWordFlag,
						AttackWordFlag = (WordFlags)attackWordFlag
					}, out int missionCharCount), missionCharCount));
				}
			return result;
		}

		private static string CreateQuery(this CommonDatabaseConnection connection, FindWordInfo info, int endWordFlag, int attackWordFlag)
		{
			string condition;
			var data = info.Word;
			string indexColumnName = GetIndexColumnName(info);
			if (data.CanSubstitution)
				condition = $"WHERE ({indexColumnName} = '{data.Content}' OR {indexColumnName} = '{data.Substitution}')";
			else
				condition = $"WHERE {indexColumnName} = '{data.Content}'";

			var opt = new PreferenceInfo { PathFinderFlags = info.PathFinderFlags, WordPreference = info.WordPreference, Condition = "", OrderCondition = "" };

			// 한방 단어
			ApplyPreference(PathFinderInfo.UseEndWord, endWordFlag, DatabaseConstants.EndWordIndexPriority, ref opt);

			// 공격 단어
			ApplyPreference(PathFinderInfo.UseAttackWord, attackWordFlag, DatabaseConstants.AttackWordIndexPriority, ref opt);

			// 미션 단어
			string orderCondition;
			if (string.IsNullOrWhiteSpace(info.MissionChar))
				orderCondition = $"({opt.OrderCondition} LENGTH({DatabaseConstants.WordColumnName}))";
			else
				orderCondition = $"({connection.GetCheckMissionCharFuncName()}({DatabaseConstants.WordColumnName}, '{info.MissionChar}') + {opt.OrderCondition} LENGTH({DatabaseConstants.WordColumnName}))";

			if (info.Mode == GameMode.All)
				condition = opt.Condition = "";

			return $"SELECT * FROM {DatabaseConstants.WordListTableName} {condition} {opt.Condition} ORDER BY {orderCondition} DESC LIMIT {DatabaseConstants.QueryResultLimit}";
		}

		private static void ApplyPreference(PathFinderInfo targetFlag, int flag, int targetPriority, ref PreferenceInfo opt)
		{
			if (!opt.PathFinderFlags.HasFlag(targetFlag))
				opt.Condition += $"AND (flags & {flag} = 0)";
			else if (opt.WordPreference == WordPreference.ATTACK_DAMAGE)
				opt.OrderCondition += $"(CASE WHEN (flags & {flag} != 0) THEN {targetPriority} ELSE 0 END) +";
		}

		private struct PreferenceInfo
		{
			public PathFinderInfo PathFinderFlags;

			public WordPreference WordPreference;

			public string Condition;

			public string OrderCondition;
		}

		private struct GetPathObjectFlagsInfo
		{
			public string Word;

			public string MissionChar;

			public WordFlags WordFlags;

			public WordFlags EndWordFlag;

			public WordFlags AttackWordFlag;
		}
	}
}
