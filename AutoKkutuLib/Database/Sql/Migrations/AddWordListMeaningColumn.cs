namespace AutoKkutuLib.Database.Sql.Migrations;
internal class AddWordListMeaningColumn : MigrationBase
{
	public override string Name => "Add meaning column to word_list";
	public override DateTime Date => new(2023, 7, 25);

	public AddWordListMeaningColumn(DbConnectionBase dbConnetion) : base(dbConnetion)
	{
	}

	public override bool ConditionMet() => !Db.Query.IsColumnExists(DatabaseConstants.WordTableName, DatabaseConstants.MeaningColumnName).Execute();
	public override void Execute()
	{
		Db.Query.AddColumn(DatabaseConstants.WordTableName, DatabaseConstants.MeaningColumnName, "TEXT NOT NULL DEFAULT ''").Execute();
		LibLogger.Warn<AddWordListMeaningColumn>("Added meaning column.");
	}
}
