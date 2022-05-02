using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;

namespace AutoKkutu.Databases.SQLite
{
	public partial class SQLiteDatabase : DatabaseWithDefaultConnection
	{
		private readonly string DatabaseFilePath;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000", Justification = "Implemented Dispose()")]
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
				var connection = SQLiteDatabaseHelper.OpenConnection(DatabaseFilePath);
				RegisterDefaultConnection(new SQLiteDatabaseConnection(connection));

				connection.CreateFunction(DefaultConnection.GetCheckMissionCharFuncName(), (string word, string missionWord) =>
				{
					char target = char.ToUpperInvariant(missionWord.First());
					int occurrence = (from char c in word.ToUpperInvariant() where c == target select c).Count();
					return occurrence > 0 ? DatabaseConstants.MissionCharIndexPriority + occurrence : 0;
				});

				// Check the database tables
				DefaultConnection.CheckTable();

				Logger.Info("Successfully established database connection.");
			}
			catch (Exception ex)
			{
				Logger.Error(DatabaseConstants.ErrorConnect, ex);
				DatabaseEvents.TriggerDatabaseError();
			}
		}

		public override void CheckConnectionType(CommonDatabaseConnection connection)
		{
			if (connection != null && connection.GetType() != typeof(SqliteConnection))
				throw new NotSupportedException($"Connection is not {nameof(SqliteConnection)}");
		}

		public override string GetDBType() => "SQLite";

		public override CommonDatabaseConnection OpenSecondaryConnection()
		{
			return new SQLiteDatabaseConnection(SQLiteDatabaseHelper.OpenConnection(DatabaseFilePath));
		}
	}
}
