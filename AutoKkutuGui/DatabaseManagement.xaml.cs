using Serilog;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using AutoKkutuLib.Database.Sqlite;
using AutoKkutuLib;
using AutoKkutuLib.Database.Jobs.Node;
using AutoKkutuLib.Database.Jobs.Word;
using AutoKkutuLib.Database.Jobs.DbCheck;

namespace AutoKkutuGui;

public partial class DatabaseManagement : Window
{
	private readonly AutoKkutu autoKkutu;

	public DatabaseManagement(AutoKkutu autoKkutu)
	{
		this.autoKkutu = autoKkutu;
		InitializeComponent();
		Title = "Data-base Management";
	}

	private void BatchWordJob(string content, BatchJobOptions options, WordFlags flags)
	{
		if (string.IsNullOrWhiteSpace(content))
			return;

		var wordList = content.Trim().Split(Environment.NewLine.ToCharArray());

		((BatchWordJob)(options.HasFlag(BatchJobOptions.Remove) ? new BatchWordDeletionJob(autoKkutu.Database, regexp: options.HasFlag(BatchJobOptions.Regexp)) : new BatchWordAdditionJob(autoKkutu.NodeManager, autoKkutu.Browser, flags, options.HasFlag(BatchJobOptions.Verify)))).Execute(wordList);
	}

	private void Batch_Submit_Click(object sender, RoutedEventArgs e) => BatchWordJob(Batch_Input.Text, GetBatchWordJobFlags(), GetBatchAddWordDatabaseAttributes());

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
			SqliteDatabaseHelper.LoadFromExternalSQLite(autoKkutu.Database, dialog.FileName);
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
			foreach (var fileName in dialog.FileNames)
			{
				if (!new FileInfo(fileName).Exists)
				{
					Log.Warning("File {fileName} doesn't exists.", fileName);
					continue;
				}

				try
				{
					builder.AppendLine(File.ReadAllText(fileName, Encoding.UTF8));
				}
				catch (IOException ioe)
				{
					Log.Error(ioe, "IOException occurred during reading word list file {fileName}.", fileName);
				}
			}

			BatchWordJob(builder.ToString(), GetBatchWordJobFlags(), GetBatchAddWordDatabaseAttributes());
		}
	}

	private NodeTypes GetSelectedNodeTypes()
	{
		var type = (NodeTypes)0;
		if (Node_EndWord.IsChecked ?? false)
			type |= NodeTypes.EndWord;
		if (Node_AttackWord.IsChecked ?? false)
			type |= NodeTypes.AttackWord;
		if (Node_Reverse_EndWord.IsChecked ?? false)
			type |= NodeTypes.ReverseEndWord;
		if (Node_Reverse_AttackWord.IsChecked ?? false)
			type |= NodeTypes.ReverseAttackWord;
		if (Node_Kkutu_EndWord.IsChecked ?? false)
			type |= NodeTypes.KkutuEndWord;
		if (Node_Kkutu_AttackWord.IsChecked ?? false)
			type |= NodeTypes.KkutuAttackWord;
		if (Node_KKT_EndWord.IsChecked ?? false)
			type |= NodeTypes.KKTEndWord;
		if (Node_KKT_AttackWord.IsChecked ?? false)
			type |= NodeTypes.KKTAttackWord;
		return type;
	}

	private void CheckDB_Start_Click(object sender, RoutedEventArgs e) => new DbCheckJob(autoKkutu.NodeManager).CheckDB(Use_OnlineDic.IsChecked ?? false, autoKkutu.Browser);

	private WordFlags GetBatchAddWordDatabaseAttributes()
	{
		var flags = WordFlags.None;

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

		// 쿵쿵따 한방 단어
		if (Batch_KKT_EndWord.IsChecked ?? false)
			flags |= WordFlags.KKTEndWord;

		// 쿵쿵따 공격 단어
		if (Batch_KKT_AttackWord.IsChecked ?? false)
			flags |= WordFlags.KKTAttackWord;

		return flags;
	}

	private BatchJobOptions GetBatchWordJobFlags()
	{
		var mode = BatchJobOptions.None;

		if (Batch_Mode_Remove.IsSelected || Batch_Mode_Remove_Regexp.IsSelected)
			mode |= BatchJobOptions.Remove;

		if (Batch_Mode_Remove_Regexp.IsSelected)
			mode |= BatchJobOptions.Regexp;

		// Verify-before-Add
		if (Batch_Verify.IsChecked ?? false)
			mode |= BatchJobOptions.Verify;

		return mode;
	}
	private void Node_Remove_Click(object sender, RoutedEventArgs e) => autoKkutu.Database.BatchRemoveNode(Node_Input.Text, GetSelectedNodeTypes());

	private void Node_Submit_Click(object sender, RoutedEventArgs e) => autoKkutu.Database.BatchAddNode(Node_Input.Text, GetSelectedNodeTypes());

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

		foreach (var folderName in folderNames)
		{
			if (!Directory.Exists(folderName))
			{
				Log.Warning("Folder {folderName} doesn't exists.", folderName);
				continue;
			}

			try
			{
				foreach (var fileName in Directory.EnumerateFiles(folderName, "*", SearchOption.AllDirectories))
				{
					try
					{
						builder.AppendLine(File.ReadAllText(fileName, Encoding.UTF8));
					}
					catch (IOException ioe)
					{
						Log.Error(ioe, "IOException during reading word list file {fileName}.", fileName);
					}
				}
			}
			catch (IOException ioe)
			{
				Log.Error(ioe, "Unable to enumerate files in folder {folderName}.", folderName);
			}
		}

		BatchWordJob(builder.ToString(), GetBatchWordJobFlags(), GetBatchAddWordDatabaseAttributes());
	}

	private void OnCloseRequested(object sender, EventArgs e) => Close();

	[Flags]
	private enum BatchJobOptions
	{
		None = 0 << 0,
		Remove = 1 << 0,
		Verify = 1 << 1,
		Regexp = 1 << 2
	}
}
