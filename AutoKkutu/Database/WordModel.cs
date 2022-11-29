namespace AutoKkutu.Database
{
	public sealed class WordModel
	{
		public int Id
		{
			get; set;
		}

		public string Word
		{
			get; set;
		} = "";

		public string WordIndex
		{
			get; set;
		} = "";

		public string ReverseWordIndex
		{
			get; set;
		} = "";

		public string KkutuWordIndex
		{
			get; set;
		} = "";

		public int Flags
		{
			get; set;
		}
	}
}
