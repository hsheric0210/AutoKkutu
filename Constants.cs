using System;
using System.Windows.Media;

namespace AutoKkutu
{
	public static class Constants
	{
		public static class ColorDefinitions
		{
			public static Color NormalColor = Color.FromRgb(64, 80, 141);

			public static Color WarningColor = Color.FromRgb(243, 108, 26);

			public static Color ErrorColor = Color.FromRgb(137, 45, 45);

			public static Color WaitColor = Color.FromRgb(121, 121, 121);
		}

		[Flags]
		public enum WordFlags
		{
			None = 0,
			EndWord = 1 << 0, // 한방 단어
			AttackWord = 1 << 1, // 공격 단어
			ReverseEndWord = 1 << 2, // 앞말잇기 한방 단어
			ReverseAttackWord = 1 << 3, // 앞말잇기 공격 단어
			MiddleEndWord = 1 << 4, // 가운뎃말잇기 한방 단어
			MiddleAttackWord = 1 << 5, // 가운뎃말잇기 공격 단어
			KkutuEndWord = 1 << 6, // 끄투 한방 단어
			KkutuAttackWord = 1 << 7, // 끄투 공격 단어
		}

		[Flags]
		public enum NodeFlags
		{
			EndWord = 1 << 0, // 한방 단어
			AttackWord = 1 << 1, // 공격 단어
			ReverseEndWord = 1 << 2, // 앞말잇기 한방 단어
			ReverseAttackWord = 1 << 3, // 앞말잇기 공격 단어
			KkutuEndWord = 1 << 4, // 끄투 한방 단어
			KkutuAttackWord = 1 << 5, // 끄투 공격 단어
		}
	}
}
