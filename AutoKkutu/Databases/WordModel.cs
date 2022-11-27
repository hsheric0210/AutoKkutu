using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.Databases
{
	public class WordModel
	{
		[Column(DatabaseConstants.SequenceColumnName), Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int SequenceId
		{
			get; set;
		}

		[Column(DatabaseConstants.WordColumnName), MaxLength(DatabaseConstants.MaxWordLength)]
		public string Word
		{
			get; set;
		}

		[Column(DatabaseConstants.WordIndexColumnName), MinLength(1), MaxLength(1)]
		public string WordIndex
		{
			get; set;
		}

		[Column(DatabaseConstants.ReverseWordIndexColumnName), MinLength(1), MaxLength(1)]
		public string ReverseWordIndex
		{
			get; set;
		}

		[Column(DatabaseConstants.KkutuWordIndexColumnName), MinLength(2), MaxLength(2)]
		public string KkutuWorldIndex
		{
			get; set;
		}

		[Column(DatabaseConstants.FlagsColumnName)]
		public int Flags
		{
			get; set;
		}
	}
}
