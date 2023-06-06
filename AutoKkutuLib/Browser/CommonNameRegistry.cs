namespace AutoKkutuLib.Browser;

public enum CommonNameRegistry
{
	None = 0,

	/// <summary>
	/// 랜덤 생성된 기본 JavaScript 네임스페이스 이름
	/// </summary>
	Namespace = 2023,

	/// <summary>
	/// 게임이 진행 중인지 상태를 반환하는 JavaScript 함수 이름
	/// </summary>
	GameInProgress,

	/// <summary>
	/// 채팅 내용을 업데이트하는 JavaScript 함수 이름
	/// </summary>
	UpdateChat,

	/// <summary>
	/// '전송' 버튼을 클릭하는 JavaScript 함수 이름
	/// </summary>
	ClickSubmit,

	/// <summary>
	/// 현재 라운드의 Index 번호를 반환하는 JavaScript 함수 이름
	/// </summary>
	RoundIndex,

	/// <summary>
	/// 현재 턴이 내 턴인지를 판단하고 그 결과를 반환하는 JavaScript 함수 이름
	/// </summary>
	IsMyTurn,

	/// <summary>
	/// 현재 표시된 단어 조건 또는 단어를 반환하는 JavaScript 함수 이름
	/// </summary>
	PresentedWord,

	/// <summary>
	/// 현재 라운드의 글자 또는 단어를 반환하는 JavaScript 함수 이름
	/// </summary>
	[Obsolete("RoundText는 AutoKkutu 내 그 어느 곳에서도 사용되지 않음. 따라서 DomHandler에서도 제거되는 것이 적절.")]
	RoundText,

	/// <summary>
	/// 현재 라운드에서 입력된 단어가 틀렸는지, 그리고 그 단어를 반환하는 JavaScript 함수 이름
	/// </summary>
	TurnError,

	/// <summary>
	/// 현재 게임 모드를 반환하는 JavaScript 함수 이름
	/// </summary>
	GameMode,

	/// <summary>
	/// 현재 남은 턴 시간을 반환하는 JavaScript 함수 이름
	/// </summary>
	TurnTime,

	/// <summary>
	/// 현재 남은 라운드 시간을 반환하는 JavaScript 함수 이름
	/// </summary>
	RoundTime,

	/// <summary>
	/// 현재 제시된 단어 힌트(턴에서 단어를 잇지 못했을 때 제시됨)를 반환하는 JavaScript 함수 이름
	/// </summary>
	TurnHint,

	/// <summary>
	/// 현재 턴의 미션 글자를 반환하는 JavaScript 함수 이름
	/// </summary>
	MissionChar,

	/// <summary>
	/// 현재 라운드의 활성 단어 이력을 반환하는 JavaScript 함수 이름
	/// </summary>
	WordHistories,

	/// <summary>
	/// 방 게임 모드 ID(1, 2, 3...)를 실제 게임 모드 문자열(KKT, ESS, ...)로 변환하는 JavaScript 함수 이름
	/// </summary>
	RoomModeToGameMode,

	/// <summary>
	/// 키보드 입력(keydown, keyup) 이벤트를 호출하는 JavaScript 함수 이름
	/// </summary>
	CallKeyEvent,

	/// <summary>
	/// 현재 채팅 내용을 반환하는 JavaScript 함수 이름
	/// </summary>
	GetChat,

	/// <summary>
	/// 키보드 입력을 시뮬레이트하는 JavaScript 함수 이름
	/// </summary>
	SimulateInput,

	/// <summary>
	/// wsHook 전역 객체 이름
	/// </summary>
	WsHook,

	/// <summary>
	/// 원본 WebSocket 개체 백업 전역 변수 이름
	/// </summary>
	WsOriginal,

	/// <summary>
	/// WebSocket 메세지 필터 필드 이름
	/// </summary>
	WsFilter,

	/// <summary>
	/// AutoKkutu와의 통신을 위한 WebSocket이 할당된 전역 변수 이름; Selenium 전용
	/// </summary>
	WsGlobal,

	/// <summary>
	/// AutoKkutu와의 통신을 위한 WebSocket의 데이터 버퍼 전역 변수 이름; Selenium 전용
	/// </summary>
	WsBuffer,

	/// <summary>
	/// CefSharp JavascriptBinding 전역 객체 이름
	/// </summary>
	JsbGlobal,

	/// <summary>
	/// CefSharp JavascriptBinding 객체 이름
	/// </summary>
	JsbObject,

}
