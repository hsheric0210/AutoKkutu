using Dapper;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;

namespace AutoKkutu.Database.Extension
{
	public static class NodeExtension
	{

		public static bool AddNode(this AbstractDatabaseConnection connection, string node, string? tableName = null)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException(nameof(node));

			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndNodeIndexTableName;

			string nodeString;
			if (tableName.Equals(DatabaseConstants.KkutuWordIndexColumnName, StringComparison.Ordinal))
				nodeString = node[..2];
			else
				nodeString = node[0].ToString();

			if (connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = @node;", new
			{
				Node = nodeString
			}) > 0)
			{
				return false;
			}

			connection.Execute($"INSERT INTO {tableName}({DatabaseConstants.WordIndexColumnName}) VALUES(@Node)", new
			{
				Node = nodeString
			});
			return true;
		}

		public static int DeleteNode(this AbstractDatabaseConnection connection, string node, string? tableName = null)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException(nameof(node));

			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndNodeIndexTableName;

			return connection.Execute($"DELETE FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = @Node", new
			{
				Node = node
			});
		}

		public static ICollection<string> GetNodeList(this AbstractDatabaseConnection connection, string tableName)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			List<string> result = connection.Query<string>($"SELECT ({DatabaseConstants.WordIndexColumnName}) FROM {tableName}").AsList();
			Log.Information("Found Total {0} nodes in {1}.", result.Count, tableName);
			return result;
		}
	}
}
