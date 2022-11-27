using AutoKkutu.Constants;
using AutoKkutu.Modules;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;

namespace AutoKkutu.Databases.Extension
{
	public static class FindWordExtension
	{
		private static string GetIndex(this WordModel word, FindWordInfo opts)
		{
			if (word is null)
				throw new ArgumentNullException(nameof(word));

			switch (opts.Mode)
			{
				case GameMode.FirstAndLast:
					return word.ReverseWordIndex;

				case GameMode.Kkutu:

					// TODO: 세 글자용 인덱스도 만들기
					ResponsePresentedWord demand = opts.Word;
					if (demand.Content.Length == 2 || demand.CanSubstitution && demand.Substitution!.Length == 2)
						return word.KkutuWorldIndex;
					break;
			}

			return word.WordIndex;
		}

		public static void GetOptimalWordDatabaseAttributes(GameMode mode, out int endWordFlag, out int attackWordFlag)
		{
			switch (mode)
			{
				case GameMode.FirstAndLast:
					endWordFlag = (int)WordDatabaseAttributes.ReverseEndWord;
					attackWordFlag = (int)WordDatabaseAttributes.ReverseAttackWord;
					return;

				case GameMode.MiddleAndFirst:
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

		public static IList<PathObject> FindWord(this PathDbContext ctx, FindWordInfo info)
		{
			if (ctx == null)
				throw new ArgumentNullException(nameof(ctx));

			GetOptimalWordDatabaseAttributes(info.Mode, out int endWordFlag, out int attackWordFlag);

			var filter = ctx.Word.CreateWordFilter(info, endWordFlag, attackWordFlag);
			var sorter = ctx.CreateSorter(info, info.MissionChar, endWordFlag, attackWordFlag);
			return ctx.Word.Where(w => filter(w)).OrderByDescending(w => sorter(w)).Take(DatabaseConstants.QueryResultLimit).Select(w => w.ToPathObject(info.MissionChar, endWordFlag, attackWordFlag)).ToList();
		}

		private static PathObject ToPathObject(this WordModel word, string missionChar, int endWordFlag, int attackWordFlag)
		{
			if (word is null)
				throw new ArgumentNullException(nameof(word));

			return new PathObject(word.Word, GetPathObjectFlags(new GetPathObjectFlagsInfo
			{
				Word = word.Word,
				WordDatabaseAttributes = (WordDatabaseAttributes)word.Flags,
				MissionChar = missionChar,
				EndWordFlag = (WordDatabaseAttributes)endWordFlag,
				AttackWordFlag = (WordDatabaseAttributes)attackWordFlag
			}, out int missionCharCount), missionCharCount);
		}

		private static Func<WordModel, bool> CreateWordFilter(this DbSet<WordModel> table, FindWordInfo info, int endWordFlag, int attackWordFlag)
		{
			ResponsePresentedWord data = info.Word;
			PathFinderOptions findFlags = info.PathFinderFlags;
			return (WordModel word) =>
			{
				// All mode
				if (info.Mode == GameMode.All)
					return true;

				// Check index
				string index = word.GetIndex(info);
				if (!string.Equals(index, data.Content, StringComparison.OrdinalIgnoreCase) && (!data.CanSubstitution || !string.Equals(index, data.Substitution, StringComparison.OrdinalIgnoreCase)))
					return false;

				// Disable end-word
				if (!findFlags.HasFlag(PathFinderOptions.UseEndWord) && (word.Flags & endWordFlag) != 0)
					return false;

				// Disable attack-word
				if (!findFlags.HasFlag(PathFinderOptions.UseAttackWord) && (word.Flags & attackWordFlag) != 0)
					return false;

				// KungKungTta
				if (info.Mode == GameMode.KungKungTta && (word.Flags & (int)WordDatabaseAttributes.KKT3) == 0)
					return false;

				return true;
			};
		}

		private static Func<WordModel, int> CreateSorter(this PathDbContext ctx, FindWordInfo info, string missionWord, int endWordFlag, int attackWordFlag)
		{
			WordPreference wordPreference = info.WordPreference;
			return (WordModel word) =>
			{

				int priority;
				if (string.IsNullOrWhiteSpace(missionWord))
				{
					priority = ctx.WordPriority(word.Flags,
								 endWordFlag,
								 attackWordFlag,
								 GetAttributeOrdinal(wordPreference, WordAttributes.EndWord),
								 GetAttributeOrdinal(wordPreference, WordAttributes.AttackWord),
								 GetAttributeOrdinal(wordPreference, WordAttributes.None));
				}
				else
				{
					priority = ctx.MissionWordPriority(word.Word,
						word.Flags,
						missionWord,
						endWordFlag,
						attackWordFlag,
						GetAttributeOrdinal(wordPreference, WordAttributes.EndWord | WordAttributes.MissionWord),
						GetAttributeOrdinal(wordPreference, WordAttributes.EndWord),
						GetAttributeOrdinal(wordPreference, WordAttributes.AttackWord | WordAttributes.MissionWord),
						GetAttributeOrdinal(wordPreference, WordAttributes.AttackWord),
						GetAttributeOrdinal(wordPreference, WordAttributes.MissionWord),
						GetAttributeOrdinal(wordPreference, WordAttributes.None));
				}

				return word.Word.Length + priority;
			};
		}

		private static int GetAttributeOrdinal(WordPreference preference, WordAttributes attributes)
		{
			WordAttributes[] fullAttribs = preference.GetAttributes();
			int index = Array.IndexOf(fullAttribs, attributes);
			return fullAttribs.Length - (index >= 0 ? index : fullAttribs.Length) - 1;
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
