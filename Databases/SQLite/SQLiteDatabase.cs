using AutoKkutu.Databases.Extension;
using Microsoft.Data.Sqlite;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AutoKkutu.Databases.SQLite
{
	public partial class SQLiteDatabase : DatabaseWithDefaultConnection
	{
		private readonly string DatabaseFilePath;

		public SQLiteDatabase(string fileName) : base()
		{
			DatabaseFilePath = $"{Environment.CurrentDirectory}\\{fileName}";

			try
			{
				// Create database if not exists
				if (!new FileInfo(DatabaseFilePath).Exists)
				{
					Logger.Info(CultureInfo.CurrentCulture, "Database file {databaseFilePath} doesn't exists; creating new one...", DatabaseFilePath);
					File.Create(DatabaseFilePath).Close();
				}

				// Open the connection
				Logger.Info("Opening database connection...");
				SqliteConnection connection = SQLiteDatabaseHelper.OpenConnection(DatabaseFilePath);
				RegisterDefaultConnection(new SQLiteDatabaseConnection(connection));
				RegisterRearrangeFunc(connection);
				RegisterRearrangeMissionFunc(connection);

				// Check the database tables
				DefaultConnection.CheckTable();

				Logger.Info("Successfully established database connection.");
			}
			catch (Exception ex)
			{
				Logger.Error(ex, DatabaseConstants.ErrorConnect);
				DatabaseEvents.TriggerDatabaseError();
			}
		}

		// Rearrange_Mission(string word, int flags, string missionword, int endWordFlag, int attackWordFlag, int endMissionWordOrdinal, int endWordOrdinal, int attackMissionWordOrdinal, int attackWordOrdinal, int missionWordOrdinal, int normalWordOrdinal)
		private void RegisterRearrangeMissionFunc(SqliteConnection connection) =>
			connection.CreateFunction(DefaultConnection!.GetRearrangeMissionFuncName(), (string word, int flags, string missionWord, int endWordFlag, int attackWordFlag, int endMissionWordOrdinal, int endWordOrdinal, int attackMissionWordOrdinal, int attackWordOrdinal, int missionWordOrdinal, int normalWordOrdinal) =>
			{
				char missionChar = char.ToUpperInvariant(missionWord[0]);
				int missionOccurrence = (from char c in word.ToUpperInvariant() where c == missionChar select c).Count();
				bool hasMission = missionOccurrence > 0;

				if ((flags & endWordFlag) != 0)
					return (hasMission ? endMissionWordOrdinal : endWordOrdinal) * DatabaseConstants.MaxWordPlusMissionLength + missionOccurrence * 256;
				if ((flags & attackWordFlag) != 0)
					return (hasMission ? attackMissionWordOrdinal : attackWordOrdinal) * DatabaseConstants.MaxWordPlusMissionLength + missionOccurrence * 256;
				return (hasMission ? missionWordOrdinal : normalWordOrdinal) * DatabaseConstants.MaxWordPlusMissionLength + missionOccurrence * 256;
			});

		// Rearrange(int endWordFlag, int attackWordFlag, int endWordOrdinal, int attackWordOrdinal, int normalWordOrdinal)
		private void RegisterRearrangeFunc(SqliteConnection connection) =>
			connection.CreateFunction(DefaultConnection.GetRearrangeFuncName(), (int flags, int endWordFlag, int attackWordFlag, int endWordOrdinal, int attackWordOrdinal, int normalWordOrdinal) =>
			{
				if ((flags & endWordFlag) != 0)
					return endWordOrdinal * DatabaseConstants.MaxWordLength;
				if ((flags & attackWordFlag) != 0)
					return attackWordOrdinal * DatabaseConstants.MaxWordLength;
				return normalWordOrdinal * DatabaseConstants.MaxWordLength;
			});

		public override void CheckConnectionType(CommonDatabaseConnection connection)
		{
			if (connection != null && connection.GetType() != typeof(SqliteConnection))
				throw new NotSupportedException($"Connection is not {nameof(SqliteConnection)}");
		}

		public override string GetDBType() => "SQLite";

		public override CommonDatabaseConnection OpenSecondaryConnection() => new SQLiteDatabaseConnection(SQLiteDatabaseHelper.OpenConnection(DatabaseFilePath));
	}
}
