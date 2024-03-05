using AutoKkutuLib.Database.Jobs.Word;
using System.Text.RegularExpressions;

namespace AutoKkutuLib.Database.Jobs.DbCheck.WordTableChecks;
internal class InvalidWordSubcheck : IWordTableSubcheck
{
	private readonly char[] blacklistedChars = new char[] { ' ', ':', ';', '?', '!' };
	private readonly Regex regexMatch = new("[^a-zA-Z0-9ㄱ-ㅎ가-힣]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

	private readonly IList<string> invalidList = new List<string>();

	public string SubcheckName => "Remove Invalid Words";

	private bool IsInvalid(string content)
	{
		if (content.Length <= 1)
			return true;

		var first = content[0];
		if (first is '(' or '{' or '[' or '-' or '.')
			return true;

		var last = content.Last();
		if (last is ')' or '}' or ']')
			return true;

		return blacklistedChars.Any(ch => content.Contains(ch, StringComparison.Ordinal))
			|| regexMatch.Match(content).Success;
	}

	public bool Verify(WordModel entry)
	{
		if (IsInvalid(entry.Word))
		{
			invalidList.Add(entry.Word);
			return true; // Prevent further checks
		}

		return false;
	}

	public int Fix(DbConnectionBase db)
	{
		var job = new BatchWordDeletionJob(db, false);
		return job.Execute(invalidList).TotalCount;
	}
}
