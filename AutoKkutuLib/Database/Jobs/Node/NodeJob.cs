namespace AutoKkutuLib.Database.Jobs.Node;

public abstract class NodeJob
{
	protected AbstractDatabaseConnection DbConnection { get; }

	protected NodeJob(AbstractDatabaseConnection dbConnection) => DbConnection = dbConnection;
}
