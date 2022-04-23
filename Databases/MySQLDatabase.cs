using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu.Databases
{
	public partial class MySQLDatabase : CommonDatabase
	{
		private static MySqlConnection DatabaseConnection;

		private string ConnectionString;

		private const string DatabaseName = "autokkutu";

		public MySQLDatabase(string connectionString) : base()
		{
			ConnectionString = connectionString;

			try
			{
				// Open the connection
				Logger.Info("Opening database connection...");
				DatabaseConnection = new MySqlConnection(connectionString);
				DatabaseConnection.Open();

				ExecuteNonQuery($"DROP FUNCTION IF EXISTS {GetCheckMissionCharFuncName()};");
				ExecuteNonQuery($@"CREATE FUNCTION {GetCheckMissionCharFuncName()}(word VARCHAR(256), missionWord VARCHAR(2)) RETURNS INT
DETERMINISTIC
NO SQL
BEGIN
	DECLARE occurrence INT;

	SET occurrence = ROUND((LENGTH(word) - LENGTH(REPLACE(LOWER(word), LOWER(missionWord), ''))) / LENGTH(missionWord));
	IF occurrence > 0 THEN
		RETURN occurrence + {DatabaseConstants.MissionCharIndexPriority};
	ELSE
		RETURN 0;
	END IF;
END;
");

				// Check the database tables
				CheckTable();

				Logger.Info("Successfully established database connection.");
			}
			catch (Exception ex)
			{
				Logger.Error("Failed to connect to the database", ex);
				if (DBError != null)
					DBError(null, EventArgs.Empty);
			}
		}

		protected override string GetCheckMissionCharFuncName() => "__AutoKkutu_CheckMissionChar";

		public override string GetDBInfo() => "MySQL";

		protected override int ExecuteNonQuery(string query, IDisposable connection = null)
		{
			CheckConnectionType(connection);
			try
			{
				using (var command = new MySqlCommand(query, (MySqlConnection)(connection ?? DatabaseConnection)))
					return command.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to execute MySQL query '{query}'", ex);
			}
			return -1;
		}

		protected override object ExecuteScalar(string query, IDisposable connection = null)
		{
			CheckConnectionType(connection);
			try
			{
				using (var command = new MySqlCommand(query, (MySqlConnection)(connection ?? DatabaseConnection)))
					return command.ExecuteScalar();
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to execute MySQL query '{query}'", ex);
			}
			return null;
		}

		protected override CommonDatabaseReader ExecuteReader(string query, IDisposable connection = null)
		{
			CheckConnectionType(connection);
			try
			{
				using (var command = new MySqlCommand(query, (MySqlConnection)(connection ?? DatabaseConnection)))
					return new MySQLDatabaseReader(command.ExecuteReader());
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to execute MySQL query '{query}'", ex);
			}
			return null;
		}

		private void CheckConnectionType(object connection)
		{
			if (connection != null && connection.GetType() != typeof(MySqlConnection))
				throw new ArgumentException("Connection is not SqliteConnection");
		}

		protected override int DeduplicateDatabase(IDisposable connection)
		{
			CheckConnectionType(connection);

			// Deduplicate db
			// https://wiki.postgresql.org/wiki/Deleting_duplicates
			return ExecuteNonQuery(DatabaseConstants.DeduplicationQuery, (MySqlConnection)connection);
		}

		protected override IDisposable OpenSecondaryConnection()
		{
			var connection = new MySqlConnection(ConnectionString);
			connection.Open();
			return connection;
		}

		protected override bool IsColumnExists(string columnName, string tableName = null, IDisposable connection = null)
		{
			tableName = tableName ?? DatabaseConstants.WordListName;
			try
			{
				return Convert.ToInt32(ExecuteScalar($"SELECT COUNT(*) FROM Information_schema.columns WHERE table_schema='{DatabaseName}' AND table_name='{tableName}' AND column_name='{columnName}';")) > 0;
			}
			catch (Exception ex)
			{
				Logger.Info($"Failed to check the existence of column '{columnName}' in table '{tableName}' : {ex.ToString()}");
				return false;
			}
		}

		public override bool IsTableExists(string tableName, IDisposable connection = null)
		{
			try
			{
				return Convert.ToInt32(ExecuteScalar($"SELECT COUNT(*) FROM information_schema.tables WHERE table_name='{tableName}';", connection).ToString()) > 0;
			}
			catch (Exception ex)
			{
				Logger.Info($"Failed to check the existence of table '{tableName}' : {ex.ToString()}");
				return false;
			}
		}

		protected override void AddSequenceColumnToWordList() => ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListName} ADD COLUMN seq NOT NULL AUTO_INCREMENT PRIMARY KEY;");

		protected override string GetWordListColumnOptions() => "seq INT NOT NULL AUTO_INCREMENT PRIMARY KEY, word VARCHAR(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

		protected override string GetColumnType(string columnName, string tableName = null, IDisposable connection = null)
		{
			tableName = tableName ?? DatabaseConstants.WordListName;
			try
			{
				return ExecuteScalar($"SELECT data_type FROM Information_schema.columns WHERE table_name='{tableName}' AND column_name='{columnName}';", connection).ToString();
			}
			catch (Exception ex)
			{
				Logger.Info($"Failed to get data type of column '{columnName}' in table '{tableName}' : {ex.ToString()}");
				return "";
			}
		}

		protected override void ChangeWordListColumnType(string columnName, string newType, string tableName = null, IDisposable connection = null) => ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListName} MODIFY {columnName} {newType}");

		protected override void DropWordListColumn(string columnName) => ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListName} DROP {columnName}");
	}

	public class MySQLDatabaseReader : CommonDatabaseReader
	{
		private MySqlDataReader Reader;
		public MySQLDatabaseReader(MySqlDataReader reader) => Reader = reader;

		public object GetObject(string name) => Reader[name];
		public string GetString(int index) => Reader.GetString(index);
		public int GetOrdinal(string name) => Reader.GetOrdinal(name);
		public bool Read() => Reader.Read();
		void IDisposable.Dispose() => Reader.Dispose();
	}
}
