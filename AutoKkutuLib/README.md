# AutoKkutu Library

실질적으로 AutoKkutu의 모든 기능들이 구현되어 있는 라이브러리 코드입니다.
필요하시다면 라이브러리 코드만 불러와서 사용하실 수도 있습니다.

## Dependency Graph

![#](Dependency-Graph.svg)

## TODOs

- [ ] Scrap `DatabaseEvents.cs` and alter it with something better one.
- [ ] Fix potential infinite-loop on `OnlineDictionaryCheckExtension.VerifyWordOnline`

## 안티 치트 개발자들을 위한 약간의 팁

지금 현재 구현체에 의하면 채팅창에 단어를 입력할 때 onkeydown, onkeypress, onkeyup 이벤트가 호출되지 않습니다. 이를 이용하면 자동 입력을 원천 봉쇄할 수 있습니다! (물론, 접근성 또는 입력 보조 유틸리티와의 호환성은 보장 못함)
물론 이 문제는 직접 윈도우 상에서 키보드 입력 자체를 시뮬레이션함으로써 완전히 해결할 수 있으나, 이를 구현하려 여러 번 시도할 때마다 키보드 입력 시뮬레이션 시 IME와의 작동 호환성 관련 문제로 골머리를 썩고 있습니다.
