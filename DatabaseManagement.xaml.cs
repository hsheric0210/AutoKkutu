using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using log4net;
using static AutoKkutu.Constants;

namespace AutoKkutu
{
	public partial class DatabaseManagement : Window
	{
		private const string MANAGER_NAME = "데이터베이스 관리자";
		private const string MANAGE_SUCCESSFUL = "성공적으로 작업을 수행했습니다.";
		private const string MANAGE_UNSUCCESSFUL = "작업을 수행하는 도중 문제가 발생했습니다\n자세한 사항은 콘솔을 참조하십시오.";

		private static readonly ILog Logger = LogManager.GetLogger(nameof(DatabaseManagement));

		private CommonDatabase Database;

		public DatabaseManagement(CommonDatabase database)
		{
			Database = database;
			InitializeComponent();
			Title = $"Data-base Management";
		}

		public static void BatchWordJob(CommonDatabase database, string content, BatchWordJobFlags mode, WordFlags flags)
		{
			if (string.IsNullOrWhiteSpace(content))
				return;

			string[] wordlist = content.Trim().Split(Environment.NewLine.ToCharArray());

			if (mode.HasFlag(BatchWordJobFlags.Remove))
				database.BatchRemoveWord(wordlist);
			else
				database.BatchAddWord(wordlist, mode, flags);
		}

		private void Node_Submit_Click(object sender, RoutedEventArgs e)
		{
			BatchAddNode(false);
		}

		private void Node_Remove_Click(object sender, RoutedEventArgs e)
		{
			BatchAddNode(true);
		}

		private void BatchAddNode(bool remove)
		{
			NodeFlags GetSelectedNodeTypes()
			{
				NodeFlags type = (NodeFlags)0;
				if (Node_EndWord.IsChecked ?? false)
					type |= NodeFlags.EndWord;
				if (Node_AttackWord.IsChecked ?? false)
					type |= NodeFlags.AttackWord;
				if (Node_Reverse_EndWord.IsChecked ?? false)
					type |= NodeFlags.ReverseEndWord;
				if (Node_Reverse_AttackWord.IsChecked ?? false)
					type |= NodeFlags.ReverseAttackWord;
				if (Node_Kkutu_EndWord.IsChecked ?? false)
					type |= NodeFlags.KkutuEndWord;
				if (Node_Kkutu_AttackWord.IsChecked ?? false)
					type |= NodeFlags.KkutuAttackWord;
				return type;
			}

			var content = Node_Input.Text;
			if (string.IsNullOrWhiteSpace(content))
				return;

			Database.BatchAddNode(content, remove, GetSelectedNodeTypes());
		}

		private BatchWordJobFlags GetBatchWordJobFlags()
		{
			BatchWordJobFlags mode = BatchWordJobFlags.Default;

			// Remove
			if (Batch_Remove.IsChecked ?? false)
				mode |= BatchWordJobFlags.Remove;

			// Verify-before-Add
			if (Batch_Verify.IsChecked ?? false)
				mode |= BatchWordJobFlags.VerifyBeforeAdd;

			return mode;
		}

		private WordFlags GetBatchAddWordFlags()
		{
			WordFlags flags = WordFlags.None;

			// 한방 단어
			if (Batch_EndWord.IsChecked ?? false)
				flags |= WordFlags.EndWord;

			// 공격 단어
			if (Batch_AttackWord.IsChecked ?? false)
				flags |= WordFlags.AttackWord;

			// 앞말잇기 한방 단어
			if (Batch_Reverse_EndWord.IsChecked ?? false)
				flags |= WordFlags.ReverseEndWord;

			// 앞말잇기 공격 단어
			if (Batch_Reverse_AttackWord.IsChecked ?? false)
				flags |= WordFlags.ReverseAttackWord;

			// 가운뎃말잇기 한방 단어
			if (Batch_Middle_EndWord.IsChecked ?? false)
				flags |= WordFlags.MiddleEndWord;

			// 가운뎃말잇기 공격 단어
			if (Batch_Middle_AttackWord.IsChecked ?? false)
				flags |= WordFlags.MiddleAttackWord;

			// 끄투 한방 단어
			if (Batch_Kkutu_EndWord.IsChecked ?? false)
				flags |= WordFlags.KkutuEndWord;

			// 끄투 공격 단어
			if (Batch_Kkutu_AttackWord.IsChecked ?? false)
				flags |= WordFlags.KkutuAttackWord;

			return flags;
		}

		private void Batch_Submit_Click(object sender, RoutedEventArgs e)
		{
			BatchWordJob(Database, Batch_Input.Text, GetBatchWordJobFlags(), GetBatchAddWordFlags());
		}

		private void Batch_Submit_DB_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			dialog.Title = "단어 목록을 불러올 외부 SQLite 데이터베이스 파일을 선택하세요";
			dialog.Multiselect = false;
			dialog.CheckPathExists = true;
			dialog.CheckFileExists = true;
			if (dialog.ShowDialog() ?? false)
				Database.LoadFromExternalSQLite(dialog.FileName);
		}

		private void Batch_Submit_File_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			dialog.Title = "단어 목록을 불러올 파일들을 선택하세요";
			dialog.Multiselect = true;
			dialog.CheckPathExists = true;
			dialog.CheckFileExists = true;
			if (dialog.ShowDialog() ?? false)
			{
				var builder = new StringBuilder();
				foreach (string filename in dialog.FileNames)
				{
					if (!new FileInfo(filename).Exists)
					{
						Logger.WarnFormat("File '{0}' doesn't exists.", filename);
						continue;
					}

					try
					{
						builder.AppendLine(File.ReadAllText(filename, Encoding.UTF8));
					}
					catch (IOException ioe)
					{
						Logger.Error($"IOException occurred during reading word list files", ioe);
					}
				}

				BatchWordJob(Database, builder.ToString(), GetBatchWordJobFlags(), GetBatchAddWordFlags());
			}
		}

		private void Batch_Submit_Folder_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new CommonOpenFileDialog();
			dialog.Title = "단어 목록을 불러올 파일들이 들어 있는 폴더들을 선택하세요 (주의: 하위 폴더에 있는 모든 파일까지 포함됩니다)";
			dialog.Multiselect = true;
			dialog.EnsurePathExists = true;
			dialog.IsFolderPicker = true;
			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				var builder = new StringBuilder();
				foreach (string foldername in dialog.FileNames)
				{
					if (!Directory.Exists(foldername))
					{
						Logger.WarnFormat("'{0}' is not a folder.", foldername);
						continue;
					}

					try
					{
						foreach (string filename in Directory.EnumerateFiles(foldername, "*", SearchOption.AllDirectories))
							try
							{
								builder.AppendLine(File.ReadAllText(filename, Encoding.UTF8));
							}
							catch (IOException ioe)
							{
								Logger.Error($"IOException during reading word list files", ioe);
							}
					}
					catch (IOException ioe)
					{
						Logger.Error($"Unable to enumerate files in folder {foldername}", ioe);
					}
				}

				BatchWordJob(Database, builder.ToString(), GetBatchWordJobFlags(), GetBatchAddWordFlags());
			}
		}

		private void CheckDB_Start_Click(object sender, RoutedEventArgs e) => Database.CheckDB(Use_OnlineDic.IsChecked.Value);
	}
}
