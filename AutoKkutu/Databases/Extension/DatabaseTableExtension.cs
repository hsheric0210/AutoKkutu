using System;

namespace AutoKkutu.Databases.Extension
{
	public static class DatabaseTableExtension
	{
		public static void CheckTable(this CommonDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			// Create node tables
			foreach (string tableName in new string[] { DatabaseConstants.EndWordListTableName, DatabaseConstants.AttackWordListTableName, DatabaseConstants.ReverseEndWordListTableName, DatabaseConstants.ReverseAttackWordListTableName, DatabaseConstants.KkutuEndWordListTableName, DatabaseConstants.KkutuAttackWordListTableName })
				connection.MakeTableIfNotExists(tableName);

			connection.MakeTableIfNotExists(DatabaseConstants.KKTEndWordListTableName, () => connection.ExecuteNonQuery($"INSERT INTO {DatabaseConstants.KKTEndWordListTableName} SELECT * FROM {DatabaseConstants.EndWordListTableName}"));
			connection.MakeTableIfNotExists(DatabaseConstants.KKTAttackWordListTableName, () => connection.ExecuteNonQuery($"INSERT INTO {DatabaseConstants.KKTAttackWordListTableName} SELECT * FROM {DatabaseConstants.AttackWordListTableName}"));

			// Create word list table
			if (!connection.IsTableExists(DatabaseConstants.WordListTableName))
				connection.MakeTable(DatabaseConstants.WordListTableName);
			else
				connection.CheckBackwardCompatibility();

			// Create indexes
			foreach (string columnName in new string[] { DatabaseConstants.WordIndexColumnName, DatabaseConstants.ReverseWordIndexColumnName, DatabaseConstants.KkutuWordIndexColumnName })
				connection.CreateIndex(DatabaseConstants.WordListTableName, columnName);
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

		public static void MakeTableIfNotExists(this CommonDatabaseConnection connection, string tableName, Action? callback = null)
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
