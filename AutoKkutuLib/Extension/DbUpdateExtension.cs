using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Extension;
using AutoKkutuLib.Path;
using Serilog;
using System.Globalization;

namespace AutoKkutuLib.Extension;
public static class DbUpdateExtension
{
	public static string? UpdateDatabase(this NodeManager nodeManager, SpecialPathList pathList)
	{
		// fixme: this check should be performed by caller, not here.
		//if (!AutoKkutuMain.Configuration.AutoDBUpdateEnabled)
		//	return null;

		Log.Debug(I18n.PathFinder_AutoDBUpdate);
		try
		{
			pathList.Lock.EnterUpgradeableReadLock();
			var AddQueueCount = pathList.NewPaths.Count;
			var RemoveQueueCount = pathList.InexistentPaths.Count;
			if (AddQueueCount + RemoveQueueCount == 0)
			{
				Log.Warning(I18n.PathFinder_AutoDBUpdate_Empty);
			}
			else
			{
				Log.Debug(I18n.PathFinder_AutoDBUpdate_New, AddQueueCount);
				var AddSuccessfulCount = AddNewPaths(nodeManager, CopyPathList(pathList.NewPaths, pathList.Lock));

				Log.Information(I18n.PathFinder_AutoDBUpdate_Remove, RemoveQueueCount);

				var RemoveSuccessfulCount = RemoveInexistentPaths(nodeManager.DbConnection, CopyPathList(pathList.InexistentPaths, pathList.Lock));
				var result = string.Format(CultureInfo.CurrentCulture, I18n.PathFinder_AutoDBUpdate_Result, AddSuccessfulCount, AddQueueCount, RemoveSuccessfulCount, RemoveQueueCount);

				Log.Information(I18n.PathFinder_AutoDBUpdate_Finished, result);
				return result;
			}
		}
		finally
		{
			pathList.Lock.ExitUpgradeableReadLock();
		}

		return null;
	}

	private static ICollection<string> CopyPathList(this ICollection<string> pathList, ReaderWriterLockSlim rwLock)
	{
		try
		{
			var copy = new List<string>(pathList);
			rwLock.EnterWriteLock();
			pathList.Clear();
			return copy;
		}
		finally
		{
			rwLock.ExitWriteLock();
		}
	}

	private static int AddNewPaths(NodeManager nodeManager, ICollection<string> paths)
	{
		var count = 0;
		foreach (var word in paths)
		{
			WordFlags flags = nodeManager.CalcWordFlags(word);

			try
			{
				Log.Debug(I18n.PathFinder_AddPath, word, flags);
				if (nodeManager.DbConnection.AddWord(word, flags))
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

	private static int RemoveInexistentPaths(AbstractDatabaseConnection dbConnection, ICollection<string> paths)
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
