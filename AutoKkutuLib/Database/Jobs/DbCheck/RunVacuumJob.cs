namespace AutoKkutuLib.Database.Jobs.DbCheck;
internal class RunVacuumJob : DbCheckSubtaskBase
{
	public RunVacuumJob(DbConnectionBase db) : base(db, "Run Vacuum")
	{
	}

	protected override int RunCore()
	{
		Db.Query.Vacuum().Execute();
		return 0;
	}

	public override void BriefResult() { }
}
