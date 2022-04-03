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

namespace AutoKkutu
{
	// TODO: 좌상단의 '단어 추가' 기능은 좌하단의 단어 일괄 추가 기능으로 대체 가능하니, 부작용에 대해 잠깐만 생각해본 뒤 없애버리기
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

		private static readonly string LOG_INSTANCE_NAME = "DatabaseManagement";

		public static bool KkutuOnlineDictCheck(string word)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Find '" + word + "' in kkutu dict...", LOG_INSTANCE_NAME);
			EvaluateJS("document.getElementById('dict-input').value ='" + word + "'");
			EvaluateJS("document.getElementById('dict-search').click()");
			Thread.Sleep(1500);
			string result = EvaluateJS("document.getElementById('dict-output').innerHTML");
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Server Response : " + result, LOG_INSTANCE_NAME);
			if (string.IsNullOrWhiteSpace(result) || result == "404: 유효하지 않은 단어입니다.")
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Can't find '" + word + "' in kkutu dict.", LOG_INSTANCE_NAME);
				return false;
			}
			else
			{
				if (result == "검색 중")
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, "Invaild server response. Resend the request.", LOG_INSTANCE_NAME);
					return KkutuOnlineDictCheck(word);
				}
				else
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Successfully Find '" + word + "' in kkutu dict.", LOG_INSTANCE_NAME);
					return true;
				}
			}
		}

		public static async void BatchAddWord(string content, bool onlineVerify, bool forceEndword)
		{
			if (string.IsNullOrWhiteSpace(content))
				return;
			if (onlineVerify && string.IsNullOrWhiteSpace(EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", MANAGER_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			string[] WordList = content.Trim().Split(Environment.NewLine.ToCharArray());
			int SuccessCount = 0;
			int DuplicateCount = 0;
			int FailedCount = 0;
			int NewEndNode = 0;

			ConsoleManager.Log(ConsoleManager.LogType.Info, $"{WordList.Length} elements queued.", LOG_INSTANCE_NAME);
			foreach (string word in WordList)
			{
				if (string.IsNullOrWhiteSpace(word))
					continue;

				if (onlineVerify)
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Check kkutu dict : '" + word + "' .", LOG_INSTANCE_NAME);

				// Check word length
				if (word.Length <= 1)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, "'" + word + "' is too short to add!", LOG_INSTANCE_NAME);
					FailedCount++;
				}
				else if (!onlineVerify || KkutuOnlineDictCheck(word))
				{
					bool isEndword = PathFinder.EndWordList.Contains(word[word.Length - 1].ToString());

					if (forceEndword || isEndword)
						ConsoleManager.Log(ConsoleManager.LogType.Info, "'" + word + "' is End word.", LOG_INSTANCE_NAME);
					else
						ConsoleManager.Log(ConsoleManager.LogType.Info, "'" + word + "' isn't End word.", LOG_INSTANCE_NAME);

					try
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, "Adding'" + word + "' into database...", LOG_INSTANCE_NAME);
						if (DatabaseManager.AddWord(word, forceEndword || isEndword))
						{
							SuccessCount++;
							ConsoleManager.Log(ConsoleManager.LogType.Info, $"Successfully Add '{word}' to database!", LOG_INSTANCE_NAME);
						}
						else
						{
							DuplicateCount++;
							ConsoleManager.Log(ConsoleManager.LogType.Warning, $"'{word}' already exists on database", LOG_INSTANCE_NAME);
						}
						if (forceEndword && !isEndword)
						{
							// Add to endword dictionary
							string endnode = word.Last().ToString();
							PathFinder.EndWordList.Add(endnode);
							ConsoleManager.Log(ConsoleManager.LogType.Warning, $"Added new end word node '{endnode}", LOG_INSTANCE_NAME);
							NewEndNode++;
						}
					}
					catch (Exception ex)
					{
						// Check one or more exceptions are occurred during addition
						FailedCount++;
						ConsoleManager.Log(ConsoleManager.LogType.Error, $"Failed to Add '{word}' to database : {ex.ToString()}", LOG_INSTANCE_NAME);
						continue;
					}
				}
			}
			ConsoleManager.Log(ConsoleManager.LogType.Info, $"Database Operation Complete. {SuccessCount} Success / {NewEndNode} New end node / {DuplicateCount} Duplicated / {FailedCount} Failed.", LOG_INSTANCE_NAME);
			string StatusMessage;
			if (forceEndword)
				StatusMessage = $"{SuccessCount} 개 성공 / {NewEndNode} 개의 새로운 한방 끝말 / {DuplicateCount} 개 중복 / {FailedCount} 개 실패";
			else
				StatusMessage = $"{SuccessCount} 개 성공 / {DuplicateCount} 개 중복 / {FailedCount} 개 실패";
			MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{StatusMessage}", MANAGER_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
		private void Batch_Submit_EndWord(string content)
		{
			if (string.IsNullOrWhiteSpace(content) || content == INPUT_NODE_PLACEHOLDER)
				return;

			var NodeList = content.Trim().Split(Environment.NewLine.ToCharArray());

			int SuccessCount = 0;
			int DuplicateCount = 0;
			int FailedCount = 0;

			ConsoleManager.Log(ConsoleManager.LogType.Info, $"{NodeList.Length} elements queued.", LOG_INSTANCE_NAME);
			foreach (string node in NodeList)
			{
				if (string.IsNullOrWhiteSpace(node))
					continue;
				try
				{
					if (DatabaseManager.AddEndWord(node))
					{
						ConsoleManager.Log(ConsoleManager.LogType.Error, string.Format("Successfully add Endword '{0}'!", node[0]), LOG_INSTANCE_NAME);
						SuccessCount++;
					}
					else
					{
						ConsoleManager.Log(ConsoleManager.LogType.Warning, $"'{node[0]}' is already exists", LOG_INSTANCE_NAME);
						DuplicateCount++;
					}
				}
				catch (Exception ex)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, string.Format("Failed to add Endword '{0}'! : {1}", node[0], ex.ToString()), LOG_INSTANCE_NAME);
					FailedCount++;
				}
			}

			ConsoleManager.Log(ConsoleManager.LogType.Info, $"Database Operation Complete. {SuccessCount} Success / {DuplicateCount} Duplicated / {FailedCount} Failed.", LOG_INSTANCE_NAME);
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
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to run script on site. Expection : \n" + e2.ToString(), LOG_INSTANCE_NAME);
				return " ";
			}
		}

		// Button Handlers

		private void Manual_Submit_Click(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(Manual_Input.Text) && Manual_Input.Text != INPUT_KEYWORD_PLACEHOLDER)
			{
				try
				{
					DatabaseManager.AddWord(Manual_Input.Text.Trim(), Manual_EndWord.IsChecked.Value);
				}
				catch (Exception ex)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to add word '" + Manual_Input.Text.Trim() + "'! : " + ex.ToString(), LOG_INSTANCE_NAME);
					MessageBox.Show(MANAGE_UNSUCCESSFUL, MANAGER_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
				Manual_Input.Text = "";
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Successfully add word '" + Manual_Input.Text.Trim() + "'!", LOG_INSTANCE_NAME);
				MessageBox.Show(MANAGE_SUCCESSFUL, MANAGER_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}

		private void Node_Submit_Click(object sender, RoutedEventArgs e)
		{
			Batch_Submit_EndWord(Node_Input.Text);
		}

		
		private void Batch_Submit_Click(object sender, RoutedEventArgs e)
		{
			BatchAddWord(Batch_Input.Text, Batch_Verify.IsChecked ?? false, Batch_EndWord.IsChecked ?? false);
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
						ConsoleManager.Log(ConsoleManager.LogType.Error, $"IOException during reading word list files: {ioe}", LOG_INSTANCE_NAME);
					}
				BatchAddWord(builder.ToString(), Batch_Verify.IsChecked ?? false, Batch_EndWord.IsChecked ?? false);
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
						ConsoleManager.Log(ConsoleManager.LogType.Warning, $"'{foldername}' is not a folder; skipped", LOG_INSTANCE_NAME);
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
								ConsoleManager.Log(ConsoleManager.LogType.Error, $"IOException during reading word list files: {ioe}", LOG_INSTANCE_NAME);
							}
					}
					catch (IOException ioe)
					{
						ConsoleManager.Log(ConsoleManager.LogType.Error, $"Unable to enumerate files in folder {foldername}: {ioe}", LOG_INSTANCE_NAME);
					}
				}
				BatchAddWord(builder.ToString(), Batch_Verify.IsChecked ?? false, Batch_EndWord.IsChecked ?? false);
			}
		}

		private void Manual_Delete_Click(object sender, RoutedEventArgs e)
		{
			if (!(string.IsNullOrWhiteSpace(Manual_Input.Text) || Manual_Input.Text == INPUT_KEYWORD_PLACEHOLDER))
			{
				DatabaseManager.DeleteWord(Manual_Input.Text);
				Manual_Input.Text = "";
				MessageBox.Show("성공적으로 작업을 수행했습니다.\n자세한 사항은 콘솔을 참조하십시오.", MANAGER_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}

		private void CheckDB_Start_Click(object sender, RoutedEventArgs e) => DatabaseManager.CheckDB(Use_OnlineDic.IsChecked.Value);

		private void Manual_Input_GotFocus(object sender, RoutedEventArgs e)
		{
			if (Manual_Input.Text == INPUT_KEYWORD_PLACEHOLDER)
				Manual_Input.Text = "";
		}

		private void Manual_Input_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Manual_Input.Text))
				Manual_Input.Text = INPUT_KEYWORD_PLACEHOLDER;
		}

		private void Node_Input_GotFocus(object sender, RoutedEventArgs e)
		{
			if (Node_Input.Text == INPUT_NODE_PLACEHOLDER)
				Node_Input.Text = "";
		}

		private void Node_Input_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Node_Input.Text))
				Node_Input.Text = INPUT_NODE_PLACEHOLDER;
		}

		private void Batch_Input_GotFocus(object sender, RoutedEventArgs e)
		{
			if (Batch_Input.Text == INPUT_AUTOMATIC_PLACEHOLDER)
				Batch_Input.Text = "";
		}

		private void Batch_Input_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Batch_Input.Text))
				Batch_Input.Text = INPUT_AUTOMATIC_PLACEHOLDER;
		}
	}
}
