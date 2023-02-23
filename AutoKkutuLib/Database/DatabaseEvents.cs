namespace AutoKkutuLib.Database;

public static class DatabaseEvents
{
	public static event EventHandler<DatabaseImportEventArgs>? DatabaseImportStart;

	public static event EventHandler<DatabaseImportEventArgs>? DatabaseImportDone;

	public static event EventHandler? DatabaseIntegrityCheckStart;

	public static event EventHandler<DataBaseIntegrityCheckDoneEventArgs>? DatabaseIntegrityCheckDone;

	public static event EventHandler? DatabaseError;

	public static void TriggerDatabaseImportStart(this DatabaseImportEventArgs args) => DatabaseImportStart?.Invoke(null, args);

	public static void TriggerDatabaseImportDone(this DatabaseImportEventArgs args) => DatabaseImportDone?.Invoke(null, args);

	public static void TriggerDatabaseIntegrityCheckStart() => DatabaseIntegrityCheckStart?.Invoke(null, EventArgs.Empty);

	public static void TriggerDatabaseIntegrityCheckDone(this DataBaseIntegrityCheckDoneEventArgs args) => DatabaseIntegrityCheckDone?.Invoke(null, args);

	public static void TriggerDatabaseError() => DatabaseError?.Invoke(null, EventArgs.Empty);
}

public class DatabaseImportEventArgs : EventArgs
{
	public string Name
	{
		get;
	}

	public string? Result
	{
		get;
	}

	public DatabaseImportEventArgs(string name) => Name = name;

	public DatabaseImportEventArgs(string name, string result)
	{
		Name = name;
		Result = result;
	}
}

public class DataBaseIntegrityCheckDoneEventArgs : EventArgs
{
	public string Result
	{
		get;
	}

	public DataBaseIntegrityCheckDoneEventArgs(string result) => Result = result;
}
