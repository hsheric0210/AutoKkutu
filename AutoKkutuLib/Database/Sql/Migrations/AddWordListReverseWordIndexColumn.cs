namespace AutoKkutuLib.Database.Sql.Migrations;
internal class AddWordListReverseWordIndexColumn : MigrationBase
{
	public override string Name => "Add reverseWordIndex column to word_list";
	public override DateTime Date => new(2022, 4, 12); // commit efb8444c2b84fe188f95fd89dae2f7e0e263be1c
	public AddWordListReverseWordIndexColumn(DbConnectionBase dbConnetion) : base(dbConnetion)
	{
	}

	public override bool ConditionMet() => !Db.Query.IsColumnExists(DatabaseConstants.WordTableName, DatabaseConstants.ReverseWordIndexColumnName).Execute();
	public override void Execute()
	{
		Db.Query.AddColumn(DatabaseConstants.WordTableName, DatabaseConstants.ReverseWordIndexColumnName, "CHAR(1) NOT NULL DEFAULT ' '").Execute();
		LibLogger.Warn<AddWordListReverseWordIndexColumn>($"Added {DatabaseConstants.ReverseWordIndexColumnName} column.");
	}
}
