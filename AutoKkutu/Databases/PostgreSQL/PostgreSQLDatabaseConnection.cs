using AutoKkutu.Databases.Extension;
using Serilog;
using Npgsql;
using System;
using System.Globalization;

namespace AutoKkutu.Databases.PostgreSQL
{
	public partial class PostgreSqlDatabaseConnection : AbstractDatabaseConnection
	{
		private readonly NpgsqlConnection Connection;

		public PostgreSqlDatabaseConnection(NpgsqlConnection connection) => Connection = connection;

		public override void AddSequenceColumnToWordList() => this.ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordTableName} ADD COLUMN seq SERIAL PRIMARY KEY");

		public override void ChangeWordListColumnType(string tableName, string columnName, string newType) => this.ExecuteNonQuery($"ALTER TABLE {tableName} ALTER COLUMN {columnName} TYPE {newType}");

		public override void DropWordListColumn(string columnName) => this.ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordTableName} DROP COLUMN {columnName}");

		public override string? GetColumnType(string tableName, string columnName)
		{
			tableName ??= DatabaseConstants.WordTableName;
			try
			{
				return this.ExecuteScalar("SELECT data_type FROM information_schema.columns WHERE table_name=@tableName AND column_name=@columnName;", CreateParameter("@tableName", tableName), CreateParameter("@columnName", columnName))?.ToString();
			}
			catch (Exception ex)
			{
				Log.Error(ex, DatabaseConstants.ErrorGetColumnType, columnName, tableName);
			}
			return null;
		}

		public override string GetRearrangeFuncName() => "__AutoKkutu_Rearrange";

		public override string GetRearrangeMissionFuncName() => "__AutoKkutu_RearrangeMission";

		public override string GetWordListColumnOptions() => "seq SERIAL PRIMARY KEY, word CHAR VARYING(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

		public override bool IsColumnExists(string tableName, string columnName)
		{
			tableName ??= DatabaseConstants.WordTableName;
			try
			{
				return Convert.ToInt32(this.ExecuteScalar("SELECT COUNT(*) FROM information_schema.columns WHERE table_name=@tableName AND column_name=@columnName;", CreateParameter("@tableName", tableName), CreateParameter("@columnName", columnName)), CultureInfo.InvariantCulture) > 0;
			}
			catch (Exception ex)
			{
				Log.Error(ex, DatabaseConstants.ErrorIsColumnExists, columnName, tableName);
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
				Log.Error(ex, DatabaseConstants.ErrorIsTableExists, tablename);
				return false;
			}
		}

		public override void ExecuteVacuum() => this.ExecuteNonQuery("VACUUM");

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				Connection.Dispose();
			base.Dispose(disposing);
		}
	}
}
