using System;
using System.Linq;
using System.Text;
using static AutoKkutu.Constants;

namespace AutoKkutu
{
	public static class DatabaseUtils
	{
		public static WordFlags GetFlags(string word)
		{
			WordFlags flags = WordFlags.None;
			PathFinder.CheckNodePresence(null, word.GetLaFTailNode(), PathFinder.EndWordList, WordFlags.EndWord, ref flags);
			PathFinder.CheckNodePresence(null, word.GetLaFTailNode(), PathFinder.AttackWordList, WordFlags.AttackWord, ref flags);
			PathFinder.CheckNodePresence(null, word.GetFaLTailNode(), PathFinder.ReverseEndWordList, WordFlags.ReverseEndWord, ref flags);
			PathFinder.CheckNodePresence(null, word.GetFaLTailNode(), PathFinder.ReverseAttackWordList, WordFlags.ReverseAttackWord, ref flags);
			if (word.Length > 2)
			{
				PathFinder.CheckNodePresence(null, word.GetKkutuTailNode(), PathFinder.KkutuEndWordList, WordFlags.KkutuEndWord, ref flags);
				PathFinder.CheckNodePresence(null, word.GetKkutuTailNode(), PathFinder.KkutuAttackWordList, WordFlags.KkutuAttackWord, ref flags);
				if (word.Length % 2 == 1)
				{
					PathFinder.CheckNodePresence(null, word.GetMaFNode(), PathFinder.EndWordList, WordFlags.MiddleEndWord, ref flags);
					PathFinder.CheckNodePresence(null, word.GetMaFNode(), PathFinder.AttackWordList, WordFlags.MiddleAttackWord, ref flags);
				}
			}
			return flags;
		}

		public static void CorrectFlags(string word, ref WordFlags flags, ref int NewEndNode, ref int NewAttackNode)
		{
			if (PathFinder.CheckNodePresence("end", word.GetLaFTailNode(), PathFinder.EndWordList, WordFlags.EndWord, ref flags, true))
				NewEndNode++;
			if (PathFinder.CheckNodePresence("attack", word.GetLaFTailNode(), PathFinder.AttackWordList, WordFlags.AttackWord, ref flags, true))
				NewAttackNode++;
			if (PathFinder.CheckNodePresence("reverse end", word.GetFaLTailNode(), PathFinder.ReverseEndWordList, WordFlags.ReverseEndWord, ref flags, true))
				NewEndNode++;
			if (PathFinder.CheckNodePresence("reverse attack", word.GetFaLTailNode(), PathFinder.ReverseAttackWordList, WordFlags.ReverseAttackWord, ref flags, true))
				NewAttackNode++;
			if (word.Length > 2)
			{
				if (PathFinder.CheckNodePresence("kkutu end", word.GetKkutuTailNode(), PathFinder.KkutuEndWordList, WordFlags.KkutuEndWord, ref flags, true))
					NewEndNode++;
				if (PathFinder.CheckNodePresence("kkutu attack", word.GetKkutuTailNode(), PathFinder.KkutuAttackWordList, WordFlags.KkutuAttackWord, ref flags, true))
					NewAttackNode++;
				if (word.Length % 2 == 1)
				{
					if (PathFinder.CheckNodePresence("middle end", word.GetMaFNode(), PathFinder.EndWordList, WordFlags.MiddleEndWord, ref flags, true))
						NewEndNode++;
					if (PathFinder.CheckNodePresence("middle attack", word.GetMaFNode(), PathFinder.AttackWordList, WordFlags.MiddleAttackWord, ref flags, true))
						NewAttackNode++;
				}
			}
		}

		public static string GetLaFHeadNode(this string word) => word.First().ToString();
		public static string GetFaLHeadNode(this string word) => word.Last().ToString();
		public static string GetKkutuHeadNode(this string word) => word.Length >= 4 ? word.Substring(0, 2) : (word.Length >= 3 ? word.First().ToString() : "");

		public static string GetLaFTailNode(this string word) => word.Last().ToString();
		public static string GetFaLTailNode(this string word) => word.First().ToString();
		public static string GetKkutuTailNode(this string word) => word.Length >= 4 ? word.Substring(word.Length - 3, 2) : word.Last().ToString();
		public static string GetMaFNode(this string word) => word[(word.Length - 1) / 2].ToString();
	}
}
