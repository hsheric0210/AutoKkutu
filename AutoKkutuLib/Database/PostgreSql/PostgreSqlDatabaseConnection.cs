using Serilog;
using Npgsql;
using Dapper;

namespace AutoKkutuLib.Database.PostgreSql;

public partial class PostgreSqlDatabaseConnection : AbstractDatabaseConnection
{
	public PostgreSqlDatabaseConnection(NpgsqlConnection connection)
	{
		Initialize(connection);
	}

	public override void AddSequenceColumnToWordList() => this.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} ADD COLUMN seq SERIAL PRIMARY KEY");

	public override void ChangeWordListColumnType(string tableName, string columnName, string newType) => this.Execute($"ALTER TABLE {tableName} ALTER COLUMN {columnName} TYPE {newType}");

	public override void DropWordListColumn(string columnName) => this.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} DROP COLUMN {columnName}");

	public override string? GetColumnType(string tableName, string columnName)
	{
		tableName ??= DatabaseConstants.WordTableName;
		try
		{
			return this.ExecuteScalar<string>("SELECT data_type FROM information_schema.columns WHERE table_name=@TableName AND column_name=@ColumnName;", new
			{
				TableName = tableName,
				ColumnName = columnName
			});
		}
		catch (Exception ex)
		{
			Log.Error(ex, DatabaseConstants.ErrorGetColumnType, columnName, tableName);
		}
		return null;
	}

	public override string GetWordPriorityFuncName() => "__AutoKkutu_Rearrange";

	public override string GetMissionWordPriorityFuncName() => "__AutoKkutu_RearrangeMission";

	public override string GetWordListColumnOptions() => "seq SERIAL PRIMARY KEY, word CHAR VARYING(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

	public override bool IsColumnExists(string tableName, string columnName)
	{
		tableName ??= DatabaseConstants.WordTableName;
		try
		{
			return this.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.columns WHERE table_name=@TableName AND column_name=@ColumnName;", new
			{
				TableName = tableName,
				ColumnName = columnName
			}) > 0;
		}
		catch (Exception ex)
		{
			Log.Error(ex, DatabaseConstants.ErrorIsColumnExists, columnName, tableName);
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
			Log.Error(ex, DatabaseConstants.ErrorIsTableExists, tableName);
			return false;
		}
	}

	public override void ExecuteVacuum() => this.Execute("VACUUM;");
}
