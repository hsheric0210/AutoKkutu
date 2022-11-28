using AutoKkutu.Constants;
using Microsoft.EntityFrameworkCore;
using System;

namespace AutoKkutu.Databases.Extension
{
	public static class WordIndexTableExtension
	{
		public static DbSet<WordIndexModel> GetWordIndexTable(this PathDbContext context, NodeTypes nodeType)
		{
			if (context is null)
				throw new ArgumentNullException(nameof(context));

			return nodeType switch
			{
				NodeTypes.EndWord or NodeTypes.KKTEndWord => context.EndWordIndex,
				NodeTypes.AttackWord or NodeTypes.KKTAttackWord => context.AttackWordIndex,
				NodeTypes.ReverseEndWord => context.ReverseEndWordIndex,
				NodeTypes.ReverseAttackWord => context.ReverseAttackWordIndex,
				NodeTypes.KkutuEndWord => context.KkutuEndWordIndex,
				NodeTypes.KkutuAttackWord => context.KkutuAttackWordIndex,
				_ => throw new NotImplementedException(),
			};
		}
	}
}
