using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.Databases.WordIndex
{
	[Table(DatabaseConstants.KkutuEndWordIndexTableName)]
	public class KkutuEndWordIndex : IWordIndex
	{
		[Column(DatabaseConstants.WordIndexColumnName), Key, MinLength(2), MaxLength(2)]
		public string Index
		{
			get; set;
		}
	}
}
