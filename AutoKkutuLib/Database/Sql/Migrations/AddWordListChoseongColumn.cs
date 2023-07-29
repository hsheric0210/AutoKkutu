namespace AutoKkutuLib.Database.Sql.Migrations;
internal class AddWordListChoseongColumn : MigrationBase
{
	public override string Name => "Add choseong column to word_list";
	public override DateTime Date => new(2023, 7, 24);

	public AddWordListChoseongColumn(DbConnectionBase dbConnetion) : base(dbConnetion)
	{
	}

	public override bool ConditionMet() => !Db.Query.IsColumnExists(DatabaseConstants.WordTableName, DatabaseConstants.ChoseongColumnName).Execute();
	public override void Execute()
	{
		Db.Query.AddColumn(DatabaseConstants.WordTableName, DatabaseConstants.ChoseongColumnName, "VARCHAR(256) NOT NULL DEFAULT ''").Execute();
		LibLogger.Warn<AddWordListChoseongColumn>("Added choseong column.");
	}
}
