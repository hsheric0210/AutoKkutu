namespace AutoKkutuLib.Browser;

public enum CommonNameRegistry
{
	None = 0,

	/// <summary>
	/// 주입된 함수들이 속할 전역 JavaScript 개체 이름
	/// </summary>
	/// <remarks>
	/// 예시: <c>window.s9TinU2gq05iep02R6q</c>
	/// </remarks>
	InjectionNamespace = 2023,

	/// <summary>
	/// 채팅 내용을 업데이트하는 전역 JavaScript 함수 이름
	/// </summary>
	UpdateChat,

	/// <summary>
	/// '전송' 버튼을 클릭하는 전역 JavaScript 함수 이름
	/// </summary>
	ClickSubmit,

	/// <summary>
	/// 현재 라운드의 Index 번호를 반환하는 전역 JavaScript 함수 이름
	/// </summary>
	RoundIndex,

	/// <summary>
	/// 현재 턴이 내 턴인지를 판단하고 그 결과를 반환하는 전역 JavaScript 함수 이름
	/// </summary>
	IsMyTurn,

	/// <summary>
	/// 현재 표시된 단어 조건 또는 단어를 반환하는 전역 JavaScript 함수 이름
	/// </summary>
	PresentedWord,

	/// <summary>
	/// 현재 라운드에서 입력된 단어가 틀렸는지, 그리고 그 단어를 반환하는 전역 JavaScript 함수 이름
	/// </summary>
	TurnError,

	/// <summary>
	/// 현재 게임 모드를 반환하는 전역 JavaScript 함수 이름
	/// </summary>
	GameMode,

	/// <summary>
	/// 현재 남은 턴 시간을 반환하는 전역 JavaScript 함수 이름
	/// </summary>
	TurnTime,

	/// <summary>
	/// 현재 남은 라운드 시간을 반환하는 전역 JavaScript 함수 이름
	/// </summary>
	RoundTime,

	/// <summary>
	/// 현재 제시된 단어 힌트(턴에서 단어를 잇지 못했을 때 제시됨)를 반환하는 전역 JavaScript 함수 이름
	/// </summary>
	TurnHint,

	/// <summary>
	/// 현재 턴의 미션 글자를 반환하는 전역 JavaScript 함수 이름
	/// </summary>
	MissionChar,

	/// <summary>
	/// 현재 라운드의 활성 단어 이력을 반환하는 전역 JavaScript 함수 이름
	/// </summary>
	WordHistory,

	/// <summary>
	/// 방 게임 모드 ID(1, 2, 3...)를 실제 게임 모드 문자열(KKT, ESS, ...)로 변환하는 전역 JavaScript 함수 이름
	/// </summary>
	RoomModeToGameMode,

	/// <summary>
	/// 키보드 입력(keydown, keyup) 이벤트를 호출하는 전역 JavaScript 함수 이름
	/// </summary>
	SendKeyEvents,

	/// <summary>
	/// 현재 채팅 내용을 반환하는 전역 JavaScript 함수 이름
	/// </summary>
	GetChat,

	/// <summary>
	/// 키보드 입력을 시뮬레이트하는 전역 JavaScript 함수 이름
	/// </summary>
	SimulateInput,

	/// <summary>
	/// 덮어씌워지기 전 원본 WebSocket 개체 백업 전역 변수 이름
	/// </summary>
	OriginalWebSocket,

	/// <summary>
	/// WebSocket 메세지 필터 필드 이름
	/// </summary>
	WebSocketFilter,

	/// <summary>
	/// AutoKkutu와 Selenium 간의 통신을 위한 WebSocket이 할당된 전역 변수 이름
	/// </summary>
	CommWebSocket,

	/// <summary>
	/// AutoKkutu와 Selenium 간의 통신을 위한 WebSocket의 데이터 버퍼 전역 변수 이름
	/// </summary>
	CommWebSocketBuffer,

	/// <summary>
	/// 덮어씌워지기 전 원본 WebSocket.send 인스턴스 전역 백업 변수 이름
	/// </summary>
	WebSocketNativeSend,

	/// <summary>
	/// 덮어씌워지기 전 원본 WebSocket.addEventListener 전역 인스턴스 전역 백업 변수 이름
	/// </summary>
	WebSocketNativeAddEventListener,

	/// <summary>
	/// 메시지 송신 관련 통신 전역 JavaScript 함수 이름
	/// </summary>
	/// <remarks>
	/// 이 함수를 통해 브라우저와 AutoKkutu 간의 통신이 이루어집니다.
	/// </remarks>
	CommunicateSend,

	/// <summary>
	/// 메시지 수신 관련 통신 전역 JavaScript 함수 이름
	/// </summary>
	/// <remarks>
	/// 이 함수를 통해 브라우저와 AutoKkutu 간의 통신이 이루어집니다.
	/// </remarks>
	CommunicateReceive,

	/// <summary>
	/// 웹소켓 PassThru 속성 이름
	/// </summary>
	/// <remarks>
	/// 만약 이 속성의 값이 <c>true</c>이라면 프로그램 측으로 WebSocket 이벤트를 전송하지 않고 그대로 원본 WebSocket처럼 처리됩니다.
	/// AutoKkutu와 Selenium 간의 통신을 위한 WebSocket에서 통신 이벤트가 또 다시 처리되어 무한 루프가 발생할 가능성을 없애기 위해 도입되었습니다.
	/// </remarks>
	WebSocketPassThru,

	/// <summary>
	/// 덮어씌워지기 전 원본 WebSocket 개체 프로토타입 백업 전역 변수 이름
	/// </summary>
	OriginalWebSocketPrototype,

	/// <summary>
	/// 주입된 WebSocket 메시지 핸들러 목록
	/// </summary>
	/// <remarks>
	/// <c>WebSocket.onmessage</c>가 여러 번 호출될 경우, 그 이전에 등록된 메시지 핸들러들을 등록 해제하여
	/// 같은 WebSocket 메시지가 여러 번 처리되지 않도록 하기 위해 고안되었습니다.
	/// </remarks>
	InjectedWebSocketMessageHandlerList,

	/// <summary>
	/// 현재 게임 개입에 필요한 전역 JavaScript 함수들이 모두 정상적으로 등록되었는지를 나타내는 전역 속성 이름
	/// </summary>
	FunctionsRegistered,

	/// <summary>
	/// (안티치트 등에 의해 덮어씌워지기 전) 원본 window.getComputedStyle() 개체 백업 전역 변수 이름
	/// </summary>
	GetComputedStyle,

	ConsoleLog,

	SetTimeout,

	SetInterval,

	DispatchEvent,

	GetElementsByClassName,

	QuerySelector,

	QuerySelectorAll,

	GetElementById,

	GetChatBox,

	AppendChat,

	RuleKeys,

	WebSocketHelperRegistered,

	RoomHeadModeCache,

	GameDisplayCache,

	MyInputDisplayCache,

	TurnTimeDisplayCache,

	RoundTimeDisplayCache,

	ChatBoxCache,

	ChatBtnCache,

	ShiftState,

	FocusChat,

	GetTurnIndex,

	GetUserId,

	GetGameSeq,

	GetWordLength,

	WordLengthDisplayCache
}
