using AutoKkutuLib.Constants;
using AutoKkutuLib.Database;
using Serilog;

namespace AutoKkutuLib.Modules.Path;
public static class NodeBatchJobExtension
{
	public static void BatchAddNode(this NodeManager nodeManager, string content, NodeTypes type)
	{
		if (string.IsNullOrWhiteSpace(content))
			return;

		var NodeList = content.Trim().Split(Environment.NewLine.ToCharArray());

		var SuccessCount = 0;
		var DuplicateCount = 0;
		var FailedCount = 0;

		new DatabaseImportEventArgs("Batch Add Node").TriggerDatabaseImportStart();

		Log.Information("{0} elements queued.", NodeList.Length);
		foreach (var node in NodeList)
		{
			if (string.IsNullOrWhiteSpace(node))
				continue;

			try
			{
				if (nodeManager.AddNode(node, type) > 0)
				{
					Log.Information("Successfully add node {node}!", node[0]);
					SuccessCount++;
				}
				else
				{
					Log.Warning("{node} already exists.", node[0]);
					DuplicateCount++;
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to add node {node}!", node[0]);
				FailedCount++;
			}
		}

		var message = $"{SuccessCount} succeed / {DuplicateCount} duplicated / {FailedCount} failed";
		Log.Information("Database Operation Complete: {0}", message);
		new DatabaseImportEventArgs("Batch Add Node", message).TriggerDatabaseImportDone();
	}

	public static void BatchRemoveNode(this NodeManager nodeManager, string content, NodeTypes type)
	{
		if (string.IsNullOrWhiteSpace(content))
			return;

		var NodeList = content.Trim().Split(Environment.NewLine.ToCharArray());

		var SuccessCount = 0;
		var FailedCount = 0;

		new DatabaseImportEventArgs("Batch Remove Node").TriggerDatabaseImportStart();

		Log.Information("{0} elements queued.", NodeList.Length);
		foreach (var node in NodeList)
		{
			if (string.IsNullOrWhiteSpace(node))
				continue;

			try
			{
				SuccessCount += nodeManager.DeleteNode(node, type);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to remove node {node}!", node[0]);
				FailedCount++;
			}
		}

		var message = $"{SuccessCount} succeed / {FailedCount} failed";
		Log.Information("Database Operation Complete: {0}", message);
		new DatabaseImportEventArgs("Batch Remove Node", message).TriggerDatabaseImportDone();
	}
}
