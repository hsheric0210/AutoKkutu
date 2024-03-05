using AutoKkutuLib.Database.Helper;

namespace AutoKkutuLib.Database.Jobs.DbCheck;
internal class RefreshNodeListJob : DbCheckSubtaskBase
{
	private readonly NodeManager nodeManager;
	public RefreshNodeListJob(NodeManager nodeManager) : base(nodeManager.DbConnection, "Refresh Node List") => this.nodeManager = nodeManager;

	protected override int RunCore()
	{
		nodeManager.LoadNodeLists(Db);
		return 0;
	}

	public override void BriefResult() { }
}
