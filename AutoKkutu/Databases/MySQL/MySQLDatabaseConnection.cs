using AutoKkutu.Databases.Extension;
using Serilog;
using MySqlConnector;
using System;
using System.Globalization;

namespace AutoKkutu.Databases.MySQL
{
	public partial class MySqlDatabaseConnection : CommonDatabaseConnection
	{
		private readonly MySqlConnection Connection; 
		private readonly string DatabaseName;

		public MySqlDatabaseConnection(MySqlConnection connection, string dbName)
		{
			Connection = connection;
			DatabaseName = dbName;
		}

		public override void AddSequenceColumnToWordList() => this.ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN seq NOT NULL AUTO_INCREMENT PRIMARY KEY;");

		public override void ChangeWordListColumnType(string tableName, string columnName, string newType) => this.ExecuteNonQuery($"ALTER TABLE {tableName} MODIFY {columnName} {newType}");

		public override void DropWordListColumn(string columnName) => this.ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} DROP {columnName}");

		public override string? GetColumnType(string tableName, string columnName)
		{
			try
			{
				return this.ExecuteScalar("SELECT data_type FROM Information_schema.columns WHERE table_name=@tableName AND column_name=@columnName;", CreateParameter("@tableName", tableName), CreateParameter("@columnName", columnName))?.ToString();
			}
			catch (Exception ex)
			{
				Log.Information(ex, DatabaseConstants.ErrorGetColumnType, columnName, tableName);
				return "";
			}
		}

		public override string GetRearrangeFuncName() => "__AutoKkutu_Rearrange";

		public override string GetRearrangeMissionFuncName() => "__AutoKkutu_RearrangeMission";

		public override string GetWordListColumnOptions() => "seq INT NOT NULL AUTO_INCREMENT PRIMARY KEY, word VARCHAR(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

		public override bool IsColumnExists(string tableName, string columnName)
		{
			tableName ??= DatabaseConstants.WordListTableName;
			try
			{
				return Convert.ToInt32(this.ExecuteScalar("SELECT COUNT(*) FROM Information_schema.columns WHERE table_schema=@dbName AND table_name=@tableName AND column_name=@columnName;", CreateParameter("@dbName", DatabaseName), CreateParameter("@tableName", tableName), CreateParameter("@columnName", columnName)), CultureInfo.InvariantCulture) > 0;
			}
			catch (Exception ex)
			{
				Log.Information(ex, DatabaseConstants.ErrorIsColumnExists, columnName, tableName);
				return false;
			}
		}

		public override bool IsTableExists(string tablename)
		{
			try
			{
				return Convert.ToInt32(this.ExecuteScalar("SELECT COUNT(*) FROM information_schema.tables WHERE table_name=@tableName;", CreateParameter("@tableName", tablename)), CultureInfo.InvariantCulture) > 0;
			}
			catch (Exception ex)
			{
				Log.Information(ex, DatabaseConstants.ErrorIsTableExists, tablename);
				return false;
			}
		}

		public override void PerformVacuum()
		{
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				Connection.Dispose();
			base.Dispose(disposing);
		}
	}
}
