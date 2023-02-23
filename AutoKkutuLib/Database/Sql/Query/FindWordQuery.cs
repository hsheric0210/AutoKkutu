using Dapper;
using Serilog;
using System.Globalization;

namespace AutoKkutuLib.Database.Sql.Query;

public class FindWordQuery : SqlQuery<IList<PathObject>>
{
	private readonly GameMode mode;
	private readonly WordPreference preference;
	private readonly int maxCount;

	private readonly WordFlags endWordFlag;
	private readonly WordFlags attackWordFlag;

	public PathFinderParameter? Parameter { get; set; }

	internal FindWordQuery(AbstractDatabaseConnection connection, GameMode mode, WordPreference preference, int maxCount) : base(connection)
	{
		this.mode = mode;
		this.preference = preference;
		SelectFlags(mode, out endWordFlag, out attackWordFlag);
		this.maxCount = maxCount;
	}

	public static void SelectFlags(GameMode mode, out WordFlags endWordFlag, out WordFlags attackWordFlag)
	{
		switch (mode)
		{
			case GameMode.FirstAndLast:
				endWordFlag = WordFlags.ReverseEndWord;
				attackWordFlag = WordFlags.ReverseAttackWord;
				return;

			case GameMode.MiddleAndFirst:
				endWordFlag = WordFlags.MiddleEndWord;
				attackWordFlag = WordFlags.MiddleAttackWord;
				return;

			case GameMode.Kkutu:
				endWordFlag = WordFlags.KkutuEndWord;
				attackWordFlag = WordFlags.KkutuAttackWord;
				return;

			case GameMode.KungKungTta:
				endWordFlag = WordFlags.KKTEndWord;
				attackWordFlag = WordFlags.KKTAttackWord;
				return;
			default:
				endWordFlag = WordFlags.EndWord;
				attackWordFlag = WordFlags.AttackWord;
				return;
		}
	}

	public IList<PathObject> Execute(PathFinderParameter parameter)
	{
		Parameter = parameter;
		return Execute();
	}

	public override IList<PathObject> Execute()
	{
		if (Parameter is null)
			throw new InvalidOperationException(nameof(Parameter) + " not set.");

		FindQuery findQuery = CreateQuery(Parameter);
		var result = new List<PathObject>();
		try
		{
			foreach (WordModel found in Connection.Query<WordModel>(findQuery.Sql, new DynamicParameters(findQuery.Parameters)))
			{
				var wordString = found.Word.Trim();
				WordCategories categories = SetupWordCategories(
					 wordString,
					(WordFlags)found.Flags,
					Parameter.MissionChar,
					out var missionCharCount);
				result.Add(new PathObject(wordString, categories, missionCharCount));
			}
		}
		catch
		{
			Log.Error("Errored query: {sql}", findQuery.Sql);
			throw;
		}

		return result;
	}

	#region Categorization
	private WordCategories SetupWordCategories(
		string word,
		WordFlags wordFlags,
		string missionChar,
		out int missionCharCount)
	{
		WordCategories category = WordCategories.None;
		if (wordFlags.HasFlag(endWordFlag))
			category |= WordCategories.EndWord;
		if (wordFlags.HasFlag(attackWordFlag))
			category |= WordCategories.AttackWord;

		if (!string.IsNullOrWhiteSpace(missionChar))
		{
			missionCharCount = word.Count(c => c == missionChar[0]);
			if (missionCharCount > 0)
				category |= WordCategories.MissionWord;
		}
		else
		{
			missionCharCount = 0;
		}

		return category;
	}
	#endregion

	#region Query generation
	private string GetIndexColumnName(PresentedWord word)
	{
		switch (mode)
		{
			case GameMode.FirstAndLast:
				return DatabaseConstants.ReverseWordIndexColumnName;

			case GameMode.Kkutu: // TODO: 세 글자용 인덱스도 만들기
				if (word.Content.Length == 2 || word.CanSubstitution && word.Substitution!.Length == 2)
					return DatabaseConstants.KkutuWordIndexColumnName;
				break;
		}
		return DatabaseConstants.WordIndexColumnName;
	}

	private FindQuery CreateQuery(PathFinderParameter parameter)
	{
		var param = new Dictionary<string, object>();

		var filter = "";

		if (mode != GameMode.All)
		{
			PresentedWord word = parameter.Word;
			var wordIndexColumn = GetIndexColumnName(word);
			param.Add("@PrimaryWord", word.Content);
			if (word.CanSubstitution)
			{
				filter = $" WHERE ({wordIndexColumn} = @PrimaryWord OR {wordIndexColumn} = @SecondaryWord)";
				param.Add("@SecondaryWord", word.Substitution!);
			}
			else
			{
				filter = $" WHERE ({wordIndexColumn} = @PrimaryWord)";
			}

			// Use end-words?
			ApplyFlagFilter(parameter.Options, PathFinderFlags.UseEndWord, endWordFlag, ref filter);

			// Use attack-words?
			ApplyFlagFilter(parameter.Options, PathFinderFlags.UseAttackWord, attackWordFlag, ref filter);

			// Only KungKungTta words if we're on KungKungTta mode
			if (mode == GameMode.KungKungTta)
				filter += $" AND ({DatabaseConstants.FlagsColumnName} & {(int)WordFlags.KKT3} != 0)";
		}
		var orderPriority = $"({CreateWordPriorityFuncCall(parameter.MissionChar, param)} + LENGTH({DatabaseConstants.WordColumnName}))";

		return new FindQuery($"SELECT {DatabaseConstants.WordColumnName}, {DatabaseConstants.FlagsColumnName} FROM {DatabaseConstants.WordTableName}{filter} ORDER BY {orderPriority} DESC LIMIT {maxCount};", param);
	}

	private string CreateWordPriorityFuncCall(
		string missionChar,
		IDictionary<string, object> param)
	{
		if (string.IsNullOrWhiteSpace(missionChar))
		{
			/*
			WordPriority(
				string flagsColumnName,
				int endWordFlag,
				int attackWordFlag,
				int endWordPriority,
				int attackWordPriority,
				int normalWordPriority)
			 */
			return string.Format(
				CultureInfo.InvariantCulture,
				"{0}({1}, {2}, {3}, {4}, {5}, {6})",
				Connection.GetWordPriorityFuncName(),
				DatabaseConstants.FlagsColumnName,
				(int)endWordFlag,
				(int)attackWordFlag,
				GetWordTypePriority(preference, WordCategories.EndWord), // End word
				GetWordTypePriority(preference, WordCategories.AttackWord), // Attack word
				GetWordTypePriority(preference, WordCategories.None)); // Normal word
		}
		else
		{
			/*
			MissionWordPriority(
				string wordColumnName,
				string flagsColumnName,
				string missionChar,
				int endWordFlag,
				int attackWordFlag,
				int endMissionWordPriority,
				int endWordPriority,
				int attackMissionWordPriority,
				int attackWordPriority,
				int missionWordPriority,
				int normalWordPriority)
			*/
			param.Add("@MissionChar", missionChar);
			return string.Format(
				CultureInfo.InvariantCulture,
				"{0}({1}, {2}, @MissionChar, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10})",
				Connection.GetMissionWordPriorityFuncName(),
				DatabaseConstants.WordColumnName,
				DatabaseConstants.FlagsColumnName,
				(int)endWordFlag,
				(int)attackWordFlag,
				GetWordTypePriority(preference, WordCategories.EndWord | WordCategories.MissionWord), // End mission word
				GetWordTypePriority(preference, WordCategories.EndWord), // End word
				GetWordTypePriority(preference, WordCategories.AttackWord | WordCategories.MissionWord), // Attack mission word
				GetWordTypePriority(preference, WordCategories.AttackWord), // Attack word
				GetWordTypePriority(preference, WordCategories.MissionWord), // Mission word
				GetWordTypePriority(preference, WordCategories.None)); // Normal word
		}
	}

	private static int GetWordTypePriority(WordPreference preference, WordCategories attributes)
	{
		WordCategories[] fullAttribs = preference.GetAttributes();
		var index = Array.IndexOf(fullAttribs, attributes);
		return fullAttribs.Length - (index >= 0 ? index : fullAttribs.Length) - 1;
	}

	private static void ApplyFlagFilter(
		PathFinderFlags haystack,
		PathFinderFlags needle,
		WordFlags flag,
		ref string filter)
	{
		if (!haystack.HasFlag(needle))
			filter += $" AND ({DatabaseConstants.FlagsColumnName} & {(int)flag} = 0)";
	}

	private sealed record FindQuery(string Sql, IDictionary<string, object> Parameters);
#endregion
}
