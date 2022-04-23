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

		public static readonly string DeduplicationQuery = $"DELETE FROM {WordListName} WHERE seq IN (SELECT seq FROM (SELECT seq, ROW_NUMBER() OVER w as rnum FROM {WordListName} WINDOW w AS (PARTITION BY word ORDER BY seq)) t WHERE t.rnum > 1);";
	}
}
