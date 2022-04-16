using System;
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
			PathFinder.CheckNodePresence(null, word.Last().ToString(), PathFinder.EndWordList, WordFlags.EndWord, ref flags);
			PathFinder.CheckNodePresence(null, word.Last().ToString(), PathFinder.AttackWordList, WordFlags.AttackWord, ref flags);
			PathFinder.CheckNodePresence(null, word.First().ToString(), PathFinder.ReverseEndWordList, WordFlags.ReverseEndWord, ref flags);
			PathFinder.CheckNodePresence(null, word.First().ToString(), PathFinder.ReverseAttackWordList, WordFlags.ReverseAttackWord, ref flags);
			if (word.Length > 2)
			{
				PathFinder.CheckNodePresence(null, word.Substring(word.Length - 3, 2), PathFinder.KkutuEndWordList, WordFlags.KkutuEndWord, ref flags);
				PathFinder.CheckNodePresence(null, word.Substring(word.Length - 3, 2), PathFinder.KkutuAttackWordList, WordFlags.KkutuAttackWord, ref flags);
				if (word.Length % 2 == 1)
				{
					PathFinder.CheckNodePresence(null, word[(word.Length - 1) / 2].ToString(), PathFinder.EndWordList, WordFlags.MiddleEndWord, ref flags);
					PathFinder.CheckNodePresence(null, word[(word.Length - 1) / 2].ToString(), PathFinder.AttackWordList, WordFlags.MiddleAttackWord, ref flags);
				}
			}
			return flags;
		}

		public static void CorrectFlags(string word, ref WordFlags flags, ref int NewEndNode, ref int NewAttackNode)
		{
			if (PathFinder.CheckNodePresence("end", word.Last().ToString(), PathFinder.EndWordList, WordFlags.EndWord, ref flags, true))
				NewEndNode++;
			if (PathFinder.CheckNodePresence("attack", word.Last().ToString(), PathFinder.AttackWordList, WordFlags.AttackWord, ref flags, true))
				NewAttackNode++;
			if (PathFinder.CheckNodePresence("reverse end", word.First().ToString(), PathFinder.ReverseEndWordList, WordFlags.ReverseEndWord, ref flags, true))
				NewEndNode++;
			if (PathFinder.CheckNodePresence("reverse attack", word.First().ToString(), PathFinder.ReverseAttackWordList, WordFlags.ReverseAttackWord, ref flags, true))
				NewAttackNode++;
			if (word.Length > 2)
			{
				if (PathFinder.CheckNodePresence("kkutu end", word.Substring(word.Length - 3, 2), PathFinder.KkutuEndWordList, WordFlags.KkutuEndWord, ref flags, true))
					NewEndNode++;
				if (PathFinder.CheckNodePresence("kkutu attack", word.Substring(word.Length - 3, 2), PathFinder.KkutuAttackWordList, WordFlags.KkutuAttackWord, ref flags, true))
					NewAttackNode++;
				if (word.Length % 2 == 1)
				{
					if (PathFinder.CheckNodePresence("middle end", word[(word.Length - 1) / 2].ToString(), PathFinder.EndWordList, WordFlags.MiddleEndWord, ref flags, true))
						NewEndNode++;
					if (PathFinder.CheckNodePresence("middle attack", word[(word.Length - 1) / 2].ToString(), PathFinder.AttackWordList, WordFlags.MiddleAttackWord, ref flags, true))
						NewAttackNode++;
				}
			}
		}
	}
}
