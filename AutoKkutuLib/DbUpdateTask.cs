using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Sql.Query;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Node;
using AutoKkutuLib.Path;
using Serilog;
using System.Globalization;

namespace AutoKkutuLib;
public class DbUpdateTask
{
	private readonly AbstractDatabaseConnection dbConnection;
	private readonly NodeManager nodeManager;
	private readonly PathFilter specialPathList;

	public DbUpdateTask(AbstractDatabaseConnection dbConnection, NodeManager nodeManager, PathFilter specialPathList)
	{
		this.dbConnection = dbConnection;
		this.nodeManager = nodeManager;
		this.specialPathList = specialPathList;
	}

	public DbUpdateTask(NodeManager nodeManager, PathFilter specialPathList) : this(nodeManager.DbConnection, nodeManager, specialPathList)
	{
	}

	public string? Execute()
	{
		Log.Debug(I18n.PathFinder_AutoDBUpdate);
		var AddQueueCount = specialPathList.NewPaths.Count;
		var RemoveQueueCount = specialPathList.InexistentPaths.Count;
		var EndNodeQueueCount = specialPathList.NewEndPaths.Count;
		if (AddQueueCount + RemoveQueueCount == 0)
			Log.Warning(I18n.PathFinder_AutoDBUpdate_Empty);
		else
		{
			Log.Debug(I18n.PathFinder_AutoDBUpdate_New, AddQueueCount);
			var AddSuccessfulCount = AddNewPaths(CopyPathList(specialPathList.NewPaths));

			Log.Information(I18n.PathFinder_AutoDBUpdate_Remove, RemoveQueueCount);

			var RemoveSuccessfulCount = RemoveInexistentPaths(CopyPathList(specialPathList.InexistentPaths));

			var EndNodeSuccessfulCount = AddEndNodes(CopyPathList(specialPathList.NewEndPaths));

			var result = string.Format(CultureInfo.CurrentCulture, I18n.PathFinder_AutoDBUpdate_Result, AddSuccessfulCount, AddQueueCount, RemoveSuccessfulCount, RemoveQueueCount, EndNodeQueueCount, EndNodeSuccessfulCount);

			Log.Information(I18n.PathFinder_AutoDBUpdate_Finished, result);
			return result;
		}

		return null;
	}

	private ICollection<T> CopyPathList<T>(ICollection<T> pathList)
	{
		var copy = new List<T>(pathList);
		pathList.Clear();
		return copy;
	}

	private int AddNewPaths(ICollection<string> paths)
	{
		var count = 0;
		WordAdditionQuery query = dbConnection.Query.AddWord();
		foreach (var word in paths)
		{
			WordFlags flags = nodeManager.CalcWordFlags(word);

			try
			{
				Log.Debug(I18n.PathFinder_AddPath, word, flags);
				if (query.Execute(word, flags))
				{
					Log.Information(I18n.PathFinder_AddPath_Success, word);
					count++;
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, I18n.PathFinder_AddPath_Failed, word);
			}
		}

		return count;
	}

	private int RemoveInexistentPaths(ICollection<string> paths)
	{
		var count = 0;
		WordDeletionQuery query = dbConnection.Query.DeleteWord();
		foreach (var word in paths)
		{
			try
			{
				count += query.Execute(word);
			}
			catch (Exception ex)
			{
				Log.Error(ex, I18n.PathFinder_RemoveInexistent_Failed, word);
			}
		}
		return count;
	}

	private int AddEndNodes(ICollection<(GameMode, string)> nodeMap)
	{
		if (nodeMap.Count == 0)
			return 0;

		var dict = new Dictionary<GameMode, ICollection<string>>();
		foreach ((GameMode gm, var nodeList) in nodeMap)
		{
			if (!dict.TryGetValue(gm, out ICollection<string>? list))
				dict.Add(gm, list = new List<string>());
			list.Add(nodeList);
		}

		var count = 0;
		foreach ((GameMode gm, ICollection<string> nodeList) in dict)
		{
			NodeAdditionQuery query = dbConnection.Query.AddNode(gm.GetEndWordListTableName());
			foreach (var node in nodeList)
			{
				try
				{
					Log.Debug("Trying to add {0} end-node {1} to the database.", gm, node);
					if (query.Execute(node))
					{
						Log.Information("Added {0} end-node {1} to the database.", gm, node);
						count++;
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Error adding {0} end-node: {1}.", gm, node);
				}
			}
		}

		Log.Information("Added {0} end-nodes.", count);

		return count;
	}
}
