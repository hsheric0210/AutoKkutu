using AutoKkutuLib;
using AutoKkutuLib.Database;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Game;
using AutoKkutuLib.Game.Events;
using AutoKkutuLib.Path;
using CefSharp;
using Serilog;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AutoKkutuGui;

public partial class MainWindow : Window
{
	public const string VERSION = "1.0.0000";

	// Succeed KKutu-Helper Release v5.6.8500
	private const string TITLE = "AutoKkutu - Improved KKutu-Helper";

	public MainWindow()
	{
		// Visual components setup
		InitializeComponent();
		Title = TITLE;
		VersionLabel.Content = "v1.0";

		Log.Information(I18n.Main_StartLoad);
		LoadOverlay.Visibility = Visibility.Visible;

		DatabaseEvents.DatabaseError += OnDataBaseError;
		DatabaseEvents.DatabaseIntegrityCheckStart += OnDatabaseIntegrityCheckStart;
		DatabaseEvents.DatabaseIntegrityCheckDone += OnDatabaseIntegrityCheckDone;
		DatabaseEvents.DatabaseImportStart += OnDatabaseImportStart;
		DatabaseEvents.DatabaseImportDone += OnDatabaseImportDone;

		Main.PathListUpdated += OnPathListUpdated;
		Main.InitializeUI += OnInitializeUI;
		Main.SearchStateChanged += OnSearchStateChanged;
		Main.StatusMessageChanged += OnStatusMessageChanged;

		Main.AutoKkutu.Game.AutoEnter.InputDelayApply += OnEnterDelaying;
		Main.AutoKkutu.Game.AutoEnter.NoPathAvailable += OnPathNotFound;
		Main.AutoKkutu.Game.AutoEnter.AutoEntered += OnAutoEntered;

		Main.Initialize();
	}

	/* EVENTS: AutoEnter */

	private void OnEnterDelaying(object? sender, InputDelayEventArgs args) => this.UpdateStatusMessage(StatusMessage.Delaying, args.Delay);

	private void OnPathNotFound(object? sender, EventArgs args) => this.UpdateStatusMessage(StatusMessage.NotFound);

	private void OnAutoEntered(object? sender, AutoEnterEventArgs args) => this.UpdateStatusMessage(StatusMessage.AutoEntered, args.Content);

	/* EVENTS: AutoKkutu */

	private void OnPathListUpdated(object? sender, EventArgs args) => Dispatcher.Invoke(() => PathList.ItemsSource = Main.AutoKkutu.PathFinder.DisplayList);

	private void OnInitializeUI(object? sender, EventArgs args)
	{
		this.UpdateStatusMessage(StatusMessage.Wait);
		UpdateSearchState(null, false);
		Dispatcher.Invoke(() =>
		{
			// Apply browser frame
			BrowserContainer.Content = Main.Browser;

			// Update database icon
			var img = new BitmapImage();
			img.BeginInit();
			img.UriSource = new Uri($@"Images\{Main.AutoKkutu.Database.GetDBType()}.png", UriKind.Relative);
			img.EndInit();
			DBLogo.Source = img;

			// Hide LoadOverlay
			LoadOverlay.Visibility = Visibility.Hidden;
		});
	}

	private void OnSearchStateChanged(object? sender, SearchStateChangedEventArgs args) => UpdateSearchState(args.Arguments, args.IsEndWord);

	private void OnStatusMessageChanged(object? sender, StatusMessageChangedEventArgs args) => this.UpdateStatusMessage(args.Status, args.GetFormatterArguments());

	/* EVENTS: Database */

	private void OnDatabaseIntegrityCheckDone(object? sender, DataBaseIntegrityCheckDoneEventArgs args) => this.UpdateStatusMessage(StatusMessage.DatabaseIntegrityCheckDone, args.Result);

	private void OnDatabaseIntegrityCheckStart(object? sender, EventArgs e) => this.UpdateStatusMessage(StatusMessage.DatabaseIntegrityCheck);

	private void OnDataBaseError(object? sender, EventArgs e) => this.UpdateStatusMessage(StatusMessage.Error);

	private void OnDatabaseImportDone(object? sender, DatabaseImportEventArgs args) => this.UpdateStatusMessage(StatusMessage.BatchJobDone, args.Name, args.Result);

	private void OnDatabaseImportStart(object? sender, DatabaseImportEventArgs args) => this.UpdateStatusMessage(StatusMessage.BatchJob, args.Name);

	/* EVENT: Hotkeys */

	private void OnToggleDelay(object? sender, RoutedEventArgs e) => Main.ToggleFeature(config => config.DelayEnabled = !config.DelayEnabled, StatusMessage.DelayToggled);

	private void OnToggleAllDelay(object? sender, RoutedEventArgs e) => Main.ToggleFeature(config => config.DelayEnabled = config.FixDelayEnabled = !config.DelayEnabled, StatusMessage.AllDelayToggled);

	private void OnToggleAutoEnter(object? sender, RoutedEventArgs e) => Main.ToggleFeature(config => config.AutoEnterEnabled = !config.AutoEnterEnabled, StatusMessage.AutoEnterToggled);

	/* EVENTS: UI */

	private void OnChatFieldKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key is Key.Enter or Key.Return)
			SubmitChat_Click(sender, e);
	}

	private void OnClipboardSubmitClick(object? sender, RoutedEventArgs e)
	{
		try
		{
			var clipboard = Clipboard.GetText();
			if (!string.IsNullOrWhiteSpace(clipboard))
				Main.SendMessage(clipboard);
		}
		catch (Exception ex)
		{
			Log.Warning(I18n.Main_ClipboardSubmitException, ex);
		}
	}

	private void OnColorManagerClick(object? sender, RoutedEventArgs e) => new ColorManagement(Main.ColorPreference).Show();

	private void OnDBManagementClicked(object? sender, RoutedEventArgs e) => new DatabaseManagement(Main.AutoKkutu.Database).Show();

	private void OnOpenDevConsoleClicked(object? sender, RoutedEventArgs e) => Main.Browser.ShowDevTools();

	private void OnPathListContextMenuOpen(object? sender, ContextMenuEventArgs e)
	{
		var source = (FrameworkElement)e.Source;
		ContextMenu contextMenu = source.ContextMenu;
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not PathObject)
			return;
		var current = new GuiPathObject((PathObject)currentSelected);
		foreach (Control item in contextMenu.Items)
		{
			if (item is not MenuItem)
				continue;

			var available = true;
			switch (item.Name.ToUpperInvariant())
			{
				case "MAKEEND":
					available = current.MakeEndAvailable;
					break;

				case "MAKEATTACK":
					available = current.MakeAttackAvailable;
					break;

				case "MAKENORMAL":
					available = current.MakeNormalAvailable;
					break;

				case "INCLUDE":
					available = current.AlreadyUsed || current.Excluded;
					break;

				case "EXCLUDE":
					available = !current.AlreadyUsed;
					break;

				case "REMOVE":
					available = !current.Excluded;
					break;
			}

			item.IsEnabled = available;
		}
	}

	private void OnPathListMakeAttackClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not PathObject)
			return;
		Main.AutoKkutu.Database.Connection.MakeAttack((PathObject)currentSelected, Main.AutoKkutu.Game.CurrentGameMode);
	}

	private void OnPathListMakeEndClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not PathObject)
			return;
		Main.AutoKkutu.Database.Connection.MakeEnd((PathObject)currentSelected, Main.AutoKkutu.Game.CurrentGameMode);
	}

	private void OnPathListMakeNormalClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not PathObject)
			return;
		Main.AutoKkutu.Database.Connection.MakeNormal((PathObject)currentSelected, Main.AutoKkutu.Game.CurrentGameMode);
	}

	// TODO: Move these duplicate path list writes to Lib.SpecialPathList
	private void OnPathListQueueExcludedClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not PathObject)
			return;
		var path = (PathObject)currentSelected;
		path.Excluded = true;
		path.RemoveQueued = false;
		SpecialPathList spl = Main.AutoKkutu.SpecialPathList;
		try
		{
			spl.Lock.EnterWriteLock();
			spl.UnsupportedPaths.Add(path.Content);
			spl.InexistentPaths.Remove(path.Content);
		}
		finally
		{
			spl.Lock.ExitWriteLock();
		}
	}

	private void OnPathListIncludeClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not PathObject)
			return;
		var path = (PathObject)currentSelected;
		path.Excluded = false;
		path.RemoveQueued = false;
		SpecialPathList spl = Main.AutoKkutu.SpecialPathList;
		try
		{
			spl.Lock.EnterWriteLock();
			spl.UnsupportedPaths.Remove(path.Content);
			spl.InexistentPaths.Remove(path.Content);
		}
		finally
		{
			spl.Lock.ExitWriteLock();
		}
	}

	private void OnPathListQueueRemoveClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not PathObject)
			return;
		var path = (PathObject)currentSelected;
		path.Excluded = false;
		path.RemoveQueued = true;
		SpecialPathList spl = Main.AutoKkutu.SpecialPathList;
		try
		{
			spl.Lock.EnterWriteLock();
			spl.UnsupportedPaths.Add(path.Content);
			spl.InexistentPaths.Add(path.Content);
		}
		finally
		{
			spl.Lock.ExitWriteLock();
		}
	}

	private void OnPathListCopyClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not PathObject)
			return;
		Clipboard.SetText(((PathObject)currentSelected).Content);
	}

	private void OnPathListMouseDoubleClick(object? sender, MouseButtonEventArgs e)
	{
		var selected = PathList.SelectedItem;
		if (selected is not PathObject)
			return;

		var i = (PathObject)selected;
		if (i != null)
		{
			Log.Information(I18n.Main_PathSubmitted, i.Content);
			Main.SendMessage(i.Content);
		}
	}

	private void OnSettingsClick(object? sender, RoutedEventArgs e) => new ConfigWindow(Main.Prefs).Show();

	private void OnSubmitURLClick(object? sender, RoutedEventArgs e)
	{
		Main.Browser.Load(CurrentURL.Text);
		Main.FrameReloaded();
	}

	private void OnWindowClose(object? sender, CancelEventArgs e)
	{
		Log.Information(I18n.Main_ClosingDBConnection);
		try
		{
			// Dispose 책임 순서 꼬일거같은데
			Main.AutoKkutu.Database.Dispose();
			Main.AutoKkutu.Dispose();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to dispose database object.");
		}
		Log.CloseAndFlush();
	}

	private void SearchField_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key is Key.Enter or Key.Return)
			SubmitSearch_Click(sender, e);
	}

	private void UpdateSearchState(PathUpdateEventArgs? arg, bool IsEnd = false)
	{
		string Result;
		if (arg == null)
		{
			Result = IsEnd ? I18n.PathFinderUnavailable : I18n.PathFinderWaiting;
		}
		else
		{
			Result = CreatePathResultExplain(arg);
		}
		Dispatcher.Invoke(() => SearchResult.Text = Result);
	}

	private static string CreatePathResultExplain(PathUpdateEventArgs arg)
	{
		var parameter = arg.Result;
		var filter = $"'{parameter.Word.Content}'";
		if (parameter.Word.CanSubstitution)
			filter = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderSearchOverview_Or, filter, $"'{parameter.Word.Substitution}'");
		if (!string.IsNullOrWhiteSpace(parameter.MissionChar))
			filter = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderSearchOverview_MissionChar, filter, parameter.MissionChar);
		var FilterText = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderSearchOverview, filter);
		var SpecialFilterText = "";
		string FindResult;
		var ElapsedTimeText = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderTookTime, arg.TimeMillis);
		if (arg.ResultType == PathFindResult.Found)
		{
			FindResult = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderFound, arg.TotalWordCount, arg.CalcWordCount);
		}
		else
		{
			FindResult = arg.ResultType == PathFindResult.NotFound
				? string.Format(CultureInfo.CurrentCulture, I18n.PathFinderFoundButEmpty, arg.TotalWordCount)
				: I18n.PathFinderError;
		}
		if (parameter.Options.HasFlag(PathFinderOptions.UseEndWord))
			SpecialFilterText += ", " + I18n.PathFinderEndWord;
		if (parameter.Options.HasFlag(PathFinderOptions.UseAttackWord))
			SpecialFilterText += ", " + I18n.PathFinderAttackWord;

		var newSpecialFilterText = string.IsNullOrWhiteSpace(SpecialFilterText) ? string.Empty : string.Format(CultureInfo.CurrentCulture, I18n.PathFinderIncludedWord, SpecialFilterText[2..]);
		return FilterText + Environment.NewLine + newSpecialFilterText + Environment.NewLine + FindResult + Environment.NewLine + ElapsedTimeText;
	}

	private void SubmitChat_Click(object? sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(ChatField.Text))
		{
			Main.SendMessage(ChatField.Text);
			ChatField.Text = "";
		}
	}

	private void SubmitSearch_Click(object? sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(SearchField.Text))
		{
			Main.AutoKkutu.PathFinder.Find(GameMode.LastAndFirst, new PathFinderParameter(new PresentedWord(SearchField.Text, false), Main.AutoKkutu.Game.CurrentMissionChar ?? "", PathFinderOptions.ManualSearch, Main.Prefs.MaxDisplayedWordCount), Main.Prefs.ActiveWordPreference);
			SearchField.Text = "";
		}
	}
}
