using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutuLib.Database;

public sealed class WordModel
{
	[Column(DatabaseConstants.WordColumnName)]
	public string Word { get; set; } = "";

	[Column(DatabaseConstants.WordIndexColumnName)]
	public string WordIndex { get; set; } = "";

	[Column(DatabaseConstants.ReverseWordIndexColumnName)]
	public string ReverseWordIndex { get; set; } = "";

	[Column(DatabaseConstants.KkutuWordIndexColumnName)]
	public string KkutuWordIndex { get; set; } = "";

	[Column(DatabaseConstants.TypeColumnName)]
	public int Type { get; set; } = 0;

	[Column(DatabaseConstants.ThemeColumn1Name)]
	public long Theme1 { get; set; } = 0;

	[Column(DatabaseConstants.ThemeColumn2Name)]
	public long Theme2 { get; set; } = 0;

	[Column(DatabaseConstants.ThemeColumn3Name)]
	public long Theme3 { get; set; } = 0;

	[Column(DatabaseConstants.ThemeColumn4Name)]
	public long Theme4 { get; set; } = 0;

	[Column(DatabaseConstants.ChoseongColumnName)]
	public string Choseong { get; set; } = "";

	[Column(DatabaseConstants.MeaningColumnName)]
	public string Meaning { get; set; } = "";

	[Column(DatabaseConstants.FlagsColumnName)]
	public int Flags { get; set; }
}
