# AutoKkutu Library

실질적으로 AutoKkutu의 모든 기능들이 구현되어 있는 라이브러리 코드입니다.
필요하시다면 라이브러리 코드만 불러와서 사용하실 수도 있습니다.

## Dependency Graph

![#](Dependency-Graph.svg)

## TODOs

- [ ] Split 'Game' and 'Path-finding' parts into two different external library. (To increase flexibility)
- [ ] Scrap `DatabaseEvents.cs` and alter it with something better one.
- [ ] Detach Hangul input simulating feature `Hangul/` and input simulation codes in AutoEnter to external library.
- [ ] Fix potential infinite-loop on `OnlineDictionaryCheckExtension.VerifyWordOnline`
