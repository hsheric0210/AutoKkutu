namespace AutoKkutuLib.Database.Jobs.Node;
public static class NodeBatchJobExtension
{
	public static NodeCount? BatchAddNode(this DbConnectionBase dbConnection, string nodes, NodeTypes nodeTypes)
	{
		if (string.IsNullOrWhiteSpace(nodes))
			return null;

		var nodeList = nodes.Trim().Split(Environment.NewLine.ToCharArray());
		var job = new NodeAdditionJob(dbConnection, nodeTypes);

		LibLogger.Info(nameof(NodeBatchJobExtension), "Queued {0} elements to add.", nodeList.Length);
		foreach (var node in nodeList)
		{
			if (!string.IsNullOrWhiteSpace(node))
			{
				LibLogger.Info(nameof(NodeBatchJobExtension), "Processing {0}.", node);
				job.Add(node);
			}
		}

		LibLogger.Info(nameof(NodeBatchJobExtension), "{0} nodes affected. {1} errors occurred.", job.Result.TotalCount, job.Result.TotalError);
		return job.Result;
	}

	public static NodeCount? BatchRemoveNode(this DbConnectionBase dbConnection, string nodes, NodeTypes nodeTypes)
	{
		if (string.IsNullOrWhiteSpace(nodes))
			return null;

		var nodeList = nodes.Trim().Split(Environment.NewLine.ToCharArray());
		var job = new NodeDeletionJob(dbConnection, nodeTypes);

		LibLogger.Info(nameof(NodeBatchJobExtension), "Queued {0} elements to remove.", nodeList.Length);
		foreach (var node in nodeList)
		{
			if (!string.IsNullOrWhiteSpace(node))
			{
				LibLogger.Info(nameof(NodeBatchJobExtension), "Processing {0}.", node);
				job.Delete(node);
			}
		}

		LibLogger.Info(nameof(NodeBatchJobExtension), "{0} nodes affected. {1} errors occurred.", job.Result.TotalCount, job.Result.TotalError);
		return job.Result;
	}
}
