using AutoKkutu.Databases;
using AutoKkutu.Databases.SQLite;
using AutoKkutu.Utils;
using NLog;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using AutoKkutu.Constants;
using System.Globalization;

namespace AutoKkutu
{
	public partial class DatabaseManagement : Window
	{
		private static readonly Logger Logger = LogManager.GetLogger(nameof(DatabaseManagement));

		private readonly DatabaseWithDefaultConnection Database;

		public DatabaseManagement(DatabaseWithDefaultConnection database)
		{
			Database = database;
			InitializeComponent();
			Title = "Data-base Management";
		}

		public static void BatchWordJob(DatabaseWithDefaultConnection database, string content, BatchWordJobOptions mode, WordDatabaseAttributes flags)
		{
			if (database == null || string.IsNullOrWhiteSpace(content))
				return;

			string[] wordlist = content.Trim().Split(Environment.NewLine.ToCharArray());

			CommonDatabaseConnection connection = database.DefaultConnection;
			if (mode.HasFlag(BatchWordJobOptions.Remove))
				connection.BatchRemoveWord(wordlist);
			else
				connection.BatchAddWord(wordlist, mode, flags);
		}

		private void Batch_Submit_Click(object sender, RoutedEventArgs e) => BatchWordJob(Database, Batch_Input.Text, GetBatchWordJobFlags(), GetBatchAddWordDatabaseAttributes());

		private void Batch_Submit_DB_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				Title = "단어 목록을 불러올 외부 SQLite 데이터베이스 파일을 선택하세요",
				Multiselect = false,
				CheckPathExists = true,
				CheckFileExists = true
			};
			if (dialog.ShowDialog() ?? false)
				SQLiteDatabaseHelper.LoadFromExternalSQLite(Database.DefaultConnection, dialog.FileName);
		}

		private void Batch_Submit_File_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				Title = "단어 목록을 불러올 파일들을 선택하세요",
				Multiselect = true,
				CheckPathExists = true,
				CheckFileExists = true
			};
			if (dialog.ShowDialog() ?? false)
			{
				var builder = new StringBuilder();
				foreach (string fileName in dialog.FileNames)
				{
					if (!new FileInfo(fileName).Exists)
					{
						Logger.Warn(CultureInfo.CurrentCulture, "File {fileName} doesn't exists.", fileName);
						continue;
					}

					try
					{
						builder.AppendLine(File.ReadAllText(fileName, Encoding.UTF8));
					}
					catch (IOException ioe)
					{
						Logger.Error(ioe, CultureInfo.CurrentCulture, "IOException occurred during reading word list file {fileName}.", fileName);
					}
				}

				BatchWordJob(Database, builder.ToString(), GetBatchWordJobFlags(), GetBatchAddWordDatabaseAttributes());
			}
		}

		private NodeDatabaseAttributes GetSelectedNodeTypes()
		{
			var type = (NodeDatabaseAttributes)0;
			if (Node_EndWord.IsChecked ?? false)
				type |= NodeDatabaseAttributes.EndWord;
			if (Node_AttackWord.IsChecked ?? false)
				type |= NodeDatabaseAttributes.AttackWord;
			if (Node_Reverse_EndWord.IsChecked ?? false)
				type |= NodeDatabaseAttributes.ReverseEndWord;
			if (Node_Reverse_AttackWord.IsChecked ?? false)
				type |= NodeDatabaseAttributes.ReverseAttackWord;
			if (Node_Kkutu_EndWord.IsChecked ?? false)
				type |= NodeDatabaseAttributes.KkutuEndWord;
			if (Node_Kkutu_AttackWord.IsChecked ?? false)
				type |= NodeDatabaseAttributes.KkutuAttackWord;
			if (Node_KKT_EndWord.IsChecked ?? false)
				type |= NodeDatabaseAttributes.KKTEndWord;
			if (Node_KKT_AttackWord.IsChecked ?? false)
				type |= NodeDatabaseAttributes.KKTAttackWord;
			return type;
		}

		private void BatchAddNode(bool remove) => Database.DefaultConnection.BatchAddNode(Node_Input.Text, remove, GetSelectedNodeTypes());

		private void CheckDB_Start_Click(object sender, RoutedEventArgs e) => Database.CheckDB(Use_OnlineDic.IsChecked ?? false);

		private WordDatabaseAttributes GetBatchAddWordDatabaseAttributes()
		{
			WordDatabaseAttributes flags = WordDatabaseAttributes.None;

			// 한방 단어
			if (Batch_EndWord.IsChecked ?? false)
				flags |= WordDatabaseAttributes.EndWord;

			// 공격 단어
			if (Batch_AttackWord.IsChecked ?? false)
				flags |= WordDatabaseAttributes.AttackWord;

			// 앞말잇기 한방 단어
			if (Batch_Reverse_EndWord.IsChecked ?? false)
				flags |= WordDatabaseAttributes.ReverseEndWord;

			// 앞말잇기 공격 단어
			if (Batch_Reverse_AttackWord.IsChecked ?? false)
				flags |= WordDatabaseAttributes.ReverseAttackWord;

			// 가운뎃말잇기 한방 단어
			if (Batch_Middle_EndWord.IsChecked ?? false)
				flags |= WordDatabaseAttributes.MiddleEndWord;

			// 가운뎃말잇기 공격 단어
			if (Batch_Middle_AttackWord.IsChecked ?? false)
				flags |= WordDatabaseAttributes.MiddleAttackWord;

			// 끄투 한방 단어
			if (Batch_Kkutu_EndWord.IsChecked ?? false)
				flags |= WordDatabaseAttributes.KkutuEndWord;

			// 끄투 공격 단어
			if (Batch_Kkutu_AttackWord.IsChecked ?? false)
				flags |= WordDatabaseAttributes.KkutuAttackWord;

			// 쿵쿵따 한방 단어
			if (Batch_KKT_EndWord.IsChecked ?? false)
				flags |= WordDatabaseAttributes.KKTEndWord;

			// 쿵쿵따 공격 단어
			if (Batch_KKT_AttackWord.IsChecked ?? false)
				flags |= WordDatabaseAttributes.KKTAttackWord;

			return flags;
		}

		private BatchWordJobOptions GetBatchWordJobFlags()
		{
			BatchWordJobOptions mode = BatchWordJobOptions.None;

			// Remove
			if (Batch_Remove.IsChecked ?? false)
				mode |= BatchWordJobOptions.Remove;

			// Verify-before-Add
			if (Batch_Verify.IsChecked ?? false)
				mode |= BatchWordJobOptions.VerifyBeforeAdd;

			return mode;
		}

		private void Node_Remove_Click(object sender, RoutedEventArgs e) => BatchAddNode(true);

		private void Node_Submit_Click(object sender, RoutedEventArgs e) => BatchAddNode(false);

		private void OnWordFolderSubmit(object sender, RoutedEventArgs e)
		{
			var builder = new StringBuilder();
			IEnumerable<string>? folderNames = null;
			using (var dialog = new CommonOpenFileDialog())
			{
				dialog.Title = "단어 목록을 불러올 파일들이 들어 있는 폴더들을 선택하세요 (주의: 하위 폴더에 있는 모든 파일까지 포함됩니다)";
				dialog.Multiselect = true;
				dialog.EnsurePathExists = true;
				dialog.IsFolderPicker = true;
				if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
					folderNames = dialog.FileNames;
			}

			if (folderNames?.Any() != true)
				return;

			foreach (string folderName in folderNames)
			{
				if (!Directory.Exists(folderName))
				{
					Logger.Warn("Folder {folderName} doesn't exists.", folderName);
					continue;
				}

				try
				{
					foreach (string fileName in Directory.EnumerateFiles(folderName, "*", SearchOption.AllDirectories))
					{
						try
						{
							builder.AppendLine(File.ReadAllText(fileName, Encoding.UTF8));
						}
						catch (IOException ioe)
						{
							Logger.Error(ioe, CultureInfo.CurrentCulture, "IOException during reading word list file {fileName}.", fileName);
						}
					}
				}
				catch (IOException ioe)
				{
					Logger.Error(ioe, CultureInfo.CurrentCulture, "Unable to enumerate files in folder {folderName}.", folderName);
				}
			}

			BatchWordJob(Database, builder.ToString(), GetBatchWordJobFlags(), GetBatchAddWordDatabaseAttributes());
		}

		private void OnCloseRequested(object sender, EventArgs e) => Close();
	}
}
