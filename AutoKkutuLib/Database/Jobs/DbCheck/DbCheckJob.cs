using AutoKkutuLib.Browser;
using AutoKkutuLib.Database.Helper;
using AutoKkutuLib.Database.Jobs.DbCheck.WordTableChecks;
using Dapper;

namespace AutoKkutuLib.Database.Jobs.DbCheck;

public class DbCheckJob
{
	private readonly NodeManager nodeManager;
	private DbConnectionBase Db => nodeManager.DbConnection;

	public DbCheckJob(NodeManager nodeManager) => this.nodeManager = nodeManager;

	#region Main check process
	/// <summary>
	/// 데이터베이스의 무결성을 검증하고, 문제를 발견하면 수정합니다.
	/// </summary>
	/// <param name="UseOnlineDB">온라인 검사(끄투 사전을 통한 검사)를 진행하는지의 여부</param>
	public void CheckDB(bool UseOnlineDB, BrowserBase? browser)
	{
		// FIXME: Move to caller
		//if (UseOnlineDB && string.IsNullOrWhiteSpace(JSEvaluator.EvaluateJS("document.getElementById('dict-output').style")))
		//	MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 여십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Warning);
		//	return;

		DatabaseEvents.TriggerDatabaseIntegrityCheckStart();

		Task.Run(() =>
		{
			var jobs = new List<DbCheckSubtaskBase>()
			{
				new DeduplicateWordTableJob(Db),
				new RefreshNodeListJob(nodeManager),
				new InvalidEndNodeCheck(Db),
				new RefreshNodeListJob(nodeManager),
				new WordTableCheck(nodeManager),
				new RunVacuumJob(Db)
			};
			try
			{
				var affected = 0;
				var totalElementCount = Db.ExecuteScalar<int>($"SELECT COUNT(*) FROM {DatabaseConstants.WordTableName}");
				LibLogger.Info<DbCheckJob>("Database has Total {0} elements.", totalElementCount);

				foreach (var job in jobs)
					affected += job.Execute();

				LibLogger.Info<DbCheckJob>("Total {0} problems are solved.", affected);

				new DataBaseIntegrityCheckDoneEventArgs($"{affected} 개의 문제점 수정됨").TriggerDatabaseIntegrityCheckDone();
			}
			catch (Exception ex)
			{
				LibLogger.Error<DbCheckJob>(ex, "Exception while checking database");
			}

			foreach (var job in jobs)
				job.BriefResult();
		});
	}
	#endregion
}
