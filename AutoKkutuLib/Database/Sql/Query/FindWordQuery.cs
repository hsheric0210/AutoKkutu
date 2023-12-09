using Dapper;
using System.Collections.Immutable;
using System.Globalization;

namespace AutoKkutuLib.Database.Sql.Query;

public class FindWordQuery : SqlQuery<IImmutableList<PathObject>>
{
	private readonly GameMode mode;
	private readonly WordPreference preference;

	private readonly WordFlags endWordFlag;
	private readonly WordFlags attackWordFlag;

	public PathDetails? Parameter { get; set; }

	internal FindWordQuery(DbConnectionBase connection, GameMode mode, WordPreference preference) : base(connection)
	{
		this.mode = mode;
		this.preference = preference;
		SelectFlags(mode, out endWordFlag, out attackWordFlag);
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

	public IImmutableList<PathObject> Execute(PathDetails parameter)
	{
		Parameter = parameter;
		return Execute();
	}

	public override IImmutableList<PathObject> Execute()
	{
		if (Parameter is not PathDetails param)
			throw new InvalidOperationException(nameof(Parameter) + " not set.");

		LibLogger.Debug<FindWordQuery>("Finding the optimal word list for {0}.", param);
		var findQuery = CreateQuery(param);
		var result = new List<PathObject>();
		try
		{
			LibLogger.Debug<FindWordQuery>("Full query string is {0}.", findQuery.Sql);

			foreach (var found in Connection.Query<WordModel>(findQuery.Sql, new DynamicParameters(findQuery.Parameters)))
			{
				var wordString = found.Word.Trim();
				var categories = SetupWordCategories(
					 wordString,
					(WordFlags)found.Flags,
					param.Condition.MissionChar,
					out var missionCharCount);
				result.Add(new PathObject(wordString, categories, missionCharCount));
			}
		}
		catch
		{
			LibLogger.Error<FindWordQuery>("Errored query: {sql}", findQuery.Sql);
			throw;
		}

		return result.ToImmutableList();
	}

	#region Categorization
	private WordCategories SetupWordCategories(
		string word,
		WordFlags wordFlags,
		string? missionChar,
		out int missionCharCount)
	{
		var category = WordCategories.None;
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
	private string GetIndexColumnName(WordCondition word)
	{
		switch (mode)
		{
			case GameMode.FirstAndLast:
				return DatabaseConstants.ReverseWordIndexColumnName;

			case GameMode.Kkutu: // TODO: 세 글자용 인덱스도 만들기
				if (word.Char.Length == 2 || word.SubAvailable && word.SubChar!.Length == 2)
					return DatabaseConstants.KkutuWordIndexColumnName;
				break;

			case GameMode.Hunmin:
				return DatabaseConstants.ChoseongColumnName;
		}
		return DatabaseConstants.WordIndexColumnName;
	}

	private FindQuery CreateQuery(PathDetails parameter)
	{
		var param = new Dictionary<string, object>();

		var filter = "";

		if (mode != GameMode.All)
		{
			var word = parameter.Condition;
			var wordIndexColumn = GetIndexColumnName(word);
			if (word.Regexp)
			{
				// 정규 표현식 검색
				// '(?i) for Case-insensitive match - https://stackoverflow.com/a/43636
				// '^' means start of the string, '$' means end of the string
				param.Add("@PrimaryWord", "(?i)^" + word.Char + '$');

				// SQLite: https://github.com/nalgeon/sqlean/blob/main/docs/regexp.md
				// PostgreSQL: https://www.postgresql.org/docs/current/functions-matching.html
				// MySQL: https://dev.mysql.com/doc/refman/8.0/en/regexp.html
				// MariaDB: https://mariadb.com/kb/en/regexp/
				filter = $" WHERE ({DatabaseConstants.WordColumnName} REGEXP @PrimaryWord)";
			}
			else
			{
				// 일반적인 검색
				param.Add("@PrimaryWord", word.Char);

				if (word.SubAvailable)
				{
					filter = $" WHERE ({wordIndexColumn} = @PrimaryWord OR {wordIndexColumn} = @SecondaryWord)";
					param.Add("@SecondaryWord", word.SubChar!);
				}
				else
				{
					filter = $" WHERE ({wordIndexColumn} = @PrimaryWord)";
				}
			}

			// Use end-words?
			if (!parameter.HasFlag(PathFlags.UseEndWord))
				ApplyExclusionFilter(endWordFlag, ref filter);

			// Use attack-words?
			if (!parameter.HasFlag(PathFlags.UseAttackWord))
				ApplyExclusionFilter(attackWordFlag, ref filter);

			// Only KungKungTta words if we're on KungKungTta mode
			if (mode == GameMode.KungKungTta)
			{
				var flag = WordFlags.KKT3;
				if (word.WordLength == 2)
					flag = WordFlags.KKT2;
				filter += $" AND ({DatabaseConstants.FlagsColumnName} & {(int)flag} != 0)";
			}
		}
		var orderPriority = $"({CreateWordPriorityFuncCall(parameter.Condition.MissionChar, param)} + LENGTH({DatabaseConstants.WordColumnName}))";

		return new FindQuery($"SELECT {DatabaseConstants.WordColumnName}, {DatabaseConstants.FlagsColumnName} FROM {DatabaseConstants.WordTableName}{filter} ORDER BY {orderPriority} DESC LIMIT {parameter.MaxDisplayed};", param);
	}

	private string CreateWordPriorityFuncCall(
		string? missionChar,
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

	private static int GetWordTypePriority(WordPreference preference, WordCategories category)
	{
		var fullAttribs = preference.GetAttributes();
		var index = Array.IndexOf(fullAttribs, category);
		return fullAttribs.Length - (index >= 0 ? (index - 1) : fullAttribs.Length); // Shouldn't be negative
	}

	private static void ApplyExclusionFilter(
		WordFlags flag,
		ref string filter) => filter += $" AND ({DatabaseConstants.FlagsColumnName} & {(int)flag} = 0)";

	private sealed record FindQuery(string Sql, IDictionary<string, object> Parameters);
	#endregion
}
