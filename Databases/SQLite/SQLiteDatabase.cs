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
				SqliteConnection connection = SQLiteDatabaseHelper.OpenConnection(DatabaseFilePath);
				RegisterDefaultConnection(new SQLiteDatabaseConnection(connection));

				// Rearrange(int endWordFlag, int attackWordFlag, int endWordOrdinal, int attackWordOrdinal, int normalWordOrdinal)
				connection.CreateFunction(DefaultConnection.GetRearrangeFuncName(), (int flags, int endWordFlag, int attackWordFlag, int endWordOrdinal, int attackWordOrdinal, int normalWordOrdinal) =>
				{
					if ((flags & endWordFlag) != 0)
						return endWordOrdinal * DatabaseConstants.MaxWordLength;
					if ((flags & attackWordFlag) != 0)
						return attackWordOrdinal * DatabaseConstants.MaxWordLength;
					return normalWordOrdinal * DatabaseConstants.MaxWordLength;
				});

				// Rearrange_Mission(string word, int flags, string missionword, int endWordFlag, int attackWordFlag, int endMissionWordOrdinal, int endWordOrdinal, int attackMissionWordOrdinal, int attackWordOrdinal, int missionWordOrdinal, int normalWordOrdinal)
				connection.CreateFunction(DefaultConnection.GetRearrangeMissionFuncName(), (string word, int flags, string missionWord, int endWordFlag, int attackWordFlag, int endMissionWordOrdinal, int endWordOrdinal, int attackMissionWordOrdinal, int attackWordOrdinal, int missionWordOrdinal, int normalWordOrdinal) =>
				{
					char missionChar = char.ToUpperInvariant(missionWord[0]);
					int missionOccurrence = (from char c in word.ToUpperInvariant() where c == missionChar select c).Count();
					bool hasMission = missionOccurrence > 0;

					if ((flags & endWordFlag) != 0)
						return (hasMission ? endMissionWordOrdinal : endWordOrdinal) * DatabaseConstants.MaxWordPlusMissionLength + missionOccurrence;
					if ((flags & attackWordFlag) != 0)
						return (hasMission ? attackMissionWordOrdinal : attackWordOrdinal) * DatabaseConstants.MaxWordPlusMissionLength + missionOccurrence;
					return (hasMission ? missionWordOrdinal : normalWordOrdinal) * DatabaseConstants.MaxWordPlusMissionLength + missionOccurrence;
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

		public override CommonDatabaseConnection OpenSecondaryConnection() => new SQLiteDatabaseConnection(SQLiteDatabaseHelper.OpenConnection(DatabaseFilePath));
	}
}
