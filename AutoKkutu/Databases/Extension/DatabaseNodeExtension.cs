using AutoKkutu.Constants;
using AutoKkutu.EF;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;

namespace AutoKkutu.Databases.Extension
{
	public static class DatabaseNodeExtension
	{
		public static DbSet<WordIndex> GetWordIndexTable(this PathDbContext ctx, NodeDatabaseAttributes nodeType)
		{
			if (ctx is null)
				throw new ArgumentNullException(nameof(ctx));

			return nodeType switch
			{
				NodeDatabaseAttributes.EndWord or NodeDatabaseAttributes.KKTEndWord => ctx.EndWordIndex,
				NodeDatabaseAttributes.AttackWord or NodeDatabaseAttributes.KKTAttackWord => ctx.AttackWordIndex,
				NodeDatabaseAttributes.ReverseEndWord => ctx.ReverseEndWordIndex,
				NodeDatabaseAttributes.ReverseAttackWord => ctx.ReverseAttackWordIndex,
				NodeDatabaseAttributes.KkutuEndWord => ctx.KkutuEndWordIndex,
				NodeDatabaseAttributes.KkutuAttackWord => ctx.KkutuAttackWordIndex,
				_ => throw new ArgumentException("node type must be specified.", nameof(nodeType)),
			};
		}

		public static bool AddNode(this DbSet<WordIndex> indexContext, string node)
		{
			if (indexContext == null)
				throw new ArgumentNullException(nameof(indexContext));
			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException(nameof(node));

			if (indexContext.Any(c => string.Equals(c.Index, node, StringComparison.OrdinalIgnoreCase)))
				return false; // Already exists

			indexContext.Add(new WordIndex() { Index = node });
			return true;
		}

		public static int DeleteNode(this CommonDatabaseConnection connection, string node, string? tableName = null)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException(nameof(node));

			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndWordListTableName;

			return connection.ExecuteNonQuery($"DELETE FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = @index", connection.CreateParameter("@index", node));
		}

		public static ICollection<string> GetNodeList(this CommonDatabaseConnection connection, string tableName)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			var result = new List<string>();

			using CommonDatabaseCommand _command = connection.CreateCommand($"SELECT * FROM {tableName}");
			using DbDataReader reader = _command.ExecuteReader();
			int wordIndexOrdinal = reader.GetOrdinal(DatabaseConstants.WordIndexColumnName);
			while (reader.Read())
				result.Add(reader.GetString(wordIndexOrdinal));

			Log.Information("Found Total {0} nodes in {1}.", result.Count, tableName);
			return result;
		}
	}
}
