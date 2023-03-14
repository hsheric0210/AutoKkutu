# AutoKkutuLib.Database

AutoKkutu에서 데이터베이스에 접근할 때 사용하는 추상화 계층과 래퍼 클래스들이 정의된 네임스페이스입니다.

기본적으로 ORM으로 [Dapper](https://github.com/DapperLib/Dapper)를 사용하며, Dapper가 지원하는 데이터베이스들 중, 사용자 지정 함수를 정의하는 것을 허용하는 데이터베이스들은 모두 기본적으로 지원됩니다. (적절한 추가 반정규화 계층이 짜여졌을 경우에 한해서)


## TODO
- [ ] More robust and reliable database migration support
