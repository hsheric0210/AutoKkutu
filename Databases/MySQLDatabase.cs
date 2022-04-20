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

		public MySQLDatabase(string connectionString) : base()
		{
			ConnectionString = connectionString;

			try
			{
				// Open the connection
				Logger.Info("Opening database connection...");
				DatabaseConnection = new MySqlConnection(connectionString);
				DatabaseConnection.Open();

				ExecuteNonQuery(DatabaseConstants.MissionWordOccurrenceFinder(GetCheckMissionCharFuncName()));

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

		public override string GetCheckMissionCharFuncName() => "__AutoKkutu_CheckMissionChar";

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
				Logger.Error($"Failed to execute SQLite query '{query}'", ex);
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
			return ExecuteNonQuery(DatabaseConstants.SQLiteDeduplicationQuery, (MySqlConnection)connection);
		}

		protected override IDisposable OpenSecondaryConnection()
		{
			var connection = new MySqlConnection(ConnectionString);
			connection.Open();
			return connection;
		}

		protected override bool IsColumnExists(string columnName, string tableName = null, IDisposable connection = null) => throw new NotImplementedException();
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
