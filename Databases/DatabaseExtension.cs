using AutoKkutu.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using AutoKkutu.Constants;
using log4net;
using System.Data.Common;

namespace AutoKkutu.Databases
{
	public static class DatabaseExtension
	{
		private static readonly ILog Logger = LogManager.GetLogger("Database Exts");

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

		public static bool AddWord(this CommonDatabaseConnection connection, string word, WordDatabaseAttributes flags)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (string.IsNullOrWhiteSpace(word))
				throw new ArgumentNullException(nameof(word));

			if (Convert.ToInt32(connection.ExecuteScalar($"SELECT COUNT(*) FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = @word;", connection.CreateParameter("@word", word)), CultureInfo.InvariantCulture) > 0)
				return false;

			connection.ExecuteNonQuery(
				$"INSERT INTO {DatabaseConstants.WordListTableName}({DatabaseConstants.WordIndexColumnName}, {DatabaseConstants.ReverseWordIndexColumnName}, {DatabaseConstants.KkutuWordIndexColumnName}, {DatabaseConstants.WordColumnName}, {DatabaseConstants.FlagsColumnName}) VALUES(@lafHead, @falHead, @kkutuHead, @word, {(int)flags})",
				connection.CreateParameter(CommonDatabaseType.Character, 1, "@lafHead", word.GetLaFHeadNode()),
				connection.CreateParameter(CommonDatabaseType.Character, 1, "@falHead", word.GetFaLHeadNode()),
				connection.CreateParameter(CommonDatabaseType.CharacterVarying, 2, "@kkutuHead", word.GetKkutuHeadNode()),
				connection.CreateParameter("@word", word));
			return true;
		}

		public static void CheckTable(this CommonDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			// Create node tables
			foreach (string tableName in new string[] { DatabaseConstants.EndWordListTableName, DatabaseConstants.AttackWordListTableName, DatabaseConstants.ReverseEndWordListTableName, DatabaseConstants.ReverseAttackWordListTableName, DatabaseConstants.KkutuEndWordListTableName, DatabaseConstants.KkutuAttackWordListTableName })
				connection.MakeTableIfNotExists(tableName);

			// Create word list table
			if (!connection.IsTableExists(DatabaseConstants.WordListTableName))
				connection.MakeTable(DatabaseConstants.WordListTableName);
			else
				connection.CheckBackwardCompatibility();

			// Create indexes
			foreach (string columnName in new string[] { DatabaseConstants.WordIndexColumnName, DatabaseConstants.ReverseWordIndexColumnName, DatabaseConstants.KkutuWordIndexColumnName })
				connection.CreateIndex(DatabaseConstants.WordListTableName, columnName);
		}

		public static int DeduplicateDatabase(this CommonDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			return connection.ExecuteNonQuery(DatabaseConstants.DeduplicationQuery);
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

		public static int DeleteWord(this CommonDatabaseConnection connection, string word)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			return connection.ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = @word", connection.CreateParameter("@word", word));
		}

		public static ICollection<string> GetNodeList(this CommonDatabaseConnection connection, string tableName)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			var result = new List<string>();

			using (DbDataReader reader = connection.ExecuteReader($"SELECT * FROM {tableName}"))
			{
				int wordIndexOrdinal = reader.GetOrdinal(DatabaseConstants.WordIndexColumnName);
				while (reader.Read())
					result.Add(reader.GetString(wordIndexOrdinal));
			}

			Logger.InfoFormat("Found Total {0} nodes in {1}.", result.Count, tableName);
			return result;
		}

		public static void MakeTable(this CommonDatabaseConnection connection, string tablename)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			string columnOptions = tablename switch
			{
				DatabaseConstants.WordListTableName => connection.GetWordListColumnOptions(),
				DatabaseConstants.KkutuEndWordListTableName => $"{DatabaseConstants.WordIndexColumnName} VARCHAR(2) NOT NULL",
				_ => $"{DatabaseConstants.WordIndexColumnName} CHAR(1) NOT NULL",
			};
			connection.ExecuteNonQuery($"CREATE TABLE {tablename} ({columnOptions});");
		}

		public static void MakeTableIfNotExists(this CommonDatabaseConnection connection, string tableName)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			if (!connection.IsTableExists(tableName))
				connection.MakeTable(tableName);
		}

		private static void CreateIndex(this CommonDatabaseConnection connection, string tableName, string columnName) => connection.ExecuteNonQuery($"CREATE INDEX IF NOT EXISTS {columnName} ON {tableName} ({columnName})");
	}
}
