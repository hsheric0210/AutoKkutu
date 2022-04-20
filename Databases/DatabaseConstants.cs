using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu.Databases
{
	public static class DatabaseConstants
	{
		public const string DatabaseFileName = "path.sqlite";

		public const string WordListName = "word_list";
		public const string EndWordListName = "endword_list";
		public const string ReverseEndWordListName = "reverse_endword_list";
		public const string KkutuEndWordListName = "kkutu_endword_list";
		public const string AttackWordListName = "attackword_list";
		public const string ReverseAttackWordListName = "reverse_attackword_list";
		public const string KkutuAttackWordListName = "kkutu_attackword_list";

		public const string LoadFromLocalSQLite = "SQLite 데이터베이스 불러오기";

		public const int QueryResultLimit = 128;

		public const int MissionCharIndexPriority = 256;
		public const int AttackWordIndexPriority = 512;
		public const int EndWordIndexPriority = 1280; // 공격 미션 단어보다 한방 단어를 우선하기 위함; (256 + 256) + (512 + 256)

		public static readonly string SQLiteDeduplicationQuery = $"DELETE FROM {WordListName} WHERE _rowid_ IN (SELECT _rowid_ FROM (SELECT _rowid_, ROW_NUMBER() OVER w as rnum FROM {WordListName} WINDOW w AS (PARTITION BY word ORDER BY _rowid_)) t WHERE t.rnum > 1);";

		public static string MissionWordOccurrenceFinder(string GetCheckMissionCharFuncName) => $@"
DELIMITER //
DROP PROCEDURE IF EXISTS {GetCheckMissionCharFuncName} //
CREATE FUNCTION {GetCheckMissionCharFuncName}(word VARCHAR(256), missionWord CHAR(1))
RETURNS INT
BEGIN
	RETURN ROUND(LENGTH(word) - LENGTH(REPLACE(TOLOWER(word), TOLOWER(_missionWord), "")) / LENGTH(missionWord));
END; //
DELIMITER ;
";
	}
}
