using AutoKkutuLib.Database.Relational;

namespace AutoKkutuLib.Database;

public abstract class AbstractDatabase : IDisposable
{
	private AbstractDatabaseConnection? underlyingConnection;

	public AbstractDatabaseConnection Connection => underlyingConnection ?? throw new NullReferenceException("Database connection accessed before initialized");

	static AbstractDatabase() => typeof(WordModel).RegisterMapping();

	protected AbstractDatabase()
	{
	}

	public abstract AbstractDatabaseConnection OpenSecondaryConnection();

	public abstract string GetDBType();

	protected void Initialize(AbstractDatabaseConnection defaultConnection)
	{
		if (underlyingConnection != null)
			throw new InvalidOperationException($"{nameof(Connection)} is already initialized");
		underlyingConnection = defaultConnection;
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
}
