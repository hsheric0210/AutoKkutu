using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.Databases.WordIndex
{
	[Table(DatabaseConstants.EndWordIndexTableName)]
	public class EndWordIndex : IWordIndex<EndWordIndex>
	{
		[Column(DatabaseConstants.WordIndexColumnName), Key, MinLength(1), MaxLength(1)]
		public string Index
		{
			get; set;
		}
	}
}
