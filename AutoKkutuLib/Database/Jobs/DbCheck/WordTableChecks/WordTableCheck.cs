using AutoKkutuLib.Database.Helper;
using AutoKkutuLib.Extension;
using Dapper;

namespace AutoKkutuLib.Database.Jobs.DbCheck.WordTableChecks;
internal class WordTableCheck : DbCheckSubtaskBase
{
	private readonly IList<IWordTableSubcheck> subchecks;
	private readonly IDictionary<string, int> summary = new Dictionary<string, int>();

	public WordTableCheck(NodeManager nodeManager) : base(nodeManager.DbConnection, "Word table check")
	{
		subchecks = new List<IWordTableSubcheck>()
		{
			new InvalidWordSubcheck(),
			// new OnlineSubcheck(null), // not implemented yet
			new WordIndexSubcheck(DatabaseConstants.WordIndexColumnName, (WordModel e) => e.WordIndex, WordToNodeExtension.GetLaFHeadNode),
			new WordIndexSubcheck(DatabaseConstants.ReverseWordIndexColumnName, (WordModel e) => e.ReverseWordIndex, WordToNodeExtension.GetFaLHeadNode),
			new WordIndexSubcheck(DatabaseConstants.KkutuWordIndexColumnName, (WordModel e) => e.KkutuWordIndex, WordToNodeExtension.GetKkutuHeadNode),
			new WordFlagsSubcheck(new WordFlagsRecalculator(nodeManager, null!)), // fixme: add themeManager field
			new WordChoseongSubcheck()
		};
	}

	protected override int RunCore()
	{
		foreach (var entry in Db.Query<WordModel>($"SELECT * FROM {DatabaseConstants.WordTableName} ORDER BY({DatabaseConstants.WordColumnName}) DESC"))
		{
			foreach (var subcheck in subchecks)
			{
				if (subcheck.Verify(entry))
					break; // if 'Verify()' returns true, skip all the further checks for that word entry
			}
		}

		var count = 0;
		var transaction = Db.BeginTransaction(); // Speed optimization
		foreach (var subcheck in subchecks)
		{
			var innerCount = subcheck.Fix(Db);
			summary[subcheck.SubcheckName] = innerCount;
			count += innerCount;
		}
		transaction.Commit();
		return count;
	}

	public override void BriefResult()
	{
		foreach (var entry in summary)
			LibLogger.Info(CheckName, "Subcheck {0}: {1} entries affected.", entry.Key, entry.Value);
	}
}
