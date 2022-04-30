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

			/// <summary>
			/// 한방 단어
			/// </summary>
			EndWord = 1 << 0,

			/// <summary>
			/// 공격 단어
			/// </summary>
			AttackWord = 1 << 1,

			/// <summary>
			/// 앞말잇기 한방 단어
			/// </summary>
			ReverseEndWord = 1 << 2,

			/// <summary>
			/// 앞말잇기 공격 단어
			/// </summary>
			ReverseAttackWord = 1 << 3,

			/// <summary>
			/// 가운뎃말잇기 한방 단어
			/// </summary>
			MiddleEndWord = 1 << 4,

			/// <summary>
			/// 가운뎃말잇기 공격 단어
			/// </summary>
			MiddleAttackWord = 1 << 5,

			/// <summary>
			/// 끄투 한방 단어
			/// </summary>
			KkutuEndWord = 1 << 6,

			/// <summary>
			/// 끄투 공격 단어
			/// </summary>
			KkutuAttackWord = 1 << 7,
		}

		[Flags]
		public enum NodeFlags
		{
			/// <summary>
			/// 한방 단어
			/// </summary>
			EndWord = 1 << 0,

			/// <summary>
			/// 공격 단어
			/// </summary>
			AttackWord = 1 << 1,

			/// <summary>
			/// 앞말잇기 한방 단어
			/// </summary>
			ReverseEndWord = 1 << 2,

			/// <summary>
			/// 앞말잇기 공격 단어
			/// </summary>
			ReverseAttackWord = 1 << 3,

			/// <summary>
			/// 끄투 한방 단어
			/// </summary>
			KkutuEndWord = 1 << 4,

			/// <summary>
			/// 끄투 공격 단어
			/// </summary>
			KkutuAttackWord = 1 << 5,
		}
	}
}
