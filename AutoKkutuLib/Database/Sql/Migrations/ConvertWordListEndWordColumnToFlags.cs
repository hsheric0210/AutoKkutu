using AutoKkutuLib.Database.Sql;
using Dapper;

namespace AutoKkutuLib.Database.Sql.Migrations;
internal class ConvertWordListEndWordColumnToFlags : MigrationBase
{
	public override string Name => "Replace is_endword with flags column in word_list";
	public override DateTime Date => new(2022, 4, 17); // commit 9d4e1fdee0f76bda4acb0a8d7a909f36a11cc5a0
	public ConvertWordListEndWordColumnToFlags(DbConnectionBase dbConnetion) : base(dbConnetion)
	{
	}

	public override bool ConditionMet() => DbConnection.Query.IsColumnExists(DatabaseConstants.WordTableName, DatabaseConstants.IsEndwordColumnName).Execute();
	public override void Execute()
	{
		if (!DbConnection.Query.IsColumnExists(DatabaseConstants.WordTableName, DatabaseConstants.FlagsColumnName).Execute())
		{
			DbConnection.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} ADD COLUMN {DatabaseConstants.FlagsColumnName} SMALLINT NOT NULL DEFAULT 0;");
			DbConnection.Execute($"UPDATE {DatabaseConstants.WordTableName} SET {DatabaseConstants.FlagsColumnName} = CAST({DatabaseConstants.IsEndwordColumnName} AS SMALLINT);");
			LibLogger.Warn<ConvertWordListEndWordColumnToFlags>($"Converted '{DatabaseConstants.IsEndwordColumnName}' into {DatabaseConstants.FlagsColumnName} column.");
		}

		DbConnection.Query.DropWordListColumn(DatabaseConstants.IsEndwordColumnName).Execute();
		LibLogger.Warn<ConvertWordListEndWordColumnToFlags>($"Dropped {DatabaseConstants.IsEndwordColumnName} column as it is no longer used.");
	}
}
