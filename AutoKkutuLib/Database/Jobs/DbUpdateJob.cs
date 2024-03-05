using AutoKkutuLib.Database.Helper;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Path;
using System.Globalization;

namespace AutoKkutuLib.Database.Jobs;
public class DbUpdateJob
{
	private readonly DbConnectionBase dbConnection;
	private readonly NodeManager nodeManager;
	private readonly PathFilter specialPathList;

	[Flags]
	public enum DbUpdateCategories
	{
		None = 0,

		/// <summary>
		/// 새로운 단어 추가
		/// </summary>
		Add = 1 << 0,


		/// <summary>
		/// 지원되지 않는 단어 제거
		/// </summary>
		Remove = 1 << 1,

		/// <summary>
		/// 새로운 한방 노드 추가
		/// </summary>
		AddEnd = 1 << 2
	}

	public DbUpdateJob(DbConnectionBase dbConnection, NodeManager nodeManager, PathFilter specialPathList)
	{
		this.dbConnection = dbConnection;
		this.nodeManager = nodeManager;
		this.specialPathList = specialPathList;
	}

	public DbUpdateJob(NodeManager nodeManager, PathFilter specialPathList) : this(nodeManager.DbConnection, nodeManager, specialPathList)
	{
	}

	public string Execute(DbUpdateCategories categories)
	{
		LibLogger.Debug<DbUpdateJob>(I18n.PathFinder_AutoDBUpdate);
		int AddQueueCount = 0,
			RemoveQueueCount = 0,
			EndNodeQueueCount = 0,
			AddSuccessfulCount = 0,
			RemoveSuccessfulCount = 0,
			EndNodeSuccessfulCount = 0;
		if (categories.HasFlag(DbUpdateCategories.Add))
		{
			AddQueueCount = specialPathList.NewPaths.Count;
			LibLogger.Debug<DbUpdateJob>(I18n.PathFinder_AutoDBUpdate_New, AddQueueCount);
			AddSuccessfulCount = AddNewPaths(CopyPathList(specialPathList.NewPaths));
		}

		if (categories.HasFlag(DbUpdateCategories.Remove))
		{
			RemoveQueueCount = specialPathList.InexistentPaths.Count;
			LibLogger.Info<DbUpdateJob>(I18n.PathFinder_AutoDBUpdate_Remove, RemoveQueueCount);
			RemoveSuccessfulCount = RemoveInexistentPaths(CopyPathList(specialPathList.InexistentPaths));
		}

		if (categories.HasFlag(DbUpdateCategories.AddEnd))
		{
			EndNodeQueueCount = specialPathList.NewEndPaths.Count;
			EndNodeSuccessfulCount = AddEndNodes(CopyPathList(specialPathList.NewEndPaths));
		}

		var result = string.Format(CultureInfo.CurrentCulture, I18n.PathFinder_AutoDBUpdate_Result, AddSuccessfulCount, AddQueueCount, RemoveSuccessfulCount, RemoveQueueCount, EndNodeQueueCount, EndNodeSuccessfulCount);
		LibLogger.Info<DbUpdateJob>(I18n.PathFinder_AutoDBUpdate_Finished, result);
		return result;
	}

	private static ICollection<T> CopyPathList<T>(ICollection<T> pathList)
	{
		var copy = new List<T>(pathList);
		pathList.Clear();
		return copy;
	}

	private int AddNewPaths(ICollection<string> paths)
	{
		var count = 0;
		var query = dbConnection.Query.AddWord();
		var recalc = new WordFlagsRecalculator(nodeManager, null!); // fixme: 내가 보기에 분명 언젠가 이거 null 제거하는 것 잊어먹고 NPE 발생시키는 날이 온다 진짜
		foreach (var word in paths)
		{
			var flags = recalc.GetWordFlags(word);

			try
			{
				LibLogger.Debug<DbUpdateJob>(I18n.PathFinder_AddPath, word, flags);
				if (query.Execute(word, flags))
				{
					LibLogger.Info<DbUpdateJob>(I18n.PathFinder_AddPath_Success, word);
					count++;
				}
			}
			catch (Exception ex)
			{
				LibLogger.Error<DbUpdateJob>(ex, I18n.PathFinder_AddPath_Failed, word);
			}
		}

		return count;
	}

	private int RemoveInexistentPaths(ICollection<string> paths)
	{
		var count = 0;
		var query = dbConnection.Query.DeleteWord();
		foreach (var word in paths)
		{
			try
			{
				count += query.Execute(word);
			}
			catch (Exception ex)
			{
				LibLogger.Error<DbUpdateJob>(ex, I18n.PathFinder_RemoveInexistent_Failed, word);
			}
		}
		return count;
	}

	private int AddEndNodes(ICollection<(GameMode, string)> nodeMap)
	{
		if (nodeMap.Count == 0)
			return 0;

		var dict = new Dictionary<GameMode, ICollection<string>>();
		foreach ((var gm, var nodeList) in nodeMap)
		{
			if (!dict.TryGetValue(gm, out var list))
				dict.Add(gm, list = new List<string>());
			list.Add(nodeList);
		}

		var count = 0;
		foreach ((var gm, var nodeList) in dict)
		{
			var query = dbConnection.Query.AddNode(gm.GetEndWordListTableName());
			foreach (var node in nodeList)
			{
				try
				{
					LibLogger.Debug<DbUpdateJob>("Trying to add {0} end-node {1} to the database.", gm, node);
					if (query.Execute(node))
					{
						LibLogger.Info<DbUpdateJob>("Added {0} end-node {1} to the database.", gm, node);
						count++;
					}
				}
				catch (Exception ex)
				{
					LibLogger.Error<DbUpdateJob>(ex, "Error adding {0} end-node: {1}.", gm, node);
				}
			}
		}

		LibLogger.Info<DbUpdateJob>("Added {0} end-nodes.", count);

		return count;
	}
}
