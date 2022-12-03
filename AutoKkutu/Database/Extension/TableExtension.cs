using Dapper;
using System;

namespace AutoKkutu.Database.Extension
{
	public static class TableExtension
	{
		public static void CheckTable(this AbstractDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			// Create node tables
			foreach (string tableName in new string[] { DatabaseConstants.EndNodeIndexTableName, DatabaseConstants.AttackNodeIndexTableName, DatabaseConstants.ReverseEndNodeIndexTableName, DatabaseConstants.ReverseAttackNodeIndexTableName, DatabaseConstants.KkutuEndNodeIndexTableName, DatabaseConstants.KkutuAttackNodeIndexTableName })
				connection.MakeTableIfNotExists(tableName);

			connection.MakeTableIfNotExists(DatabaseConstants.KKTEndNodeIndexTableName, () => connection.Execute($"INSERT INTO {DatabaseConstants.KKTEndNodeIndexTableName} SELECT * FROM {DatabaseConstants.EndNodeIndexTableName}"));
			connection.MakeTableIfNotExists(DatabaseConstants.KKTAttackNodeIndexTableName, () => connection.Execute($"INSERT INTO {DatabaseConstants.KKTAttackNodeIndexTableName} SELECT * FROM {DatabaseConstants.AttackNodeIndexTableName}"));

			// Create word list table
			if (!connection.IsTableExists(DatabaseConstants.WordTableName))
				connection.MakeTable(DatabaseConstants.WordTableName);
			else
				connection.CheckBackwardCompatibility();

			// Create indexes
			foreach (string columnName in new string[] { DatabaseConstants.WordIndexColumnName, DatabaseConstants.ReverseWordIndexColumnName, DatabaseConstants.KkutuWordIndexColumnName })
				connection.CreateIndex(DatabaseConstants.WordTableName, columnName);
		}

		public static void MakeTable(this AbstractDatabaseConnection connection, string tablename)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			string columnOptions = tablename switch
			{
				DatabaseConstants.WordTableName => connection.GetWordListColumnOptions(),
				DatabaseConstants.KkutuEndNodeIndexTableName => $"{DatabaseConstants.WordIndexColumnName} VARCHAR(2) NOT NULL",
				_ => $"{DatabaseConstants.WordIndexColumnName} CHAR(1) NOT NULL",
			};
			connection.Execute($"CREATE TABLE {tablename} ({columnOptions});");
		}

		public static void MakeTableIfNotExists(this AbstractDatabaseConnection connection, string tableName, Action? callback = null)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			if (!connection.IsTableExists(tableName))
			{
				connection.MakeTable(tableName);
				callback?.Invoke();
			}
		}
	}
}
