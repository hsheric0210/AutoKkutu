namespace AutoKkutuLib.Database.Jobs.DbCheck;
internal class DeduplicateWordTableJob : DbCheckSubtaskBase
{
	private int removed;

	public DeduplicateWordTableJob(DbConnectionBase db) : base(db, "Deduplicate Word Table")
	{
	}

	protected override int RunCore()
	{
		try
		{
			removed = Db.Query.Deduplicate().Execute();
			LibLogger.Debug(CheckName, "Removed {0} duplicate word entries.", removed);
		}
		catch (Exception ex)
		{
			LibLogger.Error(CheckName, ex, "Word table deduplication failed");
		}

		return removed;
	}

	public override void BriefResult() => LibLogger.Info(CheckName, "Removed {0} duplicate word entries.", removed);
}
