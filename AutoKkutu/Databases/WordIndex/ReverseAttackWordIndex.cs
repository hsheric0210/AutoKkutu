using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.Databases.WordIndex
{
	[Table(DatabaseConstants.ReverseAttackWordIndexTableName)]
	public class ReverseAttackWordIndex : IWordIndex
	{
		[Column(DatabaseConstants.WordIndexColumnName), Key, MinLength(1), MaxLength(1)]
		public string Index
		{
			get; set;
		}
	}
}
