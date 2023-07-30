namespace AutoKkutuLib.Database.Sql.Migrations;
internal class AddWordListThemeColumns : MigrationBase
{
	public override string Name => "Add theme columns to word_list";
	public override DateTime Date => new(2023, 7, 25);

	public AddWordListThemeColumns(DbConnectionBase dbConnetion) : base(dbConnetion)
	{
	}

	public override bool ConditionMet() => !Db.Query.IsColumnExists(DatabaseConstants.WordTableName, DatabaseConstants.ThemeColumn1Name).Execute();
	public override void Execute()
	{
		// 64-bit * 4 -> total 256-bit bitmask
		Db.Query.AddColumn(DatabaseConstants.WordTableName, DatabaseConstants.ThemeColumn1Name, "BIGINT NOT NULL DEFAULT 0").Execute();
		Db.Query.AddColumn(DatabaseConstants.WordTableName, DatabaseConstants.ThemeColumn2Name, "BIGINT NOT NULL DEFAULT 0").Execute();
		Db.Query.AddColumn(DatabaseConstants.WordTableName, DatabaseConstants.ThemeColumn3Name, "BIGINT NOT NULL DEFAULT 0").Execute();
		Db.Query.AddColumn(DatabaseConstants.WordTableName, DatabaseConstants.ThemeColumn4Name, "BIGINT NOT NULL DEFAULT 0").Execute();
		LibLogger.Warn<AddWordListThemeColumns>("Added theme columns.");
	}
}
