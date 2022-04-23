using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu.Databases
{
	public partial class PostgreSQLDatabase : CommonDatabase
	{
		private static NpgsqlConnection DatabaseConnection;

		private string ConnectionString;

		public PostgreSQLDatabase(string connectionString) : base()
		{
			ConnectionString = connectionString;

			try
			{
				// Open the connection
				Logger.Info("Opening database connection...");
				DatabaseConnection = new NpgsqlConnection(connectionString);
				DatabaseConnection.Open();
				TryExecuteNonQuery("set application name",$"SET Application_Name TO 'AutoKkutu v{MainWindow.VERSION}';");

				TryExecuteNonQuery("register checkMissionCharFunc", $@"CREATE OR REPLACE FUNCTION {GetCheckMissionCharFuncName()}(word VARCHAR, missionWord VARCHAR)
RETURNS INTEGER AS $$
DECLARE
	occurrence INTEGER;
BEGIN
	occurrence := ROUND((LENGTH(word) - LENGTH(REPLACE(LOWER(word), LOWER(missionWord), ''))) / LENGTH(missionWord));
	IF occurrence > 0 THEN
		RETURN occurrence + {DatabaseConstants.MissionCharIndexPriority};
	ELSE
		RETURN 0;
	END IF;
END;
$$ LANGUAGE plpgsql
");

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

		protected override string GetCheckMissionCharFuncName() => "__AutoKkutu_CheckMissionChar";

		public override string GetDBInfo() => "PostgreSQL";

		protected override int ExecuteNonQuery(string query, IDisposable connection = null)
		{
			CheckConnectionType(connection);
			using (var command = new NpgsqlCommand(query, (NpgsqlConnection)(connection ?? DatabaseConnection)))
				return command.ExecuteNonQuery();
		}

		protected override object ExecuteScalar(string query, IDisposable connection = null)
		{
			CheckConnectionType(connection);
			using (var command = new NpgsqlCommand(query, (NpgsqlConnection)(connection ?? DatabaseConnection)))
				return command.ExecuteScalar();

		}

		protected override CommonDatabaseReader ExecuteReader(string query, IDisposable connection = null)
		{
			CheckConnectionType(connection);
			using (var command = new NpgsqlCommand(query, (NpgsqlConnection)(connection ?? DatabaseConnection)))
				return new PostgreSQLDatabaseReader(command.ExecuteReader());
		}

		private void CheckConnectionType(object connection)
		{
			if (connection != null && connection.GetType() != typeof(NpgsqlConnection))
				throw new ArgumentException("Connection is not NpgsqlConnection");
		}

		protected override int DeduplicateDatabase(IDisposable connection)
		{
			CheckConnectionType(connection);

			// https://wiki.postgresql.org/wiki/Deleting_duplicates
			return ExecuteNonQuery(DatabaseConstants.DeduplicationQuery, (NpgsqlConnection)connection);
		}

		protected override IDisposable OpenSecondaryConnection()
		{
			var connection = new NpgsqlConnection(ConnectionString);
			connection.Open();
			TryExecuteNonQuery("set application name", $"SET Application_Name TO 'AutoKkutu v{MainWindow.VERSION}';", connection);
			return connection;
		}

		protected override bool IsColumnExists(string columnName, string tableName = null, IDisposable connection = null)
		{
			tableName = tableName ?? DatabaseConstants.WordListTableName;
			try
			{
				return Convert.ToInt32(ExecuteScalar($"SELECT COUNT(*) FROM information_schema.columns WHERE table_name='{tableName}' AND column_name='{columnName}';")) > 0;
			}
			catch (Exception ex)
			{
				Logger.Error(string.Format(DatabaseConstants.Error_IsColumnExists, columnName, tableName), ex);
				return false;
			}
		}

		public override bool IsTableExists(string tableName, IDisposable connection = null)
		{
			try
			{
				return Convert.ToInt32(ExecuteScalar($"SELECT COUNT(*) FROM information_schema.tables WHERE table_name='{tableName}';", connection)) > 0;
			}
			catch (Exception ex)
			{
				Logger.Error(string.Format(DatabaseConstants.Error_IsTableExists, tableName), ex);
				return false;
			}
		}

		protected override void AddSequenceColumnToWordList() => ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN seq SERIAL PRIMARY KEY");

		protected override string GetWordListColumnOptions() => "seq SERIAL PRIMARY KEY, word VARCHAR(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

		protected override string GetColumnType(string columnName, string tableName = null, IDisposable connection = null)
		{
			tableName = tableName ?? DatabaseConstants.WordListTableName;
			try
			{
				return ExecuteScalar($"SELECT data_type FROM information_schema.columns WHERE table_name='{tableName}' AND column_name='{columnName}';", connection).ToString();
			}
			catch (Exception ex)
			{
				Logger.Error(string.Format(DatabaseConstants.Error_GetColumnType, columnName, tableName), ex);
			}
			return "";
		}

		protected override void ChangeWordListColumnType(string columnName, string newType, string tableName = null, IDisposable connection = null) => ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} ALTER COLUMN {columnName} TYPE {newType}");

		protected override void DropWordListColumn(string columnName) => ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} DROP COLUMN {columnName}");

		protected override void PerformVacuum() => ExecuteNonQuery("VACUUM");
	}

	public class PostgreSQLDatabaseReader : CommonDatabaseReader
	{
		private NpgsqlDataReader Reader;
		public PostgreSQLDatabaseReader(NpgsqlDataReader reader) => Reader = reader;

		protected override object GetObject(string name) => Reader[name];
		public override string GetString(int index) => Reader.GetString(index);
		public override int GetOrdinal(string name) => Reader.GetOrdinal(name);
		public override int GetInt32(int index) => Reader.GetInt32(index);
		public override bool Read() => Reader.Read();
		public override void Dispose() => Reader.Dispose();
	}
}
