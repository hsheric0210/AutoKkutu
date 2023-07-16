using Serilog;

namespace AutoKkutuLib.Database.Jobs.Node;
public static class NodeBatchJobExtension
{
	public static NodeCount? BatchAddNode(this AbstractDatabaseConnection dbConnection, string nodes, NodeTypes nodeTypes)
	{
		if (string.IsNullOrWhiteSpace(nodes))
			return null;

		var nodeList = nodes.Trim().Split(Environment.NewLine.ToCharArray());
		var job = new NodeAdditionJob(dbConnection, nodeTypes);

		Log.Information("Queued {0} elements to add.", nodeList.Length);
		foreach (var node in nodeList)
		{
			if (!string.IsNullOrWhiteSpace(node))
			{
				Log.Information("Processing {0}.", node);
				job.Add(node);
			}
		}

		Log.Information("{0} nodes affected. {1} errors occurred.", job.Result.TotalCount, job.Result.TotalError);
		return job.Result;
	}

	public static NodeCount? BatchRemoveNode(this AbstractDatabaseConnection dbConnection, string nodes, NodeTypes nodeTypes)
	{
		if (string.IsNullOrWhiteSpace(nodes))
			return null;

		var nodeList = nodes.Trim().Split(Environment.NewLine.ToCharArray());
		var job = new NodeDeletionJob(dbConnection, nodeTypes);

		Log.Information("Queued {0} elements to remove.", nodeList.Length);
		foreach (var node in nodeList)
		{
			if (!string.IsNullOrWhiteSpace(node))
			{
				Log.Information("Processing {0}.", node);
				job.Delete(node);
			}
		}

		Log.Information("{0} nodes affected. {1} errors occurred.", job.Result.TotalCount, job.Result.TotalError);
		return job.Result;
	}
}
