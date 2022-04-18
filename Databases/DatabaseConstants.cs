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

		public const int QueryResultLimit = 128;

		public const int MissionCharIndexPriority = 256;
		public const int AttackWordIndexPriority = 512;
		public const int EndWordIndexPriority = 768;

		public static readonly string SQLiteDeduplicationQuery = $"DELETE FROM {WordListName} WHERE _rowid_ IN (SELECT _rowid_ FROM (SELECT _rowid_, ROW_NUMBER() OVER w as rnum FROM {WordListName} WINDOW w AS (PARTITION BY word ORDER BY _rowid_)) t WHERE t.rnum > 1);";
	}
}
