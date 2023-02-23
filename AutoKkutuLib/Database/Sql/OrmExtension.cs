using Dapper;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutuLib.Database.Sql;

public static class OrmExtension
{
	public static void RegisterMapping(this Type type) => SqlMapper.SetTypeMap(type, new CustomPropertyTypeMap(type, (type, columnName) => Array.Find(type.GetProperties(), prop => prop.GetCustomAttributes(false).OfType<ColumnAttribute>().Any(attr => attr.Name == columnName))));
}
