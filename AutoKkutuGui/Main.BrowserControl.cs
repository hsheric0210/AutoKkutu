//#define SELENIUM
using AutoKkutuLib;
using AutoKkutuLib.Browser;
using AutoKkutuLib.Game;
using Serilog;
using System;
using System.Windows;

namespace AutoKkutuGui;

public partial class Main
{
	public void LoadFrontPage()
	{
		Browser.LoadFrontPage();
		BrowserInitialized?.Invoke(this, EventArgs.Empty);
	}

	public void NewFrameLoaded() => Browser.PageLoaded += Browser_PageLoaded;

	private void Browser_PageLoaded(object? sender, PageLoadedEventArgs args)
	{
		var serverHost = new Uri(args.Url).Host; // What a worst solution
		Log.Verbose("Browser Page Load: {host}", serverHost);

		if (!ServerConfig.TryGetServer(serverHost, out var serverInfo))
		{
			Log.Warning(I18n.Main_UnsupportedURL, serverHost);
			return;
		}

		var prevInstance = AutoKkutu;
		if (prevInstance != null)
		{
			// 동일한 서버에 대하여 이미 AutoKkutu 파사드가 존재할 경우, 재 초기화 방지
			if (prevInstance.HasSameHost(serverHost))
			{
				Log.Debug("Ignoring page load because it has same host to previous handler: {host}.", serverHost);
				return;
			}

			// 이전 AutoKkutu 파사드 제거
			prevInstance.Dispose();
			Log.Information("Disposed previous facade.");
		}

		// 서버에 대해 적절한 DOM Handler 탐색
		if (!DomHandlerManager.TryGetHandler(serverInfo.DomHandler, out var domHandler))
		{
			Log.Error("DOM Handler doesn't exists: {name}", serverInfo.DomHandler);
			return;
		}

		// 서버에 대해 적절한 WebSocket Handler 탐색
		if (!WebSocketHandlerManager.TryGetHandler(serverInfo.WebSocketHandler, out var webSocketHandler))
		{
			Log.Error("WebSocket Handler doesn't exists: {name}", serverInfo.WebSocketHandler);
			return;
		}

		// 서버에 대해 적절한 Database 연결 수립
		var db = InitializeDatabase(serverHost);
		if (db is null)
		{
			// TODO: DataBaseError 이벤트 트리거
			Log.Error("Failed to initialize database!");
			MessageBox.Show("Failed to initialize database!", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
			return;
		}

		Log.Information("Initializing new instance of AutoKkutu for {host}.", serverHost);

		var game = new Game(domHandler, webSocketHandler);
		game.Start();

		AutoKkutu = new AutoKkutu(serverHost, db, game);
		AutoKkutu.GameEnded += OnGameEnded;
		AutoKkutu.GameModeChanged += OnGameModeChange;
		AutoKkutu.TurnStarted += OnTurnStarted;
		AutoKkutu.PathRescanRequested += OnPathRescanRequested;
		AutoKkutu.TurnEnded += OnTurnEnded;
		AutoKkutu.TypingWordPresented += OnTypingWordPresented;
		AutoKkutu.UnsupportedWordEntered += OnMyPathIsUnsupported;
		AutoKkutu.RoundChanged += OnRoundChanged;
		AutoKkutuInitialized?.Invoke(this, new AutoKkutuInitializedEventArgs(AutoKkutu));

		// 정상적으로 파사드가 초기화되었다면, 페이지 로드 이벤트 등록 해제
		Browser.PageLoaded -= Browser_PageLoaded;
	}

	private void Browser_PageError(object? sender, PageErrorEventArgs args)
	{
		Log.Error("Browser load error: {error}", args.ErrorText);
	}
}
