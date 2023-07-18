using AutoKkutuLib.Database.Path;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Game;
using Serilog;

namespace AutoKkutuLib;

public partial class AutoKkutu
{
	private void RegisterInterconnections(IGame game)
	{
		game.DiscoverWordHistory += HandleDiscoverWordHistory;
		game.HintWordPresented += HandleExampleWordPresented;
		game.RoundChanged += HandleRoundChanged;
		game.UnsupportedWordEntered += HandleUnsupportedWordEntered;
	}

	private void UnregisterInterconnections(IGame game)
	{
		game.DiscoverWordHistory -= HandleDiscoverWordHistory;
		game.HintWordPresented -= HandleExampleWordPresented;
		game.RoundChanged -= HandleRoundChanged;
		game.UnsupportedWordEntered -= HandleUnsupportedWordEntered;
	}

	private void HandleDiscoverWordHistory(object? sender, WordHistoryEventArgs args)
	{
		var word = args.Word;
		PathFilter.NewPaths.Add(word);
		PathFilter.PreviousPaths.Add(word);
	}

	private void HandleExampleWordPresented(object? sender, WordPresentEventArgs args)
	{
		var word = args.Word;
		PathFilter.NewPaths.Add(word);
	}

	private void HandleRoundChanged(object? sender, EventArgs args) => PathFilter.PreviousPaths.Clear();

	private void HandleUnsupportedWordEntered(object? sender, UnsupportedWordEventArgs args)
	{
		var isInexistent = !args.IsExistingWord;
		var word = args.Word;
		ICollection<string> list;
		if (isInexistent)
		{
			list = PathFilter.InexistentPaths;
			Log.Warning(I18n.Main_UnsupportedWord_Inexistent, word);
		}
		else
		{
			list = PathFilter.UnsupportedPaths;
			var gm = Game.Session.GameMode;
			if (args.IsEndWord && gm != GameMode.None)
			{
				var node = gm.ConvertWordToTailNode(word);
				if (!string.IsNullOrWhiteSpace(node))
				{
					Log.Debug("New end node: {node}", node);
					PathFilter.NewEndPaths.Add((gm, node));
				}
			}
			Log.Warning(I18n.Main_UnsupportedWord_Existent, word);
		}
		list.Add(word);
	}
}
