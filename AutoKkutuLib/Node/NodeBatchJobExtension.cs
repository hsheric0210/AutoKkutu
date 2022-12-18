using AutoKkutuLib.Database;
using Serilog;

namespace AutoKkutuLib.Node;
public static class NodeBatchJobExtension
{
	public static NodeCount? BatchAddNode(this AbstractDatabaseConnection dbConnection, string content, NodeTypes nodeTypes)
	{
		if (string.IsNullOrWhiteSpace(content))
			return null;

		var nodeList = content.Trim().Split(Environment.NewLine.ToCharArray());
		var job = new NodeAdditionJob(dbConnection, nodeTypes);

		Log.Information("{0} elements queued.", nodeList.Length);
		foreach (var node in nodeList)
		{
			if (!string.IsNullOrWhiteSpace(node))
				job.Add(node);
		}

		return job.Result;
	}

	public static NodeCount? BatchRemoveNode(this AbstractDatabaseConnection dbConnection, string content, NodeTypes nodeTypes)
	{
		if (string.IsNullOrWhiteSpace(content))
			return null;

		var nodeList = content.Trim().Split(Environment.NewLine.ToCharArray());
		var job = new NodeDeletionJob(dbConnection, nodeTypes);

		Log.Information("{0} elements queued.", nodeList.Length);
		foreach (var node in nodeList)
		{
			if (!string.IsNullOrWhiteSpace(node))
				job.Delete(node);
		}

		return job.Result;
	}
}
