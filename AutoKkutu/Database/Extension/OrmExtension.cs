using Dapper;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AutoKkutu.Database.Extension
{
	public static class OrmExtension
	{
		public static void RegisterMapping(this Type type)
		{
			SqlMapper.SetTypeMap(type, new CustomPropertyTypeMap(type, (type, columnName) => Array.Find(type.GetProperties(), prop => prop.GetCustomAttributes(false).OfType<ColumnAttribute>().Any(attr => attr.Name == columnName))));
		}
	}
}
