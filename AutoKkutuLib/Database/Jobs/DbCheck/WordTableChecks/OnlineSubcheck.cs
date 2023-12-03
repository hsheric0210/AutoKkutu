using AutoKkutuLib.Browser;
using AutoKkutuLib.Database.Jobs.Word;

namespace AutoKkutuLib.Database.Jobs.DbCheck.WordTableChecks;
internal class OnlineSubcheck : IWordTableSubcheck
{
	private readonly BrowserBase browser;
	private readonly IList<string> inexistentList = new List<string>();

	public string SubcheckName => "Check Online Dictionary";

	public OnlineSubcheck(BrowserBase browser) => this.browser = browser;

	public bool Verify(WordModel entry)
	{
		if (browser != null && browser?.VerifyWordOnline(entry.Word.Trim()) == false)
		{
			inexistentList.Add(entry.Word);
			return true; // Prevent further checks
		}

		return false;
	}

	public int Fix(DbConnectionBase db)
	{
		var job = new BatchWordDeletionJob(db, false);
		return job.Execute(inexistentList).TotalCount;
	}
}
