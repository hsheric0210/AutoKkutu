using AutoKkutu.Databases.Extension;
using Dapper;
using Microsoft.Data.Sqlite;
using Serilog;
using System;

namespace AutoKkutu.Databases.SQLite
{
	public class SqliteDatabaseConnection : CommonDatabaseConnection
	{
		private readonly SqliteConnection Connection;

		public SqliteDatabaseConnection(SqliteConnection connection) => Connection = connection;

		public override void AddSequenceColumnToWordList() => RebuildWordList();

		public override void ChangeWordListColumnType(string tableName, string columnName, string newType) => RebuildWordList();

		public override void DropWordListColumn(string columnName) => RebuildWordList();

		public override string? GetColumnType(string tableName, string columnName)
		{
			if (tableName == null)
				return DatabaseConstants.WordListTableName;

			try
			{
				var tableInfo = Connection.Query<SqliteTableInfo>("SELECT * FROM pragma_table_info(@TableName);", new
				{
					TableName = tableName
				});
				while (reader.Read())
				{
					if (reader.GetString(nameOrdinal).Equals(columnName, StringComparison.OrdinalIgnoreCase))
						return reader.GetString(typeOrdinal);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, DatabaseConstants.ErrorGetColumnType, columnName, tableName);
			}

			return null;
		}

		private class SqliteTableInfo
		{
			public string Name
			{
				get; set;
			}

			public string Type
			{
				get; set;
			}
		}

		public override string GetRearrangeFuncName() => "Rearrange";

		public override string GetRearrangeMissionFuncName() => "Rearrange_Mission";

		public override string GetWordListColumnOptions() => "seq INTEGER PRIMARY KEY AUTOINCREMENT, word VARCHAR(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

		public override bool IsColumnExists(string tableName, string columnName) => SqliteDatabaseHelper.IsColumnExists(Connection, tableName, columnName);

		public override bool IsTableExists(string tablename) => SqliteDatabaseHelper.IsTableExists(Connection, tablename);

		public override void PerformVacuum() => CreateCommand("VACUUM").ExecuteNonQuery();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				Connection.Dispose();
			base.Dispose(disposing);
		}

		private void RebuildWordList()
		{
			CreateCommand($"ALTER TABLE {DatabaseConstants.WordListTableName} RENAME TO _{DatabaseConstants.WordListTableName};").ExecuteNonQuery();
			this.MakeTable(DatabaseConstants.WordListTableName);
			CreateCommand($"INSERT INTO {DatabaseConstants.WordListTableName} (word, word_index, reverse_word_index, kkutu_index, flags) SELECT word, word_index, reverse_word_index, kkutu_index, flags FROM _{DatabaseConstants.WordListTableName};").ExecuteNonQuery();
			CreateCommand($"DROP TABLE _{DatabaseConstants.WordListTableName};").ExecuteNonQuery();
		}
	}
}
