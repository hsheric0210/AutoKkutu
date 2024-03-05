using AutoKkutuLib.Database.Helper;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Game;

namespace AutoKkutuLib;

public partial class AutoKkutu
{
	private const string autoKkutuMediator = "AutoKkutu.Mediator";

	private void RegisterInterconnections(IGame game)
	{
		game.DiscoverWordHistory += HandleDiscoverWordHistory;
		game.HintWordPresented += HandleExampleWordPresented;
		game.RoundChanged += HandleRoundChanged;
		game.GameEnded += HandleGameEnded;
		game.UnsupportedWordEntered += HandleUnsupportedWordEntered;
	}

	private void UnregisterInterconnections(IGame game)
	{
		game.DiscoverWordHistory -= HandleDiscoverWordHistory;
		game.HintWordPresented -= HandleExampleWordPresented;
		game.RoundChanged -= HandleRoundChanged;
		game.GameEnded -= HandleGameEnded;
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

	private void HandleGameEnded(object? sender, EventArgs args)
	{
		PathFilter.PreviousPaths.Clear();
		PathFilter.UnsupportedPaths.Clear();
	}

	private void HandleUnsupportedWordEntered(object? sender, UnsupportedWordEventArgs args)
	{
		var isInexistent = !args.IsExistingWord;
		var word = args.Word;
		ICollection<string> list;
		if (isInexistent)
		{
			list = PathFilter.InexistentPaths;
			LibLogger.Warn(autoKkutuMediator, I18n.Main_UnsupportedWord_Inexistent, word);
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
					LibLogger.Debug(autoKkutuMediator, "New end node: {node}", node);
					PathFilter.NewEndPaths.Add((gm, node));
				}
			}
			LibLogger.Warn(autoKkutuMediator, I18n.Main_UnsupportedWord_Existent, word);
		}
		list.Add(word);
	}
}
