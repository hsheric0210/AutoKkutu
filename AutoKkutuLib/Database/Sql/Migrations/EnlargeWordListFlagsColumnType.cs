namespace AutoKkutuLib.Database.Sql.Migrations;
internal class EnlargeWordListFlagsColumnType : MigrationBase
{
	public override string Name => "Change the type of flags column in word_list from SMALLINT to INT";
	public override DateTime Date => new(2023, 7, 25); // commit 9d4e1fdee0f76bda4acb0a8d7a909f36a11cc5a0
	public EnlargeWordListFlagsColumnType(DbConnectionBase dbConnetion) : base(dbConnetion)
	{
	}

	public override bool ConditionMet()
	{
		var kkutuindextype = Db.Query.GetColumnType(DatabaseConstants.WordTableName, DatabaseConstants.FlagsColumnName).Execute();
		return kkutuindextype?.Equals("SMALLINT", StringComparison.OrdinalIgnoreCase) == true;
	}

	public override void Execute()
	{
		Db.Query.ChangeWordListColumnType(DatabaseConstants.WordTableName, DatabaseConstants.FlagsColumnName, newType: "INT").Execute();
		LibLogger.Warn<ChangeWordListKkutuWordIndexColumnType>($"Changed type of '{DatabaseConstants.FlagsColumnName}' from SMALLINT to INT.");
	}
}
