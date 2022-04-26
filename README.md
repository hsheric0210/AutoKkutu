# AutoKkutu - KKutu-Helper 기반 끄투 단어 추천 및 자동 입력기

[![Build status](https://ci.appveyor.com/api/projects/status/u0rt1gkhv7lbi7e4/branch/main?svg=true)](https://ci.appveyor.com/project/hsheric0210/autokkutu/branch/main)
![Issues](https://img.shields.io/github/issues/hsheric0210/AutoKkutu.svg)

AutoKkutu는 KKutu-Helper Release v5.6.8500버전을 개조하여 만들어졌습니다
(제작자가 리버싱 후 수정 허용함)

# 지원 기능
* 단어 자동 입력
	* 단어 입력 딜레이
		* 글자 수 비례 딜레이
		* 딜레이 시작 타이밍을 '나에게 턴이 돌아왔을 때'와 '마지막 단어를 입력한 이후' 둘 중에서 선택 가능
* '한방 단어 우선, 공격 단어 우선, 단어 길이 우선'과 같은 단어 검색 기준 설정 가능
* 입력되었던 단어 기반 데이터베이스 자동 업데이트 (새로운 단어 자동 추가, 존재하지 않는 단어 삭제) 기능
* 단어, 노드 일괄 추가, 파일로부터 추가, 이전 데이터베이스 파일 불러오기 기능

## 지원되는 게임 모드
* 끝말잇기
* 앞말잇기
* 가운뎃말잇기 (완벽하지 않음)
* 타자 대결
* 전체
* 자유
* 자유 끝말잇기

## 개발 언어
[![.NET](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)](https://docs.microsoft.com/ko-kr/dotnet/)
[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/ko-kr/dotnet/csharp/)

## 지원하는 데이터베이스 종류
* [![SQLite](https://img.shields.io/badge/SQLite-07405E?style=for-the-badge&logo=sqlite&logoColor=white)](https://www.sqlite.org/index.html)
* [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
* [![MySQL](https://img.shields.io/badge/MySQL-00000F?style=for-the-badge&logo=mysql&logoColor=white)](https://www.mysql.com/)
  * [![MariaDB](https://img.shields.io/badge/MariaDB-003545?style=for-the-badge&logo=mariadb&color=003545)](https://mariadb.org/)

# 현재 (공식적으로) 지원되는 사이트
* 이름 없는 끄투(https://kkutu.org/)
* 핑크끄투(https://kkutu.pink/)
* BF 끄투(https://bfkkutu.kr/)
* 끄투코리아(https://kkutu.co.kr/)
* 뮤직끄투(https://musickkutu.xyz/)

# 아이콘 출처
* [Waiting](https://icons8.com/icon/4LVMPYVBsSXd/waiting) icon by [Icons8](https://icons8.com)
* [Search More](https://icons8.com/icon/102557/search-mor) icon by [Icons8](https://icons8.com)
* [Broom](https://icons8.com/icon/Xnx8cxDef16O/broom) icon by [Icons8](https://icons8.com)
* [error](https://icons8.com/icon/103174/error) icon by [Icons8](https://icons8.com)
* [Attack](https://icons8.com/icon/8fgdm3cVkheA/attack) icon by [Icons8](https://icons8.com)
* [Skull](https://icons8.com/icon/mIIa0TRNmD4k/skull) icon by [Icons8](https://icons8.com)
* [mission](https://icons8.com/icon/cjURgjzPYDlN/mission) icon by [Icons8](https://icons8.com)
* [Warning](https://icons8.com/icon/5tH5sHqq0t2q/warning) icon by [Icons8](https://icons8.com)

# 관련 프로젝트
* [CefSharp](https://github.com/cefsharp/CefSharp/) - AutoKkutu는 CefSharp을(를) 기반으로 만들어졌습니다
* [Npgsql](https://github.com/npgsql/npgsql) - .NET용 PostgreSQL 접속 및 데이터 제공 라이브러리
* [Log4net](https://github.com/apache/logging-log4net/) - .NET용 로깅 라이브러리
* [MySqlConnector](https://github.com/mysql-net/MySqlConnector) - .NET용 MySQL 접속 및 데이터 제공 라이브러리
