using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static AutoKkutu.Constants;

namespace AutoKkutu
{
	public static class Utils
	{

		public static Random GLOBAL_RANDOM = new Random();

		public static string GenerateRandomString(int length, bool english, Random random = null)
		{
			if (random == null)
				random = GLOBAL_RANDOM;
			var builder = new StringBuilder(length);
			char start = '가';
			char end = '힣';
			if (english)
			{
				start = 'a';
				end = 'z';
			}

			for (int i = 0; i < length; i++)
				if (random.NextDouble() > 0.7)
					builder.Append(random.Next(10));
				else
					builder.Append((char)random.Next(start, end + 1));
			return builder.ToString();
		}

		public static WordFlags GetFlags(string word)
		{
			WordFlags flags = WordFlags.None;
			PathFinder.CheckNodePresence(null, GetLaFTailNode(word), PathFinder.EndWordList, WordFlags.EndWord, ref flags);
			PathFinder.CheckNodePresence(null, GetLaFTailNode(word), PathFinder.AttackWordList, WordFlags.AttackWord, ref flags);
			PathFinder.CheckNodePresence(null, GetFaLTailNode(word), PathFinder.ReverseEndWordList, WordFlags.ReverseEndWord, ref flags);
			PathFinder.CheckNodePresence(null, GetFaLTailNode(word), PathFinder.ReverseAttackWordList, WordFlags.ReverseAttackWord, ref flags);
			if (word.Length > 2)
			{
				PathFinder.CheckNodePresence(null, GetKkutuTailNode(word), PathFinder.KkutuEndWordList, WordFlags.KkutuEndWord, ref flags);
				PathFinder.CheckNodePresence(null, GetKkutuTailNode(word), PathFinder.KkutuAttackWordList, WordFlags.KkutuAttackWord, ref flags);
				if (word.Length % 2 == 1)
				{
					PathFinder.CheckNodePresence(null, GetMaFNode(word), PathFinder.EndWordList, WordFlags.MiddleEndWord, ref flags);
					PathFinder.CheckNodePresence(null, GetMaFNode(word), PathFinder.AttackWordList, WordFlags.MiddleAttackWord, ref flags);
				}
			}
			return flags;
		}

		public static void CorrectFlags(string word, ref WordFlags flags, ref int NewEndNode, ref int NewAttackNode)
		{
			if (PathFinder.CheckNodePresence("end", GetLaFTailNode(word), PathFinder.EndWordList, WordFlags.EndWord, ref flags, true))
				NewEndNode++;
			if (PathFinder.CheckNodePresence("attack", GetLaFTailNode(word), PathFinder.AttackWordList, WordFlags.AttackWord, ref flags, true))
				NewAttackNode++;
			if (PathFinder.CheckNodePresence("reverse end", GetFaLTailNode(word), PathFinder.ReverseEndWordList, WordFlags.ReverseEndWord, ref flags, true))
				NewEndNode++;
			if (PathFinder.CheckNodePresence("reverse attack", GetFaLTailNode(word), PathFinder.ReverseAttackWordList, WordFlags.ReverseAttackWord, ref flags, true))
				NewAttackNode++;
			if (word.Length > 2)
			{
				if (PathFinder.CheckNodePresence("kkutu end", GetKkutuTailNode(word), PathFinder.KkutuEndWordList, WordFlags.KkutuEndWord, ref flags, true))
					NewEndNode++;
				if (PathFinder.CheckNodePresence("kkutu attack", GetKkutuTailNode(word), PathFinder.KkutuAttackWordList, WordFlags.KkutuAttackWord, ref flags, true))
					NewAttackNode++;
				if (word.Length % 2 == 1)
				{
					if (PathFinder.CheckNodePresence("middle end", GetMaFNode(word), PathFinder.EndWordList, WordFlags.MiddleEndWord, ref flags, true))
						NewEndNode++;
					if (PathFinder.CheckNodePresence("middle attack", GetMaFNode(word), PathFinder.AttackWordList, WordFlags.MiddleAttackWord, ref flags, true))
						NewAttackNode++;
				}
			}
		}

		public static string GetLaFHeadNode(string word) => word.First().ToString();
		public static string GetFaLHeadNode(string word) => word.Last().ToString();
		public static string GetKkutuHeadNode(string word) => word.Length >= 4 ? word.Substring(0, 2) : word.First().ToString();

		public static string GetLaFTailNode(string word) => word.Last().ToString();
		public static string GetFaLTailNode(string word) => word.First().ToString();
		public static string GetKkutuTailNode(string word) => word.Substring(word.Length - 3, 2);
		public static string GetMaFNode(string word) => word[(word.Length - 1) / 2].ToString();
	}
}
