using AutoKkutuLib.Database.Path;

namespace AutoKkutuLib.Database.Jobs.DbCheck;
internal class RefreshNodeListJob : DbCheckSubtaskBase
{
	private readonly NodeManager nodeManager;
	public RefreshNodeListJob(NodeManager nodeManager) : base(nodeManager.DbConnection, "Refresh Node List") => this.nodeManager = nodeManager;

	protected override int RunCore()
	{
		try
		{
			nodeManager.LoadNodeLists(Db);
		}
		catch (Exception ex)
		{
			LibLogger.Error<DbCheckJob>(ex, "Failed to refresh node lists");
		}
		return 0;
	}

	public override void BriefResult() { }
}
