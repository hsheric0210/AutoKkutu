using Microsoft.Data.Sqlite;
using System.Data;
using System;

namespace AutoKkutu.Databases.SQLite
{
	public class SQLiteDatabaseParameter : CommonDatabaseParameter
	{
		public SQLiteDatabaseParameter(string name, object value) : base(name, value)
		{
		}

		public SQLiteDatabaseParameter(CommonDatabaseType dataType, string name, object value) : base(dataType, name, value)
		{
		}

		public SQLiteDatabaseParameter(CommonDatabaseType dataType, byte precision, string name, object value) : base(dataType, precision, name, value)
		{
		}

		public SQLiteDatabaseParameter(ParameterDirection direction, CommonDatabaseType dataType, byte precision, string name, object value) : base(direction, dataType, precision, name, value)
		{
		}

		public SqliteParameter Translate()
		{
			var parameter = new SqliteParameter();
			parameter.ParameterName = Name;
			parameter.Value = Value;
			parameter.Precision = Precision;

			var datatype = TranslateDataType();
			if (datatype != null)
				parameter.SqliteType = (SqliteType)datatype;

			return parameter;
		}

		public SqliteType? TranslateDataType()
		{
			if (DataType == null)
				return null;

			switch (DataType)
			{
				case CommonDatabaseType.SmallInt:
				case CommonDatabaseType.MiddleInt:
					return SqliteType.Integer;

				case CommonDatabaseType.Character:
				case CommonDatabaseType.CharacterVarying:
					return SqliteType.Text;
			}

			throw new NotSupportedException(DataType.ToString());
		}
	}
}
