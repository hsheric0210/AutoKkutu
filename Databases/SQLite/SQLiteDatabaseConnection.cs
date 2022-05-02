using Microsoft.Data.Sqlite;
using System.Data;
using System.Linq;

namespace AutoKkutu.Databases.SQLite
{
	public class SQLiteDatabaseConnection : CommonDatabaseConnection
	{
		private readonly SqliteConnection Connection;

		public SQLiteDatabaseConnection(SqliteConnection connection) => Connection = connection;

		public override void AddSequenceColumnToWordList() => RebuildWordList();

		public override void ChangeWordListColumnType(string columnName, string newType, string tableName = null) => RebuildWordList();

		public override CommonDatabaseParameter CreateParameter(string name, object value) => new SQLiteDatabaseParameter(name, value);

		public override CommonDatabaseParameter CreateParameter(CommonDatabaseType dataType, string name, object value) => new SQLiteDatabaseParameter(dataType, name, value);

		public override CommonDatabaseParameter CreateParameter(CommonDatabaseType dataType, byte precision, string name, object value) => new SQLiteDatabaseParameter(dataType, precision, name, value);

		public override CommonDatabaseParameter CreateParameter(ParameterDirection direction, CommonDatabaseType dataType, byte precision, string name, object value) => new SQLiteDatabaseParameter(direction, dataType, precision, name, value);

		public override void DropWordListColumn(string columnName) => RebuildWordList();

		public override int ExecuteNonQuery(string query, params CommonDatabaseParameter[] parameters) => SQLiteDatabaseHelper.ExecuteNonQuery(Connection, query, TranslateParameter(parameters));

		public override CommonDatabaseReader ExecuteReader(string query, params CommonDatabaseParameter[] parameters) => new SQLiteDatabaseReader(SQLiteDatabaseHelper.ExecuteReader(Connection, query, TranslateParameter(parameters)));

		public override object ExecuteScalar(string query, params CommonDatabaseParameter[] parameters) => SQLiteDatabaseHelper.ExecuteScalar(Connection, query, TranslateParameter(parameters));

		public override string GetCheckMissionCharFuncName() => "CheckMissionChar";

		public override string GetColumnType(string columnName, string tableName = null) => SQLiteDatabaseHelper.GetColumnType(Connection, tableName ?? DatabaseConstants.WordListTableName, columnName);

		public override string GetWordListColumnOptions() => "seq INTEGER PRIMARY KEY AUTOINCREMENT, word VARCHAR(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

		public override bool IsColumnExists(string columnName, string tableName = null) => SQLiteDatabaseHelper.IsColumnExists(Connection, tableName, columnName);

		public override bool IsTableExists(string tablename) => SQLiteDatabaseHelper.IsTableExists(Connection, tablename);

		public override void PerformVacuum() => ExecuteNonQuery("VACUUM");

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				Connection.Dispose();
			base.Dispose(disposing);
		}

		private static SqliteParameter[] TranslateParameter(params CommonDatabaseParameter[] parameters) => (from parameter in parameters where parameter is SQLiteDatabaseParameter let sqliteParam = (SQLiteDatabaseParameter)parameter select sqliteParam.Translate()).ToArray();

		private void RebuildWordList()
		{
			ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} RENAME TO _{DatabaseConstants.WordListTableName};");
			this.MakeTable(DatabaseConstants.WordListTableName);
			ExecuteNonQuery($"INSERT INTO {DatabaseConstants.WordListTableName} (word, word_index, reverse_word_index, kkutu_index, flags) SELECT word, word_index, reverse_word_index, kkutu_index, flags FROM _{DatabaseConstants.WordListTableName};");
			ExecuteNonQuery($"DROP TABLE _{DatabaseConstants.WordListTableName};");
		}
	}
}
