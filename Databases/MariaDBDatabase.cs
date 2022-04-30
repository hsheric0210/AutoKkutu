namespace AutoKkutu.Databases
{
	public partial class MariaDBDatabase : MySQLDatabase
	{
		public MariaDBDatabase(string connectionString) : base(connectionString)
		{
		}

		public override string GetDBType() => "MariaDB";
	}
}
