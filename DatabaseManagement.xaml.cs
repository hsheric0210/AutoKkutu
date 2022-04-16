using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using log4net;
using static AutoKkutu.DatabaseManager;
using static AutoKkutu.Constants;

namespace AutoKkutu
{
	public partial class DatabaseManagement : Window
	{
		public DatabaseManagement()
		{
			Title = "Data-base Management";
			InitializeComponent();
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
		}

		private const string MANAGER_NAME = "데이터베이스 관리자";
		private const string MANAGE_SUCCESSFUL = "성공적으로 작업을 수행했습니다.";
		private const string MANAGE_UNSUCCESSFUL = "작업을 수행하는 도중 문제가 발생했습니다\n자세한 사항은 콘솔을 참조하십시오.";
		private const string INPUT_KEYWORD_PLACEHOLDER = "단어를 입력하세요";
		private const string INPUT_NODE_PLACEHOLDER = "노드 입력 (여러 개는 줄바꿈으로 구분)";
		private const string INPUT_AUTOMATIC_PLACEHOLDER = "단어 입력 (여러 줄은 줄바꿈으로 구분)";

		private static readonly ILog Logger = LogManager.GetLogger("DatabaseManagement");

		public static EventHandler AddWordStart;
		public static EventHandler AddWordDone;

		public static bool KkutuOnlineDictCheck(string word)
		{
			Logger.Info("Find '" + word + "' in kkutu dict...");
			EvaluateJS("document.getElementById('dict-input').value ='" + word + "'");
			EvaluateJS("document.getElementById('dict-search').click()");
			Thread.Sleep(1500);
			string result = EvaluateJS("document.getElementById('dict-output').innerHTML");
			Logger.Info("Server Response : " + result);
			if (string.IsNullOrWhiteSpace(result) || result == "404: 유효하지 않은 단어입니다.")
			{
				Logger.Error("Can't find '" + word + "' in kkutu dict.");
				return false;
			}
			else
			{
				if (result == "검색 중")
				{
					Logger.Error("Invaild server response. Resend the request.");
					return KkutuOnlineDictCheck(word);
				}
				else
				{
					Logger.Info("Successfully Find '" + word + "' in kkutu dict.");
					return true;
				}
			}
		}

		public enum BatchAddWordMode
		{
			Add,
			Remove,
			VerifyAndAdd
		}

		public static void BatchAddWord(string content, BatchAddWordMode mode, WordFlags flags)
		{
			if (string.IsNullOrWhiteSpace(content))
				return;

			bool remove = mode == BatchAddWordMode.Remove;
			bool onlineVerify = mode == BatchAddWordMode.VerifyAndAdd;
			if (onlineVerify && string.IsNullOrWhiteSpace(EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", MANAGER_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (AddWordStart != null)
				AddWordStart(null, EventArgs.Empty);

			Task.Run(() =>
			{
				string[] WordList = content.Trim().Split(Environment.NewLine.ToCharArray());
				int SuccessCount = 0;
				int DuplicateCount = 0;
				int FailedCount = 0;
				int NewEndNode = 0;
				int NewAttackNode = 0;

				Logger.Info($"{WordList.Length} elements queued.");
				foreach (string word in WordList)
				{
					if (string.IsNullOrWhiteSpace(word))
						continue;

					if (!remove && onlineVerify)
						Logger.Info($"Check kkutu dict : '{word}' .");

					// Check word length
					if (!remove && word.Length <= 1)
					{
						Logger.Error($"'{word}' is too short to add!");
						FailedCount++;
					}
					else if (remove || !onlineVerify || KkutuOnlineDictCheck(word))
					{
						if (remove)
						{
							try
							{
								DatabaseManager.DeleteWord(word);
								SuccessCount++;
							}
							catch (Exception ex)
							{
								FailedCount++;
								Logger.Error($"Failed to remove '{word}' from database", ex);
								continue;
							}
						}
						else
						{
							try
							{
								Utils.CorrectFlags(word, ref flags, ref NewEndNode, ref NewAttackNode);

								Logger.Info($"Adding'{word}' into database... (flags: {flags})");
								if (DatabaseManager.AddWord(word, flags))
								{
									SuccessCount++;
									Logger.Info($"Successfully Add '{word}' to database!");
								}
								else
								{
									DuplicateCount++;
									Logger.Warn($"'{word}' already exists on database");
								}
							}
							catch (Exception ex)
							{
								// Check one or more exceptions are occurred during addition
								FailedCount++;
								Logger.Error($"Failed to add '{word}' to database", ex);
								continue;
							}
						}
					}
				}
				Logger.Info($"Database Operation Complete. {SuccessCount} success / {NewEndNode} new end nodes / {NewAttackNode} new attack nodes / {DuplicateCount} duplicated / {FailedCount} failed.");
				string StatusMessage;
				if (remove)
					StatusMessage = $"{SuccessCount} 개 성공적으로 제거 / {FailedCount} 개 제거 실패";
				else
					StatusMessage = $"{SuccessCount} 개 성공 / {NewEndNode} 개의 새로운 한방 노드 / {NewAttackNode} 개의 새로운 공격 노드 / {DuplicateCount} 개 중복 / {FailedCount} 개 실패";
				MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{StatusMessage}", MANAGER_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);
				if (AddWordDone != null)
					AddWordDone(null, EventArgs.Empty);
			});
		}

		private void BatchAddNode(string content, bool remove, NodeFlags type)
		{
			if (string.IsNullOrWhiteSpace(content) || content == INPUT_NODE_PLACEHOLDER)
				return;

			var NodeList = content.Trim().Split(Environment.NewLine.ToCharArray());

			int SuccessCount = 0;
			int DuplicateCount = 0;
			int FailedCount = 0;

			Logger.Info($"{NodeList.Length} elements queued.");
			foreach (string node in NodeList)
			{
				if (string.IsNullOrWhiteSpace(node))
					continue;
				try
				{
					if (remove)
					{
						SuccessCount += DatabaseManager.DeleteNode(node, type);
					}
					else if (DatabaseManager.AddNode(node, type))
					{
						Logger.Info(string.Format("Successfully add Endword '{0}'!", node[0]));
						SuccessCount++;
					}
					else
					{
						Logger.Warn($"'{node[0]}' is already exists");
						DuplicateCount++;
					}
				}
				catch (Exception ex)
				{
					Logger.Error($"Failed to add Endword '{node[0]}'!", ex);
					FailedCount++;
				}
			}

			Logger.Info($"Database Operation Complete. {SuccessCount} Success / {DuplicateCount} Duplicated / {FailedCount} Failed.");
			MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{SuccessCount} 개 성공 / {DuplicateCount} 개 중복 / {FailedCount} 개 실패", MANAGER_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		public static string EvaluateJS(string script)
		{
			try
			{
				return MainWindow.browser.EvaluateScriptAsync(script)?.Result?.Result?.ToString() ?? " ";
			}
			catch (NullReferenceException)
			{
				return " ";
			}
			catch (Exception e2)
			{
				Logger.Error("Failed to run script on site.", e2);
				return " ";
			}
		}

		// Button Handlers

		private NodeFlags GetSelectedNodeTypes()
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

		private void Node_Submit_Click(object sender, RoutedEventArgs e)
		{
			BatchAddNode(Node_Input.Text, false, GetSelectedNodeTypes());
		}

		private void Node_Remove_Click(object sender, RoutedEventArgs e)
		{
			BatchAddNode(Node_Input.Text, true, GetSelectedNodeTypes());
		}

		private BatchAddWordMode GetBatchAddWordMode()
		{
			BatchAddWordMode mode;
			if (Batch_Remove.IsChecked ?? false)
				mode = BatchAddWordMode.Remove;
			else if (Batch_Verify.IsChecked ?? false)
				mode = BatchAddWordMode.VerifyAndAdd;
			else
				mode = BatchAddWordMode.Add;
			return mode;
		}

		private WordFlags GetBatchAddWordFlags()
		{
			WordFlags flags = WordFlags.None;
			if (Batch_EndWord.IsChecked ?? false)
				flags |= WordFlags.EndWord;
			if (Batch_AttackWord.IsChecked ?? false)
				flags |= WordFlags.AttackWord;
			if (Batch_Reverse_EndWord.IsChecked ?? false)
				flags |= WordFlags.ReverseEndWord;
			if (Batch_Reverse_AttackWord.IsChecked ?? false)
				flags |= WordFlags.ReverseAttackWord;
			if (Batch_Middle_EndWord.IsChecked ?? false)
				flags |= WordFlags.MiddleEndWord;
			if (Batch_Middle_AttackWord.IsChecked ?? false)
				flags |= WordFlags.MiddleAttackWord;
			if (Batch_Kkutu_EndWord.IsChecked ?? false)
				flags |= WordFlags.KkutuEndWord;
			if (Batch_Kkutu_AttackWord.IsChecked ?? false)
				flags |= WordFlags.KkutuAttackWord;
			return flags;
		}

		private void Batch_Submit_Click(object sender, RoutedEventArgs e)
		{
			BatchAddWord(Batch_Input.Text, GetBatchAddWordMode(), GetBatchAddWordFlags());
		}

		private void Batch_Submit_DB_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			dialog.Title = "단어 목록을 불러올 데이터베이스 파일을 선택하세요";
			dialog.Multiselect = false;
			dialog.CheckPathExists = true;
			dialog.CheckFileExists = true;
			if (dialog.ShowDialog() ?? false)
				DatabaseManager.LoadFromDB(dialog.FileName);
		}

		private void Batch_Submit_File_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			dialog.Title = "단어 목록을 불러올 파일을 선택하세요";
			dialog.Multiselect = true;
			dialog.CheckPathExists = true;
			dialog.CheckFileExists = true;
			if (dialog.ShowDialog() ?? false)
			{
				var builder = new StringBuilder();
				foreach (string filename in dialog.FileNames)
					try
					{
						builder.AppendLine(File.ReadAllText(filename, Encoding.UTF8));
					}
					catch (IOException ioe)
					{
						Logger.Error($"IOException occurred during reading word list files", ioe);
					}
				BatchAddWord(builder.ToString(), GetBatchAddWordMode(), GetBatchAddWordFlags());
			}
		}

		private void Batch_Submit_Folder_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new CommonOpenFileDialog();
			dialog.Title = "단어 목록을 불러올 파일들이 들어 있는 폴더을 선택하세요 (주의: 하위 폴더에 있는 모든 파일까지 포함됩니다)";
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
						Logger.Warn($"'{foldername}' is not a folder; skipped");
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
				BatchAddWord(builder.ToString(), GetBatchAddWordMode(), GetBatchAddWordFlags());
			}
		}

		private void CheckDB_Start_Click(object sender, RoutedEventArgs e) => DatabaseManager.CheckDB(Use_OnlineDic.IsChecked.Value);

		private void Node_Input_GotFocus(object sender, RoutedEventArgs e)
		{
			if (Node_Input.Text == INPUT_NODE_PLACEHOLDER)
			{
				Node_Input.Text = "";
				Node_Input.FontStyle = FontStyles.Normal;
			}
		}

		private void Node_Input_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Node_Input.Text))
			{
				Node_Input.Text = INPUT_NODE_PLACEHOLDER;
				Node_Input.FontStyle = FontStyles.Italic;
			}
		}

		private void Batch_Input_GotFocus(object sender, RoutedEventArgs e)
		{
			if (Batch_Input.Text == INPUT_AUTOMATIC_PLACEHOLDER)
			{
				Batch_Input.Text = "";
				Batch_Input.FontStyle = FontStyles.Normal;
			}
		}

		private void Batch_Input_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Batch_Input.Text))
			{
				Batch_Input.Text = INPUT_AUTOMATIC_PLACEHOLDER;
				Batch_Input.FontStyle = FontStyles.Italic;
			}
		}
	}
}
