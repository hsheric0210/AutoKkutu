using AutoKkutu.Constants;
using System;
using Microsoft.EntityFrameworkCore;
using Serilog;
using AutoKkutu.Utils;

namespace AutoKkutu.Databases.Extension
{
	public static class GameModeExtension
	{

		public static DbSet<IWordIndex> GetAttackWordIndexForMode(this PathDbContext context, GameMode mode)
		{
			if (context is null)
				throw new ArgumentNullException(nameof(context));

			return mode switch
			{
				GameMode.FirstAndLast => context.ReverseAttackWordIndex,
				GameMode.Kkutu => context.KkutuAttackWordIndex,
				_ => context.AttackWordIndex,
			};
		}

		public static DbSet<IWordIndex> GetEndWordIndexForMode(this PathDbContext context, GameMode mode)
		{
			if (context is null)
				throw new ArgumentNullException(nameof(context));

			return mode switch
			{
				GameMode.FirstAndLast => context.ReverseEndWordIndex,
				GameMode.Kkutu => context.KkutuEndWordIndex,
				_ => context.EndWordIndex,
			};
		}

		// ISOLATION

		public static void MakeAttack(this PathDbContext context, GameMode mode, string word)
		{
			string node = mode.WordToNode(word);
			context.GetEndWordIndexForMode(mode).DeleteNode(node);
			if (context.GetAttackWordIndexForMode(mode).AddNode(node))
				Log.Information(I18n.PathMark_Success, node, I18n.PathMark_Attack, mode);
			else
				Log.Warning(I18n.PathMark_AlreadyDone, node, I18n.PathMark_Attack, mode);
		}

		public static void MakeEnd(this PathDbContext context, GameMode mode, string word)
		{
			string node = mode.WordToNode(word);
			context.GetAttackWordIndexForMode(mode).DeleteNode(node);
			if (context.GetEndWordIndexForMode(mode).AddNode(node))
				Log.Information(I18n.PathMark_Success, node, I18n.PathMark_End, mode);
			else
				Log.Warning(I18n.PathMark_AlreadyDone, node, I18n.PathMark_End, mode);
		}

		public static void MakeNormal(this PathDbContext context, GameMode mode, string word)
		{
			string node = mode.WordToNode(word);
			bool endWord = context.GetEndWordIndexForMode(mode).DeleteNode(node) > 0;
			bool attackWord = context.GetAttackWordIndexForMode(mode).DeleteNode(node) > 0;
			if (endWord || attackWord)
				Log.Information(I18n.PathMark_Success, node, I18n.PathMark_Normal, mode);
			else
				Log.Warning(I18n.PathMark_AlreadyDone, node, I18n.PathMark_Normal, mode);
		}

		public static string WordToNode(this GameMode mode, string word)
		{
			switch (mode)
			{
				case GameMode.FirstAndLast:
					return word.GetFaLTailNode();

				case GameMode.MiddleAndFirst:
					if (word.Length % 2 == 1)
						return word.GetMaFNode();
					break;

				case GameMode.Kkutu:
					if (word.Length > 2)
						return word.GetKkutuTailNode();
					break;
			}
			return word.GetLaFTailNode();
		}
	}
}
