namespace AutoKkutuLib.Database.Sql.Migrations;
internal class AddWordListSequenceColumn : MigrationBase
{
	public override string Name => "Add sequence column to word_list";
	public override DateTime Date => new(2022, 4, 24); // commit 534cc9d437dfd462c2d33bbfd2b4337b222c2a43

	public AddWordListSequenceColumn(DbConnectionBase dbConnetion) : base(dbConnetion)
	{
	}

	public override bool ConditionMet() => !Db.Query.IsColumnExists(DatabaseConstants.WordTableName, DatabaseConstants.SequenceColumnName).Execute();
	public override void Execute()
	{
		Db.Query.AddWordListSequenceColumn().Execute();
		LibLogger.Warn<AddWordListSequenceColumn>("Added sequence column.");
	}
}
