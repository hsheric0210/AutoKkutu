using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.Databases
{
	public class WordIndexModel
	{
		[Column(DatabaseConstants.WordIndexColumnName), Key, MinLength(1), MaxLength(2)]
		public string Index
		{
			get; set;
		}
	}
}
