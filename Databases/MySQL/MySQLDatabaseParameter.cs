using System;
using System.Data;
using MySqlConnector;

namespace AutoKkutu.Databases.MySQL
{
	public class MySQLDatabaseParameter : CommonDatabaseParameter
	{
		public MySQLDatabaseParameter(string name, object value) : base(name, value)
		{
		}

		public MySQLDatabaseParameter(CommonDatabaseType dataType, string name, object value) : base(dataType, name, value)
		{
		}

		public MySQLDatabaseParameter(CommonDatabaseType dataType, byte precision, string name, object value) : base(dataType, precision, name, value)
		{
		}

		public MySQLDatabaseParameter(ParameterDirection direction, CommonDatabaseType dataType, byte precision, string name, object value) : base(direction, dataType, precision, name, value)
		{
		}

		public MySqlParameter Translate()
		{
			var parameter = new MySqlParameter();
			parameter.ParameterName = Name;
			parameter.Value = Value;
			parameter.MySqlDbType = TranslateDataType();
			parameter.Precision = Precision;
			return parameter;
		}

		public MySqlDbType TranslateDataType()
		{
			switch (DataType)
			{
				case CommonDatabaseType.Int16:
					return MySqlDbType.Int16;

				case CommonDatabaseType.Int32:
					return MySqlDbType.Int32;

				case CommonDatabaseType.Char:
				case CommonDatabaseType.VarChar:
					return MySqlDbType.VarChar;
			}

			throw new NotSupportedException(DataType?.ToString());
		}
	}
}
