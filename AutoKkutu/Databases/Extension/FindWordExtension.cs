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

		private static WordType GetPathObjectFlags(GetPathObjectFlagsInfo info, out int missionCharCount)
		{
			WordDbTypes WordDatabaseAttributes = info.WordDatabaseAttributes;
			WordType wordAttributes = WordType.None;
			if (WordDatabaseAttributes.HasFlag(info.EndWordFlag))
				wordAttributes |= WordType.EndWord;
			if (WordDatabaseAttributes.HasFlag(info.AttackWordFlag))
				wordAttributes |= WordType.AttackWord;

			string missionChar = info.MissionChar;
			if (!string.IsNullOrWhiteSpace(missionChar))
			{
				missionCharCount = info.Word.Count(c => c == missionChar[0]);
				if (missionCharCount > 0)
					wordAttributes |= WordType.MissionWord;
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
				WordDatabaseAttributes = (WordDbTypes)word.Flags,
				MissionChar = missionChar,
				EndWordFlag = (WordDbTypes)endWordFlag,
				AttackWordFlag = (WordDbTypes)attackWordFlag
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
				if (info.Mode == GameMode.KungKungTta && (word.Flags & (int)WordDbTypes.KKT3) == 0)
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
								 GetAttributeOrdinal(wordPreference, WordType.EndWord),
								 GetAttributeOrdinal(wordPreference, WordType.AttackWord),
								 GetAttributeOrdinal(wordPreference, WordType.None));
				}
				else
				{
					priority = ctx.MissionWordPriority(word.Word,
						word.Flags,
						missionWord,
						endWordFlag,
						attackWordFlag,
						GetAttributeOrdinal(wordPreference, WordType.EndWord | WordType.MissionWord),
						GetAttributeOrdinal(wordPreference, WordType.EndWord),
						GetAttributeOrdinal(wordPreference, WordType.AttackWord | WordType.MissionWord),
						GetAttributeOrdinal(wordPreference, WordType.AttackWord),
						GetAttributeOrdinal(wordPreference, WordType.MissionWord),
						GetAttributeOrdinal(wordPreference, WordType.None));
				}

				return word.Word.Length + priority;
			};
		}

		private static int GetAttributeOrdinal(WordPreference preference, WordType attributes)
		{
			WordType[] fullAttribs = preference.GetAttributes();
			int index = Array.IndexOf(fullAttribs, attributes);
			return fullAttribs.Length - (index >= 0 ? index : fullAttribs.Length) - 1;
		}

		private struct GetPathObjectFlagsInfo
		{
			public string Word;

			public string MissionChar;

			public WordDbTypes WordDatabaseAttributes;

			public WordDbTypes EndWordFlag;

			public WordDbTypes AttackWordFlag;
		}
	}
}
