using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;

namespace AutoKkutu.Databases.Extension
{
	public static class NodeExtension
	{

		public static bool AddNode(this CommonDatabaseConnection connection, string node, string? tableName = null)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException(nameof(node));

			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndWordListTableName;

			if (Convert.ToInt32(connection.ExecuteScalar($"SELECT COUNT(*) FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = @node;", connection.CreateParameter("@node", node[0])), CultureInfo.InvariantCulture) > 0)
				return false;

			CommonDatabaseParameter parameter;
			if (tableName.Equals(DatabaseConstants.KkutuWordIndexColumnName, StringComparison.Ordinal))
				parameter = connection.CreateParameter(CommonDatabaseType.CharacterVarying, 2, "@index", node[..2]);
			else
				parameter = connection.CreateParameter(CommonDatabaseType.Character, 1, "@index", node[0]);

			connection.ExecuteNonQuery($"INSERT INTO {tableName}({DatabaseConstants.WordIndexColumnName}) VALUES(@index)", parameter);
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
