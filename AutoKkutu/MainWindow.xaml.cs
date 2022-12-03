using AutoKkutu.Constants;
using AutoKkutu.Database;
using AutoKkutu.Modules;
using AutoKkutu.Modules.AutoEnter;
using AutoKkutu.Modules.PathFinder;
using AutoKkutu.Modules.PathManager;
using AutoKkutu.Utils;
using CefSharp;
using Serilog;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AutoKkutu
{
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

			AutoKkutuMain.PathListUpdated += OnPathListUpdated;
			AutoKkutuMain.InitializeUI += OnInitializeUI;
			AutoKkutuMain.SearchStateChanged += OnSearchStateChanged;
			AutoKkutuMain.StatusMessageChanged += OnStatusMessageChanged;

			AutoEnter.EnterDelaying += OnEnterDelaying;
			AutoEnter.PathNotFound += OnPathNotFound;
			AutoEnter.AutoEntered += OnAutoEntered;

			AutoKkutuMain.Initialize();
		}

		/* EVENTS: AutoEnter */

		private void OnEnterDelaying(object? sender, InputDelayEventArgs args) => this.UpdateStatusMessage(StatusMessage.Delaying, args.Delay);

		private void OnPathNotFound(object? sender, EventArgs args) => this.UpdateStatusMessage(StatusMessage.NotFound);

		private void OnAutoEntered(object? sender, AutoEnterEventArgs args) => this.UpdateStatusMessage(StatusMessage.AutoEntered, args.Content);

		/* EVENTS: AutoKkutu */

		private void OnPathListUpdated(object? sender, EventArgs args) => Dispatcher.Invoke(() => PathList.ItemsSource = PathFinder.DisplayList);

		private void OnInitializeUI(object? sender, EventArgs args)
		{
			this.UpdateStatusMessage(StatusMessage.Wait);
			UpdateSearchState(null, false);
			Dispatcher.Invoke(() =>
			{
				// Apply browser frame
				BrowserContainer.Content = AutoKkutuMain.Browser;

				// Update database icon
				var img = new BitmapImage();
				img.BeginInit();
				img.UriSource = new Uri($@"Images\{AutoKkutuMain.Database.GetDBType()}.png", UriKind.Relative);
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

		private void OnToggleDelay(object? sender, RoutedEventArgs e) => AutoKkutuMain.ToggleFeature(config => config.DelayEnabled = !config.DelayEnabled, StatusMessage.DelayToggled);

		private void OnToggleAllDelay(object? sender, RoutedEventArgs e) => AutoKkutuMain.ToggleFeature(config => config.DelayEnabled = config.FixDelayEnabled = !config.DelayEnabled, StatusMessage.AllDelayToggled);

		private void OnToggleAutoEnter(object? sender, RoutedEventArgs e) => AutoKkutuMain.ToggleFeature(config => config.AutoEnterEnabled = !config.AutoEnterEnabled, StatusMessage.AutoEnterToggled);

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
				string clipboard = Clipboard.GetText();
				if (!string.IsNullOrWhiteSpace(clipboard))
					AutoKkutuMain.SendMessage(clipboard);
			}
			catch (Exception ex)
			{
				Log.Warning(I18n.Main_ClipboardSubmitException, ex);
			}
		}

		private void OnColorManagerClick(object? sender, RoutedEventArgs e)
		{
			new ColorManagement(AutoKkutuMain.ColorPreference).Show();
		}

		private void OnDBManagementClicked(object? sender, RoutedEventArgs e) => new DatabaseManagement(AutoKkutuMain.Database).Show();

		private void OnOpenDevConsoleClicked(object? sender, RoutedEventArgs e) => AutoKkutuMain.Browser.ShowDevTools();

		private void OnPathListContextMenuOpen(object? sender, ContextMenuEventArgs e)
		{
			var source = (FrameworkElement)e.Source;
			ContextMenu contextMenu = source.ContextMenu;
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			var current = ((PathObject)currentSelected);
			foreach (Control item in contextMenu.Items)
			{
				if (item is not MenuItem)
					continue;

				bool available = true;
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
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			((PathObject)currentSelected).MakeAttack(AutoKkutuMain.Configuration.GameMode, AutoKkutuMain.Database.Connection);
		}

		private void OnPathListMakeEndClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			((PathObject)currentSelected).MakeEnd(AutoKkutuMain.Configuration.GameMode, AutoKkutuMain.Database.Connection);
		}

		private void OnPathListMakeNormalClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			((PathObject)currentSelected).MakeNormal(AutoKkutuMain.Configuration.GameMode, AutoKkutuMain.Database.Connection);
		}

		private void OnPathListQueueExcludedClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			var path = (PathObject)currentSelected;
			path.Excluded = true;
			path.RemoveQueued = false;
			try
			{
				PathManager.PathListLock.EnterWriteLock();
				PathManager.UnsupportedPathList.Add(path.Content);
				PathManager.InexistentPathList.Remove(path.Content);
			}
			finally
			{
				PathManager.PathListLock.ExitWriteLock();
			}
		}

		private void OnPathListIncludeClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			var path = (PathObject)currentSelected;
			path.Excluded = false;
			path.RemoveQueued = false;
			try
			{
				PathManager.PathListLock.EnterWriteLock();
				PathManager.UnsupportedPathList.Remove(path.Content);
				PathManager.InexistentPathList.Remove(path.Content);
			}
			finally
			{
				PathManager.PathListLock.ExitWriteLock();
			}
		}

		private void OnPathListQueueRemoveClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			var path = (PathObject)currentSelected;
			path.Excluded = false;
			path.RemoveQueued = true;
			try
			{
				PathManager.PathListLock.EnterWriteLock();
				PathManager.UnsupportedPathList.Add(path.Content);
				PathManager.InexistentPathList.Add(path.Content);
			}
			finally
			{
				PathManager.PathListLock.ExitWriteLock();
			}
		}

		private void OnPathListCopyClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			Clipboard.SetText(((PathObject)currentSelected).Content);
		}

		private void OnPathListMouseDoubleClick(object? sender, MouseButtonEventArgs e)
		{
			object selected = PathList.SelectedItem;
			if (selected is not PathObject)
				return;

			var i = (PathObject)selected;
			if (i != null)
			{
				Log.Information(I18n.Main_PathSubmitted, i.Content);
				AutoKkutuMain.SendMessage(i.Content);
			}
		}

		private void OnSettingsClick(object? sender, RoutedEventArgs e)
		{
			new ConfigWindow(AutoKkutuMain.Configuration).Show();
		}

		private void OnSubmitURLClick(object? sender, RoutedEventArgs e)
		{
			AutoKkutuMain.Browser.Load(CurrentURL.Text);
			AutoKkutuMain.FrameReloaded();
		}

		private void OnWindowClose(object? sender, CancelEventArgs e)
		{
			Log.Information(I18n.Main_ClosingDBConnection);
			try
			{
				AutoKkutuMain.Database.Dispose();
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
				if (IsEnd)
					Result = I18n.PathFinderUnavailable;
				else
					Result = I18n.PathFinderWaiting;
			}
			else
			{
				Result = CreatePathResultExplain(arg);
			}
			Dispatcher.Invoke(() => SearchResult.Text = Result);
		}

		private static string CreatePathResultExplain(PathUpdateEventArgs arg)
		{
			string filter = $"'{arg.Word.Content}'";
			if (arg.Word.CanSubstitution)
				filter = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderSearchOverview_Or, filter, $"'{arg.Word.Substitution}'");
			if (!string.IsNullOrWhiteSpace(arg.MissionChar))
				filter = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderSearchOverview_MissionChar, filter, arg.MissionChar);
			string FilterText = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderSearchOverview, filter);
			string SpecialFilterText = "";
			string FindResult;
			string ElapsedTimeText = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderTookTime, arg.TimeMillis);
			if (arg.ResultType == PathFindResult.Found)
			{
				FindResult = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderFound, arg.TotalWordCount, arg.CalcWordCount);
			}
			else
			{
				if (arg.ResultType == PathFindResult.NotFound)
					FindResult = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderFoundButEmpty, arg.TotalWordCount);
				else
					FindResult = I18n.PathFinderError;
			}
			if (arg.Flags.HasFlag(PathFinderOptions.UseEndWord))
				SpecialFilterText += ", " + I18n.PathFinderEndWord;
			if (arg.Flags.HasFlag(PathFinderOptions.UseAttackWord))
				SpecialFilterText += ", " + I18n.PathFinderAttackWord;

			string newSpecialFilterText = string.IsNullOrWhiteSpace(SpecialFilterText) ? string.Empty : string.Format(CultureInfo.CurrentCulture, I18n.PathFinderIncludedWord, SpecialFilterText[2..]);
			return FilterText + Environment.NewLine + newSpecialFilterText + Environment.NewLine + FindResult + Environment.NewLine + ElapsedTimeText;
		}

		private void SubmitChat_Click(object? sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(ChatField.Text))
			{
				AutoKkutuMain.SendMessage(ChatField.Text);
				ChatField.Text = "";
			}
		}

		private void SubmitSearch_Click(object? sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(SearchField.Text))
			{
				AutoKkutuMain.StartPathFinding(new PresentedWord(SearchField.Text, false), AutoKkutuMain.Handler?.CurrentMissionChar ?? string.Empty, PathFinderOptions.ManualSearch);
				SearchField.Text = "";
			}
		}
	}
}
