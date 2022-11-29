using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.Database
{
	public sealed class WordModel
	{
		[Column(DatabaseConstants.WordColumnName)]
		public string Word
		{
			get; set;
		} = "";

		[Column(DatabaseConstants.WordIndexColumnName)]
		public string WordIndex
		{
			get; set;
		} = "";

		[Column(DatabaseConstants.ReverseWordIndexColumnName)]
		public string ReverseWordIndex
		{
			get; set;
		} = "";

		[Column(DatabaseConstants.KkutuWordIndexColumnName)]
		public string KkutuWordIndex
		{
			get; set;
		} = "";

		[Column(DatabaseConstants.FlagsColumnName)]
		public int Flags
		{
			get; set;
		}
	}
}
