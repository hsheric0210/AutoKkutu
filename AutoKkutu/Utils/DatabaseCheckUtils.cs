using AutoKkutu.Constants;
using AutoKkutu.Databases;
using AutoKkutu.Databases.Extension;
using AutoKkutu.Modules;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AutoKkutu.Utils
{
	public static class DatabaseCheckUtils
	{
		/// <summary>
		/// 데이터베이스의 무결성을 검증하고, 문제를 발견하면 수정합니다.
		/// </summary>
		/// <param name="checkOnlineDictionary">온라인 검사(끄투 사전을 통한 검사)를 진행하는지의 여부</param>
		public static void CheckDB(this PathDbContext context, bool checkOnlineDictionary)
		{
			if (checkOnlineDictionary && string.IsNullOrWhiteSpace(JSEvaluator.EvaluateJS(nameof(CheckDB), "document.getElementById('dict-output').style")))
			{
				MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 여십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			DatabaseEvents.TriggerDatabaseIntegrityCheckStart();

			Task.Run(() =>
			{
				try
				{
					var watch = new Stopwatch();

					int totalElementCount = context.Word.Count();
					Log.Information("Database has Total {0} elements.", totalElementCount);

					int currentElementIndex = 0, DeduplicatedCount = 0, RemovedCount = 0, FixedCount = 0;

					var delete = new List<string>();
					Dictionary<string, string> wordIndexCorrection = new(), reverseWordIndexCorrection = new(), kkutuIndexCorrection = new();
					var flagCorrection = new Dictionary<string, int>();

					Log.Information("Opening auxiliary connection...");
					using (PathDbContext secondaryContext = new PathDbContext(context.ProviderType, context.ConnectionString))
					{
						DbSet<WordModel> wordTable = secondaryContext.Word;

						// Deduplicate
						DeduplicatedCount = wordTable.DeduplicateDatabaseAndGetCount();

						// Refresh node lists
						secondaryContext.RefreshNodeLists();

						// Check for errorsd
						Log.Information("Searching problems...");

						watch.Start();
						foreach (WordModel word in wordTable) // for each all words
						{
							currentElementIndex++;
							string content = word.Word;

							// Check word validity
							if (IsWordInvalid(content))
							{
								Log.Information("Invalid word {word} will be removed.", content);
								delete.Add(content);
								continue;
							}

							// Online verify
							if (checkOnlineDictionary && !BatchJobUtils.CheckOnline(content.Trim()))
							{
								Log.Information("Inexistent-in-online word {word} will be removed.", content);
								delete.Add(content);
								continue;
							}

							// Check WordIndex tag
							ValidateWordIndex(word, WordIndexType.WordIndex, DatabaseUtils.GetLaFHeadNode, wordIndexCorrection);

							// Check ReverseWordIndex tag
							ValidateWordIndex(word, WordIndexType.ReverseWordIndex, DatabaseUtils.GetFaLHeadNode, reverseWordIndexCorrection);

							// Check KkutuIndex tag
							ValidateWordIndex(word, WordIndexType.KkutuWordIndex, DatabaseUtils.GetKkutuHeadNode, kkutuIndexCorrection);

							// Check Flags
							ValidateFlags(word, flagCorrection);
						}
						watch.Stop();
						Log.Information("Done searching problems. Took {0}ms.", watch.ElapsedMilliseconds);

						watch.Restart();

						// Start fixing
						RemovedCount += wordTable.DeleteWordRange(delete);
						FixedCount += wordTable.FixWordIndexRange(wordIndexCorrection, WordIndexType.WordIndex, DatabaseUtils.GetLaFHeadNode);
						FixedCount += wordTable.FixWordIndexRange(reverseWordIndexCorrection, WordIndexType.ReverseWordIndex, DatabaseUtils.GetFaLHeadNode);
						FixedCount += wordTable.FixWordIndexRange(kkutuIndexCorrection, WordIndexType.KkutuWordIndex, DatabaseUtils.GetKkutuHeadNode);
						FixedCount += wordTable.FixFlagRange(flagCorrection);

						secondaryContext.SaveChanges();
						watch.Stop();
						Log.Information("Done fixing problems. Took {0}ms.", watch.ElapsedMilliseconds);

						secondaryContext.Vacuum();
					}

					Log.Information("Database check completed: Total {0} / Removed {1} / Deduplicated {2} / Fixed {3}.", totalElementCount, RemovedCount, DeduplicatedCount, FixedCount);

					DatabaseEvents.TriggerDatabaseIntegrityCheckDone(new DataBaseIntegrityCheckDoneEventArgs($"{RemovedCount + DeduplicatedCount} 개 항목 제거됨 / {FixedCount} 개 항목 수정됨"));
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception while checking database");
				}
			});
		}

		/// <summary>
		/// (지원되는 DBMS에 한해) Vacuum 작업을 실행합니다.
		/// </summary>
		private static void Vacuum(this PathDbContext context)
		{
			var watch = new Stopwatch();
			Log.Information("Executing vacuum...");
			watch.Restart();
			context.ExecuteVacuum();
			watch.Stop();
			Log.Information("Vacuum took {0}ms.", watch.ElapsedMilliseconds);
		}

		private static int FixFlagRange(this DbSet<WordModel> table, Dictionary<string, int> correctionRange)
		{
			int counter = 0;
			foreach (var correction in correctionRange)
			{
				var elements = table.Where(w => string.Equals(w.Word, correction.Key)).ToList();
				foreach (var element in elements)
					element.Flags = correction.Value;
				counter += elements.Count;
			}
			return counter;
		}

		private static int FixWordIndexRange(this DbSet<WordModel> table, IDictionary<string, string> range, WordIndexType wordIndexType, Func<string, string>? indexSupplier)
		{
			int counter = 0;
			foreach (var (word, wordIndex) in range)
			{
				IList<WordModel> updateRange = table.Where(w => string.Equals(w.Word, word)).ToList();

				foreach (WordModel element in updateRange)
					element.SetWordIndex(wordIndexType, wordIndex);
				Log.Information("{funcName}: Reset {wordIndexType} of {word} to {wordIndexValue}.", nameof(FixWordIndexRange), wordIndexType, word, wordIndex);
				counter += updateRange.Count;
			}
			return counter;
		}

		private static int DeleteWordRange(this DbSet<WordModel> table, IEnumerable<string> range)
		{
			int counter = 0;
			foreach (string word in range)
			{
				IList<WordModel> removalRange = table.Where(w => string.Equals(w.Word, word)).ToList();
				table.RemoveRange(removalRange);

				Log.Information("{funcName}: Removed {word}", nameof(DeleteWordRange), word);
				counter += removalRange.Count;
			}
			return counter;
		}

		private static void ValidateFlags(WordModel word, IDictionary<string, int> correctionRange)
		{
			WordDbTypes calculatedFlags = DatabaseUtils.GetWordFlags(word.Word);
			int calculatedFlagsInt = (int)calculatedFlags;
			if (calculatedFlagsInt != word.Flags)
			{
				Log.Information("{funcName}: {word} has invalid flags {flags}, will be reset to {correctFlags}.", nameof(ValidateFlags), word.Word, (WordDbTypes)word.Flags, calculatedFlags, calculatedFlags);
				correctionRange.Add(word.Word, calculatedFlagsInt);
			}
		}

		private static void ValidateWordIndex(WordModel word, WordIndexType type, Func<string, string> correctIndexSupplier, IDictionary<string, string> correctionRange)
		{
			string correctWordIndex = correctIndexSupplier(word.Word);
			string wordIndex = word.GetWordIndex(type);
			if (!string.Equals(correctWordIndex, wordIndex, StringComparison.OrdinalIgnoreCase))
			{
				Log.Information("{funcName}: {word} has invalid word index {wordIndex} on {wordIndexType}, will be reset to {correctWordIndex}.", nameof(ValidateWordIndex), word.Word, wordIndex, type, correctWordIndex);
				correctionRange.Add(word.Word, wordIndex);
			}
		}

		/// <summary>
		/// 단어가 올바른 단어인지, 유효하지 않은 문자를 포함하고 있진 않은지 검사합니다.
		/// </summary>
		/// <param name="word">검사할 단어</param>
		/// <returns>단어가 유효할 시 false, 그렇지 않을 경우 true</returns>
		private static bool IsWordInvalid(string word)
		{
			if (word.Length == 1 || int.TryParse(word[0].ToString(), out int _))
				return true;

			char first = word[0];
			if (first is '(' or '{' or '[' or '-' or '.')
				return true;

			char last = word.Last();
			if (last is ')' or '}' or ']')
				return true;

			return word.Contains(' ', StringComparison.Ordinal) || word.Contains(':', StringComparison.Ordinal);
		}

		/// <summary>
		/// 단어 노드 목록들(한방 단어 노드 목록, 공격 단어 노드 목록 등)을 데이터베이스로부터 다시 로드합니다.
		/// </summary>
		private static void RefreshNodeLists(this PathDbContext context)
		{
			var watch = new Stopwatch();
			watch.Start();
			Log.Information("Updating node lists...");
			try
			{
				PathManager.UpdateNodeLists(context);
				watch.Stop();
				Log.Information("Done refreshing node lists. Took {0}ms.", watch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to refresh node lists");
			}
		}

		private static int DeduplicateDatabaseAndGetCount(this DbSet<WordModel> table)
		{
			int count = 0;
			var watch = new Stopwatch();
			watch.Start();
			Log.Information("Deduplicating entries...");
			try
			{
				// count = table.DeduplicateDatabase();
				watch.Stop();
				Log.Information("Removed {0} duplicate entries. Took {1}ms.", count, watch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Deduplication failed");
			}
			return count;
		}
	}
}
