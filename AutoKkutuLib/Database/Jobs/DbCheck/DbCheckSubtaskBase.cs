using System.Diagnostics;

namespace AutoKkutuLib.Database.Jobs.DbCheck;
internal abstract class DbCheckSubtaskBase
{
	protected DbConnectionBase Db { get; }
	protected string CheckName { get; }
	protected DbCheckSubtaskBase(DbConnectionBase db, string checkName)
	{
		Db = db;
		CheckName = checkName;
	}

	public int Execute()
	{
		LibLogger.Info(CheckName, "Starting: {0}", CheckName);

		var watch = new Stopwatch();
		watch.Start();
		var count = RunCore();
		watch.Stop();

		LibLogger.Info(CheckName, "Finished: {0} (Took {1}ms)", CheckName, watch.ElapsedMilliseconds);

		return count;
	}

	protected abstract int RunCore();
	public abstract void BriefResult();
}
