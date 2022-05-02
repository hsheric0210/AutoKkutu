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
			parameter.Precision = Precision;

			var datatype = TranslateDataType();
			if (datatype != null)
				parameter.MySqlDbType = (MySqlDbType)datatype;

			return parameter;
		}

		public MySqlDbType? TranslateDataType()
		{
			if (DataType == null)
				return null;

			switch (DataType)
			{
				case CommonDatabaseType.SmallInt:
					return MySqlDbType.Int16;

				case CommonDatabaseType.MiddleInt:
					return MySqlDbType.Int32;

				case CommonDatabaseType.Character:
				case CommonDatabaseType.CharacterVarying:
					return MySqlDbType.VarChar;
			}

			throw new NotSupportedException(DataType.ToString());
		}
	}
}
