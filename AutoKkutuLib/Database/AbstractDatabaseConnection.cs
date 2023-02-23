using AutoKkutuLib.Database.Sql.Query;
using System.Data;

namespace AutoKkutuLib.Database;

public abstract class AbstractDatabaseConnection : IDbConnection
{
	private IDbConnection? underlyingConnection;
	private QueryFactory? queryFactory;

	protected IDbConnection Connection => underlyingConnection ?? throw new NullReferenceException("Database connection accessed before initialized");

	public QueryFactory Query => queryFactory ?? throw new NullReferenceException("Database query factory accessed before initialized");

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

	/// <summary>
	/// This method must called on initialization phase.
	/// </summary>
	protected void Initialize(IDbConnection connection, QueryFactory queryFactory)
	{
		if (underlyingConnection != null)
			throw new InvalidOperationException($"{nameof(Connection)} is already initialized");
		underlyingConnection = connection;
		this.queryFactory = queryFactory;
	}

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
