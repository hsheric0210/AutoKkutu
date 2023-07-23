using AutoKkutuLib.Database.Sql;
using AutoKkutuLib.Database.Sql.Query;
using System.Data;

namespace AutoKkutuLib.Database;

public abstract class DbConnectionBase : IDbConnection
{
	static DbConnectionBase() => typeof(WordModel).RegisterMapping();

	private IDbConnection Connection { get; }

	public abstract QueryFactory Query { get; }

	public abstract string DbType { get; }

	public string ConnectionString
	{
		get => Connection.ConnectionString;
		set => Connection.ConnectionString = value;
	}

	public int ConnectionTimeout => Connection.ConnectionTimeout;

	public string Database => Connection.Database;

	public ConnectionState State => Connection.State;

	protected DbConnectionBase(IDbConnection connection) => Connection = connection;

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

	/* Delegate methods */
	public IDbTransaction BeginTransaction() => Connection.BeginTransaction();
	public IDbTransaction BeginTransaction(IsolationLevel il) => Connection.BeginTransaction(il);
	public void ChangeDatabase(string databaseName) => Connection.ChangeDatabase(databaseName);
	public void Close() => Connection.Close();
	public IDbCommand CreateCommand() => Connection.CreateCommand();
	public void Open() => Connection.Open();
}
