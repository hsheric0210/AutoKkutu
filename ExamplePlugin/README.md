# Example Plugin

## 플러그인 개발 시 참고사항

1. **반드시 `PluginMain`이라는 이름의 메인 클래스를 가지고 있어야 합니다.**
2. 플러그인 이름(`PluginName`)에는 공백이 포함되지 않는 것이 좋습니다.
3. 플러그인을 통한 추가를 지원하는 기능들은 다음과 같습니다:
    - 입력기(`Enterer`): 게임에 입력을 전송하는 데 사용
    - DOM 핸들러(`DomHandler`): 브라우저 DOM을 분석하여 게임 진행 상황 파악
    - WebSocket 핸들러(`WebSocketHandler`): 게임이 사용하는 WebSocket을 스니핑하여 게임 진행 상황 파악
