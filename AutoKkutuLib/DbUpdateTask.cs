using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Extension;
using AutoKkutuLib.Node;
using AutoKkutuLib.Path;
using Serilog;
using System.Globalization;

namespace AutoKkutuLib;
public class DbUpdateTask
{
	private readonly AbstractDatabaseConnection dbConnection;
	private readonly NodeManager nodeManager;
	private readonly SpecialPathList specialPathList;

	public DbUpdateTask(AbstractDatabaseConnection dbConnection, NodeManager nodeManager, SpecialPathList specialPathList)
	{
		this.dbConnection = dbConnection;
		this.nodeManager = nodeManager;
		this.specialPathList = specialPathList;
	}

	public DbUpdateTask(NodeManager nodeManager, SpecialPathList specialPathList) : this(nodeManager.DbConnection, nodeManager, specialPathList)
	{
	}

	public string Execute()
	{
		// fixme: this check should be performed by caller, not here.
		//if (!AutoKkutuMain.Configuration.AutoDBUpdateEnabled)
		//	return null;

		Log.Debug(I18n.PathFinder_AutoDBUpdate);
		try
		{
			specialPathList.Lock.EnterUpgradeableReadLock();
			var AddQueueCount = specialPathList.NewPaths.Count;
			var RemoveQueueCount = specialPathList.InexistentPaths.Count;
			if (AddQueueCount + RemoveQueueCount == 0)
				Log.Warning(I18n.PathFinder_AutoDBUpdate_Empty);
			else
			{
				Log.Debug(I18n.PathFinder_AutoDBUpdate_New, AddQueueCount);
				var AddSuccessfulCount = AddNewPaths(CopyPathList(specialPathList.NewPaths));

				Log.Information(I18n.PathFinder_AutoDBUpdate_Remove, RemoveQueueCount);

				var RemoveSuccessfulCount = RemoveInexistentPaths(CopyPathList(specialPathList.InexistentPaths));
				var result = string.Format(CultureInfo.CurrentCulture, I18n.PathFinder_AutoDBUpdate_Result, AddSuccessfulCount, AddQueueCount, RemoveSuccessfulCount, RemoveQueueCount);

				Log.Information(I18n.PathFinder_AutoDBUpdate_Finished, result);
				return result;
			}
		}
		finally
		{
			specialPathList.Lock.ExitUpgradeableReadLock();
		}

		return null;
	}

	private ICollection<string> CopyPathList(ICollection<string> pathList)
	{
		try
		{
			var copy = new List<string>(pathList);
			specialPathList.Lock.EnterWriteLock();
			pathList.Clear();
			return copy;
		}
		finally
		{
			specialPathList.Lock.ExitWriteLock();
		}
	}

	private int AddNewPaths(ICollection<string> paths)
	{
		var count = 0;
		foreach (var word in paths)
		{
			WordFlags flags = nodeManager.CalcWordFlags(word);

			try
			{
				Log.Debug(I18n.PathFinder_AddPath, word, flags);
				if (dbConnection.AddWord(word, flags))
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
		foreach (var word in paths)
		{
			try
			{
				count += dbConnection.DeleteWord(word);
			}
			catch (Exception ex)
			{
				Log.Error(ex, I18n.PathFinder_RemoveInexistent_Failed, word);
			}
		}

		return count;
	}
}
