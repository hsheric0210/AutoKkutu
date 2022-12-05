using AutoKkutuLib.Utils;
using System.Data;

namespace AutoKkutuLib.Database;

public abstract class AbstractDatabaseConnection : IDbConnection
{
	private IDbConnection? _baseConnection;

	protected IDbConnection Connection => _baseConnection.RequireNotNull();

	/* Delegate properties */
	public string ConnectionString
	{
		get => Connection.ConnectionString;
		set => Connection.ConnectionString = value;
	}

	public int ConnectionTimeout => Connection.ConnectionTimeout;

	public string Database => Connection.Database;

	public ConnectionState State => Connection.State;

	protected AbstractDatabaseConnection()
	{
	}

	protected void Initialize(IDbConnection connection)
	{
		if (_baseConnection != null)
			throw new InvalidOperationException($"{nameof(Connection)} is already initialized");
		_baseConnection = connection;
	}

	public abstract void AddSequenceColumnToWordList();

	public abstract void ChangeWordListColumnType(string tableName, string columnName, string newType);

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
			Connection.Dispose();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public abstract string GetWordPriorityFuncName();

	public abstract string GetMissionWordPriorityFuncName();

	public abstract string GetWordListColumnOptions();

	public abstract void DropWordListColumn(string columnName);

	public abstract string? GetColumnType(string tableName, string columnName);

	public abstract bool IsColumnExists(string tableName, string columnName);

	public abstract bool IsTableExists(string tableName);

	public abstract void ExecuteVacuum();

	/* Delegate methods */
	public IDbTransaction BeginTransaction() => Connection.BeginTransaction();
	public IDbTransaction BeginTransaction(IsolationLevel il) => Connection.BeginTransaction(il);
	public void ChangeDatabase(string databaseName) => Connection.ChangeDatabase(databaseName);
	public void Close() => Connection.Close();
	public IDbCommand CreateCommand() => Connection.CreateCommand();
	public void Open() => Connection.Open();
}
