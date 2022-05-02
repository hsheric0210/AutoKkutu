using AutoKkutu.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using AutoKkutu.Constants;
using log4net;

namespace AutoKkutu.Databases
{
	public static class DatabaseExtension
	{
		private static readonly ILog Logger = LogManager.GetLogger("Database Exts");

		public static bool AddNode(this CommonDatabaseConnection connection, string node, string tableName = null)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException(nameof(node));

			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndWordListTableName;

			if (Convert.ToInt32(connection.ExecuteScalar($"SELECT COUNT(*) FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = @node;", connection.CreateParameter("@node", node[0])), CultureInfo.InvariantCulture) > 0)
				return false;

			connection.ExecuteNonQuery($"INSERT INTO {tableName}({DatabaseConstants.WordIndexColumnName}) VALUES('{node[0]}')");
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

			connection.ExecuteNonQuery($"INSERT INTO {DatabaseConstants.WordListTableName}({DatabaseConstants.WordIndexColumnName}, {DatabaseConstants.ReverseWordIndexColumnName}, {DatabaseConstants.KkutuWordIndexColumnName}, {DatabaseConstants.WordColumnName}, {DatabaseConstants.FlagsColumnName}) VALUES('{word.GetLaFHeadNode()}', '{word.GetFaLHeadNode()}', '{word.GetKkutuHeadNode()}', '{word}', {((int)flags)})");
			return true;
		}

		public static void CheckTable(this CommonDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			// Create node tables
			foreach (var tableName in new string[] { DatabaseConstants.EndWordListTableName, DatabaseConstants.AttackWordListTableName, DatabaseConstants.ReverseEndWordListTableName, DatabaseConstants.ReverseAttackWordListTableName, DatabaseConstants.KkutuEndWordListTableName, DatabaseConstants.KkutuAttackWordListTableName })
				connection.MakeTableIfNotExists(tableName);

			// Create word list table
			if (!connection.IsTableExists(DatabaseConstants.WordListTableName))
				connection.MakeTable(DatabaseConstants.WordListTableName);
			else
				connection.CheckBackwardCompatibility();

			// Create indexes
			foreach (var columnName in new string[] { DatabaseConstants.WordIndexColumnName, DatabaseConstants.ReverseWordIndexColumnName, DatabaseConstants.KkutuWordIndexColumnName })
				connection.CreateIndex(DatabaseConstants.WordListTableName, columnName);
		}

		public static int DeduplicateDatabase(this CommonDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			return connection.ExecuteNonQuery(DatabaseConstants.DeduplicationQuery);
		}

		public static int DeleteNode(this CommonDatabaseConnection connection, string node, string tableName = null)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException(nameof(node));

			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndWordListTableName;

			return connection.ExecuteNonQuery($"DELETE FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = '{node}'");
		}

		public static int DeleteWord(this CommonDatabaseConnection connection, string word)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			return connection.ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = '{word}'");
		}

		public static ICollection<string> GetNodeList(this CommonDatabaseConnection connection, string tableName)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			var result = new List<string>();

			using (CommonDatabaseReader reader = connection.ExecuteReader($"SELECT * FROM {tableName}"))
				while (reader.Read())
					result.Add(reader.GetString(DatabaseConstants.WordIndexColumnName));
			Logger.InfoFormat("Found Total {0} nodes in {1}.", result.Count, tableName);
			return result;
		}

		public static void MakeTable(this CommonDatabaseConnection connection, string tablename)
		{
			string columnOptions;
			switch (tablename)
			{
				case DatabaseConstants.WordListTableName:
					columnOptions = connection.GetWordListColumnOptions();
					break;

				case DatabaseConstants.KkutuEndWordListTableName:
					columnOptions = $"{DatabaseConstants.WordIndexColumnName} VARCHAR(2) NOT NULL";
					break;

				default:
					columnOptions = $"{DatabaseConstants.WordIndexColumnName} CHAR(1) NOT NULL";
					break;
			}
			connection.ExecuteNonQuery($"CREATE TABLE {tablename} ({columnOptions});");
		}

		public static void MakeTableIfNotExists(this CommonDatabaseConnection connection, string tableName)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			if (!connection.IsTableExists(tableName))
				connection.MakeTable(tableName);
		}

		private static void CreateIndex(this CommonDatabaseConnection connection, string tableName, string columnName)
		{
			connection.ExecuteNonQuery($"CREATE INDEX IF NOT EXISTS {columnName} ON {tableName} ({columnName})");
		}

		// TODO: Move to utils
		private static bool GetWordIndexColumnName(GameMode gameMode, out string str)
		{
			switch (gameMode)
			{
				case GameMode.LastAndFirst:
				case GameMode.MiddleAddFirst:
					str = DatabaseConstants.WordIndexColumnName;
					break;

				case GameMode.FirstAndLast:
					str = DatabaseConstants.ReverseWordIndexColumnName;
					break;

				case GameMode.Kkutu:
					str = DatabaseConstants.KkutuWordIndexColumnName;
					break;

				default:
					str = null;
					return false;
			}

			return true;
		}
	}
}
