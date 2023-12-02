namespace AutoKkutuLib.Database.Sql.Migrations;
internal class AddWordListKkutuWordIndexColumn : MigrationBase
{
	public override string Name => "Add kkutuWordIndex column to word_list";
	public override DateTime Date => new(2022, 4, 12); // commit efb8444c2b84fe188f95fd89dae2f7e0e263be1c
	public AddWordListKkutuWordIndexColumn(DbConnectionBase dbConnetion) : base(dbConnetion)
	{
	}

	public override bool ConditionMet() => !Db.Query.IsColumnExists(DatabaseConstants.WordTableName, DatabaseConstants.KkutuWordIndexColumnName).Execute();
	public override void Execute()
	{
		Db.Query.AddColumn(DatabaseConstants.WordTableName, DatabaseConstants.KkutuWordIndexColumnName, "VARCHAR(2) NOT NULL DEFAULT ''").Execute();
		LibLogger.Warn<AddWordListKkutuWordIndexColumn>($"Added {DatabaseConstants.KkutuWordIndexColumnName} column.");
	}
}
