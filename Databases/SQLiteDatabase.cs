﻿using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu.Databases
{
	public partial class SQLiteDatabase : CommonDatabase
	{
		private static SqliteConnection DatabaseConnection;

		private static readonly string DatabaseFilePath = $"{Environment.CurrentDirectory}\\{DatabaseConstants.DatabaseFileName}";

		public SQLiteDatabase() : base()
		{
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

				DatabaseConnection.CreateFunction(GetCheckMissionCharFuncName(), (string str, string ch) =>
				{
					int occurrence = 0;
					char target = ch.First();
					foreach (char c in str.ToCharArray())
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
				Logger.Error("Failed to connect to the database", ex);
				if (DBError != null)
					DBError(null, EventArgs.Empty);
			}
		}

		public override string GetCheckMissionCharFuncName() => "CheckMissionChar()";

		public override string GetDBInfo() => "SQLite";

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

		protected override bool IsColumnExists(string columnName, string tableName = null) => SQLiteDatabaseHelper.IsColumnExists(DatabaseConnection, tableName ?? DatabaseConstants.WordListName, columnName);

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
			return SQLiteDatabaseHelper.ExecuteNonQuery((SqliteConnection)connection, DatabaseConstants.SQLiteDeduplicationQuery);
		}

		protected override IDisposable OpenSecondaryConnection()
		{
			return SQLiteDatabaseHelper.OpenConnection(DatabaseFilePath);
		}
	}

	public class SQLiteDatabaseReader : CommonDatabaseReader
	{
		private SqliteDataReader Reader;
		public SQLiteDatabaseReader(SqliteDataReader reader) => Reader = reader;

		public object GetObject(string name) => Reader[name];
		public bool Read() => Reader.Read();
		void IDisposable.Dispose() => Reader.Dispose();
	}
}
