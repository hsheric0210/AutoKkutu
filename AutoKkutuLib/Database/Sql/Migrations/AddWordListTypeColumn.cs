namespace AutoKkutuLib.Database.Sql.Migrations;
internal class AddWordListTypeColumn : MigrationBase
{
	public override string Name => "Add type column to word_list";
	public override DateTime Date => new(2023, 7, 25);

	public AddWordListTypeColumn(DbConnectionBase dbConnetion) : base(dbConnetion)
	{
	}

	public override bool ConditionMet() => !Db.Query.IsColumnExists(DatabaseConstants.WordTableName, DatabaseConstants.TypeColumnName).Execute();
	public override void Execute()
	{
		Db.Query.AddColumn(DatabaseConstants.WordTableName, DatabaseConstants.TypeColumnName, "INT NOT NULL DEFAULT 0").Execute();
		LibLogger.Warn<AddWordListTypeColumn>("Added type column.");
	}
}
