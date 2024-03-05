using AutoKkutuLib;
using AutoKkutuLib.Database;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Game;
using AutoKkutuLib.Game.Enterer;
using AutoKkutuLib.Hangul;
using AutoKkutuLib.Path;
using Serilog;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace AutoKkutuGui;

public partial class MainWindow : Window
{
	public const string ProgramVersion = "beta-1.2";

	// Succeed KKutu-Helper Release v5.6.8500
	private const string ProgramTitle = "AutoKkutu - Next-gen KKutu-Helper V";

	private readonly Notifier notifier;
	private readonly Main mainInstance;

	public MainWindow()
	{
		// Visual components setup
		InitializeComponent();
		Title = ProgramTitle;
		VersionLabel.Content = ProgramVersion;

		Log.Information(I18n.Main_StartLoad);
		LoadOverlay.Visibility = Visibility.Visible;

		// FIXME: Remove these 'DatabaseEvents' calls
		DatabaseEvents.DatabaseError += OnDataBaseError;
		DatabaseEvents.DatabaseIntegrityCheckStart += OnDatabaseIntegrityCheckStart;
		DatabaseEvents.DatabaseIntegrityCheckDone += OnDatabaseIntegrityCheckDone;
		DatabaseEvents.DatabaseImportStart += OnDatabaseImportStart;
		DatabaseEvents.DatabaseImportDone += OnDatabaseImportDone;

		var main = Main.GetInstance();
		main.PathListUpdated += Main_PathListUpdated;
		main.BrowserInitialized += Main_BrowserFrameLoad;
		main.AutoKkutuInitialized += Main_AutoKkutuInitialized;
		main.SearchStateChanged += Main_SearchStateChanged;
		main.StatusMessageChanged += Main_StatusMessageChanged;
		main.NoPathAvailable += Main_PathNotFound;
		main.AllPathTimeOver += Main_AllPathTimeOver;
		main.EntererManager.InputDelayApply += EntererManager_EnterDelaying;
		main.EntererManager.EnterFinished += EntererManager_EnterFinished;
		mainInstance = main;
		main.LoadFrontPage();
		CurrentURL.ItemsSource = main.ServerConfig.Servers.Select(server => server.FullUrl.ToString()).ToList();

		notifier = new Notifier(cfg =>
		{
			cfg.PositionProvider = new ControlPositionProvider(this, BrowserContainer, Corner.BottomRight, 15, 15);
			cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(TimeSpan.FromSeconds(3), MaximumNotificationCount.FromCount(6));
			cfg.Dispatcher = Dispatcher;
			cfg.DisplayOptions.Width = 250;
		});

		Win32InputSimulator.FocusToBrowser += Win32InputSimulator_FocusToBrowser;
	}

	private void Win32InputSimulator_FocusToBrowser(object? sender, EventArgs e)
	{
		Dispatcher.Invoke(() =>
		{
			if (BrowserContainer.Content is UIElement elem)
				elem.Focus();
		});
	}

	/* EVENTS: AutoEnter */

	private void Main_PathNotFound(object? sender, EventArgs args)
	{
		notifier.ShowWarning("No path available!");
		this.UpdateStatusMessage(StatusMessage.NotFound);
	}

	private void Main_AllPathTimeOver(object? sender, AllPathTimeOverEventArgs args)
	{
		notifier.ShowWarning($"No path enterable within turn time {args.RemainingTurnTime}ms!");
		this.UpdateStatusMessage(StatusMessage.AllWordTimeOver);
	}

	private void EntererManager_EnterDelaying(object? sender, InputDelayEventArgs args) => this.UpdateStatusMessage(StatusMessage.Delaying, args.Delay);

	private void EntererManager_EnterFinished(object? sender, EnterFinishedEventArgs args) => this.UpdateStatusMessage(StatusMessage.EnterFinished, args.Content);

	/* EVENTS: AutoKkutu */

	private void Main_PathListUpdated(object? sender, PathListUpdateEventArgs args) => Dispatcher.Invoke(() => PathList.ItemsSource = args.GuiPathList);

	private void Main_BrowserFrameLoad(object? sender, EventArgs args)
	{
		Dispatcher.Invoke(() =>
		{
			// Apply browser frame
			BrowserContainer.Content = mainInstance.Browser.BrowserControl;

			// Hide LoadOverlay
			LoadOverlay.Visibility = Visibility.Hidden;
		});
	}

	private void Main_AutoKkutuInitialized(object? sender, AutoKkutuInitializedEventArgs args)
	{
		this.UpdateStatusMessage(StatusMessage.Wait);
		UpdateSearchState(PathFindResult.Empty(PathDetails.Empty));
		Dispatcher.Invoke(() =>
		{
			// Update database icon
			var img = new BitmapImage();
			img.BeginInit();
			img.UriSource = new Uri($@"Images\{args.Instance.Database.DbType}.png", UriKind.Relative);
			img.EndInit();

			DBManager.IsEnabled = true;
			DBLogo.Source = img;
		});
	}

	private void Main_SearchStateChanged(object? sender, PathFindResultUpdateEventArgs args) => UpdateSearchState(args.Arguments);

	private void Main_StatusMessageChanged(object? sender, StatusMessageChangedEventArgs args) => this.UpdateStatusMessage(args.Status, args.GetFormatterArguments());

	/* EVENTS: Database */

	private void OnDatabaseIntegrityCheckDone(object? sender, DataBaseIntegrityCheckDoneEventArgs args) => this.UpdateStatusMessage(StatusMessage.DatabaseIntegrityCheckDone, args.Result);

	private void OnDatabaseIntegrityCheckStart(object? sender, EventArgs e) => this.UpdateStatusMessage(StatusMessage.DatabaseIntegrityCheck);

	private void OnDataBaseError(object? sender, EventArgs e) => this.UpdateStatusMessage(StatusMessage.Error);

	private void OnDatabaseImportDone(object? sender, DatabaseImportEventArgs args) => this.UpdateStatusMessage(StatusMessage.BatchJobDone, args.Name, args.Result);

	private void OnDatabaseImportStart(object? sender, DatabaseImportEventArgs args) => this.UpdateStatusMessage(StatusMessage.BatchJob, args.Name);

	/* EVENT: Hotkeys */

	private void OnToggleDelay(object? sender, RoutedEventArgs e) => Main.GetInstance().ToggleFeature(config => config.AutoEnterDelayEnabled = !config.AutoEnterDelayEnabled, StatusMessage.DelayToggled);

	private void OnToggleAllDelay(object? sender, RoutedEventArgs e) => Main.GetInstance().ToggleFeature(config => config.AutoEnterDelayEnabled = config.FixDelayEnabled = !config.AutoEnterDelayEnabled, StatusMessage.AllDelayToggled);

	private void OnToggleAutoEnter(object? sender, RoutedEventArgs e) => Main.GetInstance().ToggleFeature(config => config.AutoEnterEnabled = !config.AutoEnterEnabled, StatusMessage.AutoEnterToggled);

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
				mainInstance.SendMessage(clipboard);
		}
		catch (Exception ex)
		{
			Log.Warning(I18n.Main_ClipboardSubmitException, ex);
		}
	}

	private void OnDBManagementClicked(object? sender, RoutedEventArgs e) => new DatabaseManagement(mainInstance.AutoKkutu).Show();

	private void OnOpenDevConsoleClicked(object? sender, RoutedEventArgs e) => mainInstance.Browser.ShowDevTools();

	private void OnPathListContextMenuOpen(object? sender, ContextMenuEventArgs e)
	{
		var source = (FrameworkElement)e.Source;
		var contextMenu = source.ContextMenu;
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
		if (currentSelected is not GuiPathObject path)
			return;

		mainInstance.AutoKkutu.Database.MakeAttack(path.Underlying, mainInstance.AutoKkutu.Game.Session.GameMode);
	}

	private void OnPathListMakeEndClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not GuiPathObject path)
			return;

		mainInstance.AutoKkutu.Database.MakeEnd(path.Underlying, mainInstance.AutoKkutu.Game.Session.GameMode);
	}

	private void OnPathListMakeNormalClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not GuiPathObject path)
			return;

		mainInstance.AutoKkutu.Database.MakeNormal(path.Underlying, mainInstance.AutoKkutu.Game.Session.GameMode);
	}

	private void OnPathListQueueExcludedClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not GuiPathObject path)
			return;

		path.Underlying.Excluded = true;
		path.Underlying.RemoveQueued = false;
		var filter = mainInstance.AutoKkutu.PathFilter;
		filter.UnsupportedPaths.Add(path.Content);
		filter.InexistentPaths.Remove(path.Content);
		PathList.Items.Refresh();
	}

	private void OnPathListIncludeClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not GuiPathObject path)
			return;

		path.Underlying.Excluded = false;
		path.Underlying.RemoveQueued = false;
		var filter = mainInstance.AutoKkutu.PathFilter;
		filter.UnsupportedPaths.Remove(path.Content);
		filter.InexistentPaths.Remove(path.Content);
		PathList.Items.Refresh();
	}

	private void OnPathListQueueRemoveClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not GuiPathObject path)
			return;

		path.Underlying.Excluded = false;
		path.Underlying.RemoveQueued = true;
		var filter = mainInstance.AutoKkutu.PathFilter;
		filter.UnsupportedPaths.Add(path.Content);
		filter.InexistentPaths.Add(path.Content);
		PathList.Items.Refresh();
	}

	private void OnPathListCopyClick(object? sender, RoutedEventArgs e)
	{
		var currentSelected = PathList.SelectedItem;
		if (currentSelected is not GuiPathObject path)
			return;
		Clipboard.SetText(path.Content);
	}

	private void OnPathListMouseDoubleClick(object? sender, MouseButtonEventArgs e)
	{
		var selected = PathList.SelectedItem;
		if (selected is not GuiPathObject path)
			return;
		var content = path.Content;
		Log.Information(I18n.Main_PathSubmitted, content);
		mainInstance.SendMessage(content);
	}

	private void OnSettingsClick(object? sender, RoutedEventArgs e)
	{
		var wnd = new ConfigWindow(mainInstance.Preference);
		wnd.PreferenceUpdate += ConfigWindow_PreferenceUpdate;
		wnd.Show();
	}

	private void ConfigWindow_PreferenceUpdate(object? sender, PreferenceUpdateEventArgs e) => mainInstance.Preference = e.Preference;

	private void OnSubmitURLClick(object? sender, RoutedEventArgs e)
	{
		mainInstance.NewFrameLoaded();
		mainInstance.Browser.Load(CurrentURL.Text);
	}

	private void OnWindowClose(object? sender, CancelEventArgs e)
	{
		Log.Information(I18n.Main_ClosingDBConnection);
		try
		{
			mainInstance.AutoKkutu?.Dispose();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to dispose AutoKkutu instance.");
		}
		Log.CloseAndFlush();
	}

	private void SearchField_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key is Key.Enter or Key.Return)
			SubmitSearch_Click(sender, e);
	}

	private void UpdateSearchState(PathFindResult arg) => Dispatcher.Invoke(() => SearchResult.Text = CreatePathResultExplain(arg));

	private static string CreatePathResultExplain(PathFindResult arg)
	{
		var parameter = arg.Details;

		var filter = $"'{parameter.Condition.Char}'";
		if (parameter.Condition.SubAvailable)
			filter = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderSearchOverview_Or, filter, $"'{parameter.Condition.SubChar}'");
		if (!string.IsNullOrWhiteSpace(parameter.Condition.MissionChar))
			filter = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderSearchOverview_MissionChar, filter, parameter.Condition.MissionChar);

		var FilterText = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderSearchOverview, filter);
		var SpecialFilterText = "";

		var FindResult = arg.Result switch
		{
			PathFindResultType.Found => string.Format(CultureInfo.CurrentCulture, I18n.PathFinderFound, arg.FoundWordList.Count, arg.FilteredWordList.Count),
			PathFindResultType.NotFound => string.Format(CultureInfo.CurrentCulture, I18n.PathFinderFoundButEmpty, arg.FoundWordList.Count),
			PathFindResultType.EndWord => I18n.PathFinderUnavailable,
			_ => I18n.PathFinderError,
		};

		var ElapsedTimeText = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderTookTime, arg.TimeMillis);

		if (parameter.HasFlag(PathFlags.UseEndWord))
			SpecialFilterText += ", " + I18n.PathFinderEndWord;
		if (parameter.HasFlag(PathFlags.UseAttackWord))
			SpecialFilterText += ", " + I18n.PathFinderAttackWord;

		var newSpecialFilterText = string.IsNullOrWhiteSpace(SpecialFilterText) ? string.Empty : string.Format(CultureInfo.CurrentCulture, I18n.PathFinderIncludedWord, SpecialFilterText[2..]);
		return FilterText + Environment.NewLine + newSpecialFilterText + Environment.NewLine + FindResult + Environment.NewLine + ElapsedTimeText;
	}

	private void SubmitChat_Click(object? sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(ChatField.Text))
		{
			mainInstance.SendMessage(ChatField.Text);
			ChatField.Text = "";
		}
	}

	private void SubmitSearch_Click(object? sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(SearchField.Text))
		{
			GameMode gameMode;
			switch (SearchGameMode.SelectedIndex)
			{
				case 1:
					gameMode = GameMode.LastAndFirst;
					break;
				case 2:
					gameMode = GameMode.FirstAndLast;
					break;
				case 3:
					gameMode = GameMode.MiddleAndFirst;
					break;
				case 4:
					gameMode = GameMode.Kkutu;
					break;
				case 5:
					gameMode = GameMode.KungKungTta;
					break;
				case 6:
					gameMode = GameMode.AllKorean;
					break;
				case 7:
					gameMode = GameMode.AllEnglish;
					break;
				case 8:
					gameMode = GameMode.All;
					break;
				case 9:
					gameMode = GameMode.Free;
					break;
				case 10:
					gameMode = GameMode.LastAndFirstFree;
					break;
				case 11:
					gameMode = GameMode.Hunmin;
					break;
				default:
					gameMode = mainInstance.AutoKkutu.Game.Session.GameMode;
					break;
			}

			var missionChar = SearchMissionChar.Text;
			if (string.IsNullOrWhiteSpace(missionChar))
				missionChar = mainInstance.AutoKkutu.Game.Session.WordCondition.MissionChar;

			var details = new PathDetails(
				InitialLaw.ApplyInitialLaw(new WordCondition(SearchField.Text, missionChar: missionChar ?? "", regexp: RegexpSearch.IsChecked ?? false)),
				mainInstance.SetupPathFinderFlags(PathFlags.DoNotAutoEnter | PathFlags.DoNotCheckExpired),
				mainInstance.Preference.ReturnModeEnabled,
				mainInstance.Preference.MaxDisplayedWordCount);

			mainInstance.AutoKkutu.CreatePathFinder()
				.SetGameMode(gameMode)
				.SetPathDetails(details)
				.SetWordPreference(mainInstance.Preference.ActiveWordPreference)
				.BeginFind(mainInstance.OnPathUpdated);

			if (!(RegexpSearch.IsChecked ?? false)) // 힘들게 적은 정규표현식을 지워버리는 것 만큼 빡치는 일도 없음
				SearchField.Text = "";
		}
	}

	private void PreEnterNodeField_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key is Key.Enter or Key.Return)
			PreEnterButton_Click(sender, e);
	}

	private void PreEnterButton_Click(object? sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(PreEnterNodeField.Text))
		{
			// TODO: 페이지가 아직 로드되지 않은 등, 입력할 수 없는 상황에서는 입력 필드 자체를 비활성화하던지 아니면 입력 시 오류를 띄우던지
			mainInstance.PerformPreSearchAndPreInput(
				mainInstance.AutoKkutu.Game.Session.GameMode,
				new WordCondition(
					PreEnterNodeField.Text,
					missionChar: mainInstance.AutoKkutu.Game.Session.WordCondition.MissionChar
				));

			PreEnterNodeField.Text = "";
		}
	}
}
