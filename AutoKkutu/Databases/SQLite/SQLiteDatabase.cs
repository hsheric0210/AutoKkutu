using AutoKkutu.Databases.Extension;
using Microsoft.Data.Sqlite;
using Serilog;
using System;
using System.IO;
using System.Linq;

namespace AutoKkutu.Databases.SQLite
{
	public partial class SqliteDatabase : AbstractDatabase
	{
		private readonly string DataSource;

		public SqliteDatabase(string fileName) : base()
		{
			DataSource = $"{Environment.CurrentDirectory}\\{fileName}";

			try
			{
				// Create database if not exists
				if (!new FileInfo(DataSource).Exists)
				{
					Log.Information("Inexistent SQLite database file {file} will be newly created.", DataSource);
					File.Create(DataSource).Close();
				}

				// Open the connection
				Log.Information("Establishing SQLite connection for {file}.", DataSource);
				SqliteConnection connection = SqliteDatabaseHelper.OpenConnection(DataSource);
				Initialize(new SqliteDatabaseConnection(connection));
				RegisterWordPriorityFunc(connection);
				RegisterMissionWordPriorityFunc(connection);

				// Check the database tables
				Connection.CheckTable();

				Log.Information("Established SQLite connection.");
			}
			catch (Exception ex)
			{
				Log.Error(ex, DatabaseConstants.ErrorConnect);
				DatabaseEvents.TriggerDatabaseError();
			}
		}

		// Rearrange_Mission(string word, int flags, string missionword, int endWordFlag, int attackWordFlag, int endMissionWordOrdinal, int endWordOrdinal, int attackMissionWordOrdinal, int attackWordOrdinal, int missionWordOrdinal, int normalWordOrdinal)
		private void RegisterMissionWordPriorityFunc(SqliteConnection connection) =>
			connection.CreateFunction(GetMissionWordPriorityFuncName(), (string word, int flags, string missionWord, int endWordFlag, int attackWordFlag, int endMissionWordOrdinal, int endWordOrdinal, int attackMissionWordOrdinal, int attackWordOrdinal, int missionWordOrdinal, int normalWordOrdinal) =>
			{
				char missionChar = char.ToUpperInvariant(missionWord[0]);
				int missionOccurrence = (from char c in word.ToUpperInvariant() where c == missionChar select c).Count();
				bool hasMission = missionOccurrence > 0;

				if ((flags & endWordFlag) != 0)
					return (hasMission ? endMissionWordOrdinal : endWordOrdinal) * DatabaseConstants.MaxWordPriorityLength + missionOccurrence * 256;
				if ((flags & attackWordFlag) != 0)
					return (hasMission ? attackMissionWordOrdinal : attackWordOrdinal) * DatabaseConstants.MaxWordPriorityLength + missionOccurrence * 256;
				return (hasMission ? missionWordOrdinal : normalWordOrdinal) * DatabaseConstants.MaxWordPriorityLength + missionOccurrence * 256;
			});

		// Rearrange(int endWordFlag, int attackWordFlag, int endWordOrdinal, int attackWordOrdinal, int normalWordOrdinal)
		private void RegisterWordPriorityFunc(SqliteConnection connection) =>
			connection.CreateFunction(GetWordPriorityFuncName(), (int flags, int endWordFlag, int attackWordFlag, int endWordOrdinal, int attackWordOrdinal, int normalWordOrdinal) =>
			{
				if ((flags & endWordFlag) != 0)
					return endWordOrdinal * DatabaseConstants.MaxWordLength;
				if ((flags & attackWordFlag) != 0)
					return attackWordOrdinal * DatabaseConstants.MaxWordLength;
				return normalWordOrdinal * DatabaseConstants.MaxWordLength;
			});

		public override string GetWordPriorityFuncName() => "WordPriority";

		public override string GetMissionWordPriorityFuncName() => "MissionWordPriority";

		public override string GetDBType() => "SQLite";

		public override AbstractDatabaseConnection OpenSecondaryConnection() => new SqliteDatabaseConnection(SqliteDatabaseHelper.OpenConnection(DataSource));
	}
}
