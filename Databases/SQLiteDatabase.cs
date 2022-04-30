using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;

namespace AutoKkutu.Databases
{
	public partial class SQLiteDatabase : CommonDatabase
	{
		private static SqliteConnection DatabaseConnection;

		private string DatabaseFilePath;

		public SQLiteDatabase(string fileName) : base()
		{
			DatabaseFilePath = $"{Environment.CurrentDirectory}\\{fileName}";

			try
			{
				// Create database if not exists
				if (!new FileInfo(DatabaseFilePath).Exists)
				{
					Logger.Info($"Database file '{DatabaseFilePath}' doesn't exists; creating new one...");
					File.Create(DatabaseFilePath).Close();
				}

				// Open the connection
				Logger.Info("Opening database connection...");
				DatabaseConnection = SQLiteDatabaseHelper.OpenConnection(DatabaseFilePath);

				DatabaseConnection.CreateFunction(GetCheckMissionCharFuncName(), (string word, string missionWord) =>
				{
					int occurrence = 0;
					char target = char.ToLowerInvariant(missionWord.First());
					foreach (char c in word.ToLowerInvariant().ToCharArray())
						if (c == target)
							occurrence++;
					return occurrence > 0 ? DatabaseConstants.MissionCharIndexPriority + occurrence : 0;
				});

				// Check the database tables
				CheckTable();

				Logger.Info("Successfully established database connection.");
			}
			catch (Exception ex)
			{
				Logger.Error(DatabaseConstants.Error_Connect, ex);
				if (DBError != null)
					DBError(null, EventArgs.Empty);
			}
		}

		protected override string GetCheckMissionCharFuncName() => "CheckMissionChar";

		public override string GetDBType() => "SQLite";

		protected override int ExecuteNonQuery(string query, IDisposable connection = null)
		{
			CheckConnectionType(connection);
			return SQLiteDatabaseHelper.ExecuteNonQuery((SqliteConnection)(connection ?? DatabaseConnection), query);
		}

		protected override object ExecuteScalar(string query, IDisposable connection = null)
		{
			CheckConnectionType(connection);
			return SQLiteDatabaseHelper.ExecuteScalar((SqliteConnection)(connection ?? DatabaseConnection), query);
		}

		protected override CommonDatabaseReader ExecuteReader(string query, IDisposable connection = null)
		{
			CheckConnectionType(connection);
			return new SQLiteDatabaseReader(SQLiteDatabaseHelper.ExecuteReader((SqliteConnection)(connection ?? DatabaseConnection), query));
		}

		private void CheckConnectionType(object connection)
		{
			if (connection != null && connection.GetType() != typeof(SqliteConnection))
				throw new ArgumentException("Connection is not SqliteConnection");
		}

		protected override int DeduplicateDatabase(IDisposable connection)
		{
			CheckConnectionType(connection);

			// Deduplicate db
			// https://wiki.postgresql.org/wiki/Deleting_duplicates
			return SQLiteDatabaseHelper.ExecuteNonQuery((SqliteConnection)connection, DatabaseConstants.DeduplicationQuery);
		}

		protected override IDisposable OpenSecondaryConnection()
		{
			return SQLiteDatabaseHelper.OpenConnection(DatabaseFilePath);
		}

		protected override bool IsColumnExists(string columnName, string tableName = null, IDisposable connection = null) => SQLiteDatabaseHelper.IsColumnExists((SqliteConnection)(connection ?? DatabaseConnection), tableName ?? DatabaseConstants.WordListTableName, columnName);

		public override bool IsTableExists(string tablename, IDisposable connection = null) => SQLiteDatabaseHelper.IsTableExists((SqliteConnection)(connection ?? DatabaseConnection), tablename);

		protected override string GetColumnType(string columnName, string tableName = null, IDisposable connection = null) => SQLiteDatabaseHelper.GetColumnType((SqliteConnection)(connection ?? DatabaseConnection), tableName ?? DatabaseConstants.WordListTableName, columnName);

		protected override void AddSequenceColumnToWordList()
		{
			RebuildWordList();
		}

		protected override string GetWordListColumnOptions() => "seq INTEGER PRIMARY KEY AUTOINCREMENT, word VARCHAR(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

		protected override void ChangeWordListColumnType(string columnName, string newType, string tableName = null, IDisposable connection = null)
		{
			RebuildWordList();
		}

		protected override void DropWordListColumn(string columnName)
		{
			RebuildWordList();
		}

		private void RebuildWordList()
		{
			ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} RENAME TO _{DatabaseConstants.WordListTableName};");
			MakeTable(DatabaseConstants.WordListTableName);
			ExecuteNonQuery($"INSERT INTO {DatabaseConstants.WordListTableName} (word, word_index, reverse_word_index, kkutu_index, flags) SELECT word, word_index, reverse_word_index, kkutu_index, flags FROM _{DatabaseConstants.WordListTableName};");
			ExecuteNonQuery($"DROP TABLE _{DatabaseConstants.WordListTableName};");
		}

		protected override void PerformVacuum() => ExecuteNonQuery("VACUUM");

		public override void Dispose() => DatabaseConnection.Dispose();
	}

	public class SQLiteDatabaseReader : CommonDatabaseReader
	{
		private SqliteDataReader Reader;
		public SQLiteDatabaseReader(SqliteDataReader reader) => Reader = reader;

		protected override object GetObject(string name) => Reader[name];
		public override string GetString(int index) => Reader.GetString(index);
		public override int GetOrdinal(string name) => Reader.GetOrdinal(name);
		public override int GetInt32(int index) => Reader.GetInt32(index);
		public override bool Read() => Reader.Read();
		public override void Dispose() => Reader.Dispose();
	}
}
