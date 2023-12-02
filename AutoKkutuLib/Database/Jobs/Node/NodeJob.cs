namespace AutoKkutuLib.Database.Jobs.Node;

public abstract class NodeJob
{
	protected DbConnectionBase DbConnection { get; }

	protected NodeJob(DbConnectionBase dbConnection) => DbConnection = dbConnection;
	public abstract void Execute(string node);
}
