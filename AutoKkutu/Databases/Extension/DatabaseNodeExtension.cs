﻿using AutoKkutu.Constants;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoKkutu.Databases.Extension
{
	public static class DatabaseNodeExtension
	{
		public static DbSet<WordIndexModel> GetWordIndexTable(this PathDbContext table, NodeDatabaseAttributes nodeType)
		{
			if (table is null)
				throw new ArgumentNullException(nameof(table));

			return nodeType switch
			{
				NodeDatabaseAttributes.EndWord or NodeDatabaseAttributes.KKTEndWord => table.EndWordIndex,
				NodeDatabaseAttributes.AttackWord or NodeDatabaseAttributes.KKTAttackWord => table.AttackWordIndex,
				NodeDatabaseAttributes.ReverseEndWord => table.ReverseEndWordIndex,
				NodeDatabaseAttributes.ReverseAttackWord => table.ReverseAttackWordIndex,
				NodeDatabaseAttributes.KkutuEndWord => table.KkutuEndWordIndex,
				NodeDatabaseAttributes.KkutuAttackWord => table.KkutuAttackWordIndex,
				_ => throw new ArgumentException("node type must be specified.", nameof(nodeType)),
			};
		}

		public static bool AddNode(this DbSet<WordIndexModel> table, string node)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));
			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException(nameof(node));

			if (table.Any(c => string.Equals(c.Index, node, StringComparison.OrdinalIgnoreCase)))
				return false; // Already exists

			table.Add(new WordIndexModel() { Index = node });
			return true;
		}

		public static int DeleteNode(this DbSet<WordIndexModel> table, string node)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));
			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException(nameof(node));

			var entities = table.Where(wordIndex => string.Equals(wordIndex.Index, node, StringComparison.OrdinalIgnoreCase)).ToList(); // Disabling lazy-init because we need to get the count
			table.RemoveRange(entities);
			return entities.Count;
		}

		public static ICollection<string> GetNodeList(this DbSet<WordIndexModel> table)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));

			var result = table.Select(wordIndex => wordIndex.Index).ToList();
			Log.Information("Found {count} nodes so far.", result.Count);
			return result;
		}
	}
}
