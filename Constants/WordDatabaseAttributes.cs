using System;

namespace AutoKkutu.Constants
{
	[Flags]
	public enum WordDatabaseAttributes
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
}
