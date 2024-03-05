using AutoKkutuLib.Browser;
using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Helper;
using AutoKkutuLib.Game;
using AutoKkutuLib.Path;

namespace AutoKkutuLib;

public partial class AutoKkutu : IDisposable
{
	private bool disposedValue;

	private string serverHost;

	#region Facade implementation - Module exposure
	public DbConnectionBase Database { get; }
	public IGame Game { get; }

	public PathFilter PathFilter { get; }
	public NodeManager NodeManager { get; }
	#endregion

	#region Module sub-element exposure wrapper (to enforce Law of Demeter)
	public BrowserBase Browser => Game.Browser;
	#endregion

	/// <summary>
	/// AutoKkutu 파사드 클래스를 생성합니다.
	/// <paramref name="dbConnection"/>에 해당하는 데이터베이스 연결은 초기화 이전에 이미 열려 있어야 하며,
	/// <paramref name="game"/>에 해당하는 게임 핸들러 인스턴스는 이미 시작된 상태(<c>Start</c> 함수가 호출된 상태)이어야 합니다.
	/// </summary>
	/// <param name="serverHost">대상으로 하는 서버의 호스트 주소</param>
	/// <param name="dbConnection">데이터베이스 연결 인스턴스</param>
	/// <param name="game">게임 핸들러 인스턴스</param>
	public AutoKkutu(string serverHost, DbConnectionBase dbConnection, IGame game)
	{
		this.serverHost = serverHost;

		Database = dbConnection;
		PathFilter = new PathFilter();
		NodeManager = new NodeManager(dbConnection);

		Game = game;

		RegisterInterconnections(game);
		RegisterEventRedirects(game);
	}

	public bool HasSameHost(string serverHost) => this.serverHost.Equals(serverHost, StringComparison.OrdinalIgnoreCase);

	public PathFinder CreatePathFinder() => new PathFinder(NodeManager, PathFilter);

	#region Disposal
	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			// Unregister game events
			if (disposing && Game != null)
			{
				UnregisterInterconnections(Game);
				UnregisterEventRedirects(Game);

				Game.Dispose();
				Database.Dispose();
			}

			disposedValue = true;
		}
	}

	/// <summary>
	/// 현재 AutoKkutu 파사드가 소유한 모든 리소스를 Dispose합니다.
	/// 생성자 파라미터로 넘어온 <c>dbConnection</c>과 <c>game</c> 역시 Dispose된다는 사실에 주의하세요.
	/// </summary>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	#endregion
}
