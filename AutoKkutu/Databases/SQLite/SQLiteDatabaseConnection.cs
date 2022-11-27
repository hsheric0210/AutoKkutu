using AutoKkutu.Databases.Extension;
using Microsoft.Data.Sqlite;

namespace AutoKkutu.Databases.SQLite
{
	public class SQLiteDatabaseConnection : CommonDatabaseConnection
	{
		private readonly SqliteConnection Connection;

		public SQLiteDatabaseConnection(SqliteConnection connection) => Connection = connection;

		public override void AddSequenceColumnToWordList() => RebuildWordList();

		public override void ChangeWordListColumnType(string tableName, string columnName, string newType) => RebuildWordList();

		public override CommonDatabaseCommand CreateCommand(string command, bool noPrepare = false) => new SQLiteDatabaseCommand(Connection, command, noPrepare);

		public override CommonDatabaseParameter CreateParameter(string name, object? value) => new SQLiteDatabaseParameter(name, value);

		public override CommonDatabaseParameter CreateParameter(CommonDatabaseType dataType, string name, object? value) => new SQLiteDatabaseParameter(dataType, name, value);

		public override CommonDatabaseParameter CreateParameter(CommonDatabaseType dataType, byte precision, string name, object? value) => new SQLiteDatabaseParameter(dataType, precision, name, value);

		public override void DropWordListColumn(string columnName) => RebuildWordList();

		public override string? GetColumnType(string tableName, string columnName) => SQLiteDatabaseHelper.GetColumnType(Connection, tableName ?? DatabaseConstants.WordListTableName, columnName);

		public override string GetRearrangeFuncName() => "Rearrange";

		public override string GetRearrangeMissionFuncName() => "Rearrange_Mission";

		public override string GetWordListColumnOptions() => "seq INTEGER PRIMARY KEY AUTOINCREMENT, word VARCHAR(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

		public override bool IsColumnExists(string tableName, string columnName) => SQLiteDatabaseHelper.IsColumnExists(Connection, tableName, columnName);

		public override bool IsTableExists(string tablename) => SQLiteDatabaseHelper.IsTableExists(Connection, tablename);

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
