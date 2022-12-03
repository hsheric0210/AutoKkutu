using Serilog;
using MySqlConnector;
using System;
using Dapper;

namespace AutoKkutu.Database.MySQL
{
	public class MySqlDatabaseConnection : AbstractDatabaseConnection
	{
		private readonly string DatabaseName;

		public MySqlDatabaseConnection(MySqlConnection connection, string dbName)
		{
			DatabaseName = dbName;
			Initialize(connection);
		}

		public override void AddSequenceColumnToWordList() => this.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} ADD COLUMN seq NOT NULL AUTO_INCREMENT PRIMARY KEY;");

		public override void ChangeWordListColumnType(string tableName, string columnName, string newType) => this.Execute($"ALTER TABLE {tableName} MODIFY {columnName} {newType}");

		public override void DropWordListColumn(string columnName) => this.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} DROP {columnName}");

		public override string? GetColumnType(string tableName, string columnName)
		{
			try
			{
				return this.ExecuteScalar<string>("SELECT data_type FROM Information_schema.columns WHERE table_name=@TableName AND column_name=@ColumnName;", new
				{
					TableName = tableName,
					ColumnName = columnName
				});
			}
			catch (Exception ex)
			{
				Log.Information(ex, DatabaseConstants.ErrorGetColumnType, columnName, tableName);
				return "";
			}
		}

		public override string GetWordPriorityFuncName() => "__AutoKkutu_Rearrange";

		public override string GetMissionWordPriorityFuncName() => "__AutoKkutu_RearrangeMission";

		public override string GetWordListColumnOptions() => "seq INT NOT NULL AUTO_INCREMENT PRIMARY KEY, word VARCHAR(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

		public override bool IsColumnExists(string tableName, string columnName)
		{
			tableName ??= DatabaseConstants.WordTableName;
			try
			{
				return this.ExecuteScalar<int>("SELECT COUNT(*) FROM Information_schema.columns WHERE table_schema=@DbName AND table_name=@TableName AND column_name=@ColumnName;", new
				{
					DbName = DatabaseName,
					TableName = tableName,
					ColumnName = columnName
				}) > 0;
			}
			catch (Exception ex)
			{
				Log.Information(ex, DatabaseConstants.ErrorIsColumnExists, columnName, tableName);
				return false;
			}
		}

		public override bool IsTableExists(string tableName)
		{
			try
			{
				return this.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_name=@TableName;", new
				{
					TableName = tableName
				}) > 0;
			}
			catch (Exception ex)
			{
				Log.Information(ex, DatabaseConstants.ErrorIsTableExists, tableName);
				return false;
			}
		}

		public override void ExecuteVacuum()
		{
		}
	}
}
