namespace AutoKkutuLib.Database.Sql.Migrations;
internal class ChangeWordListKkutuWordIndexColumnType : MigrationBase
{
	public override string Name => "Change kkutu_word_index column type from CHAR(2) to VARCHAR(2)";
	public override DateTime Date => new(2022, 4, 24); // commit 534cc9d437dfd462c2d33bbfd2b4337b222c2a43

	public ChangeWordListKkutuWordIndexColumnType(DbConnectionBase dbConnetion) : base(dbConnetion)
	{
	}

	public override bool ConditionMet()
	{
		var kkutuindextype = Db.Query.GetColumnType(DatabaseConstants.WordTableName, DatabaseConstants.KkutuWordIndexColumnName).Execute();
		return kkutuindextype != null && (kkutuindextype.Equals("CHAR(2)", StringComparison.OrdinalIgnoreCase) || kkutuindextype.Equals("character", StringComparison.OrdinalIgnoreCase));
	}

	public override void Execute()
	{
		Db.Query.ChangeWordListColumnType(DatabaseConstants.WordTableName, DatabaseConstants.KkutuWordIndexColumnName, newType: "VARCHAR(2)").Execute();
		LibLogger.Warn<ChangeWordListKkutuWordIndexColumnType>($"Changed type of '{DatabaseConstants.KkutuWordIndexColumnName}' from CHAR(2) to VARCHAR(2).");
	}
}
