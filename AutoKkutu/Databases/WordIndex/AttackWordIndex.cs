using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.Databases.WordIndex
{
	[Table(DatabaseConstants.AttackWordIndexTableName)]
	public class AttackWordIndex : IWordIndex
	{
		[Column(DatabaseConstants.WordIndexColumnName), Key, MinLength(1), MaxLength(1)]
		public string Index
		{
			get; set;
		}
	}
}
