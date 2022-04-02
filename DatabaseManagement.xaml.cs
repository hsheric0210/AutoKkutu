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
	/// <summary>
	/// DatabaseManagement.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class DatabaseManagement : Window
	{
		public ImageSource Favicon
		{
			get; set;
		}

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
		private const string INPUT_NODE_PLACEHOLDER = "노드를 입력하세요";
		private const string INPUT_AUTOMATIC_PLACEHOLDER = "단어 입력 (여러 줄 동시에 가능)";

		private static readonly string LOG_INSTANCE_NAME = "DatabaseManagement";

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
			if (!string.IsNullOrWhiteSpace(Node_Input.Text) && Node_Input.Text != INPUT_NODE_PLACEHOLDER)
			{
				try
				{
					DatabaseManager.AddEndWord(Node_Input.Text.Trim());
				}
				catch (Exception ex)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, string.Format("Failed to add Endword '{0}'! : {1}", Node_Input.Text[0], ex.ToString()), LOG_INSTANCE_NAME);
					MessageBox.Show(MANAGE_UNSUCCESSFUL, MANAGER_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
				ConsoleManager.Log(ConsoleManager.LogType.Error, string.Format("Successfully add Endword '{0}'!", Node_Input.Text[0]), LOG_INSTANCE_NAME);
				Node_Input.Text = "";
				MessageBox.Show(MANAGE_SUCCESSFUL, MANAGER_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}

		public static bool KkutuOnlineDictCheck(string i)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Find '" + i + "' in kkutu dict...", LOG_INSTANCE_NAME);
			EvaluateJS("document.getElementById('dict-input').value ='" + i + "'");
			EvaluateJS("document.getElementById('dict-search').click()");
			Thread.Sleep(1500);
			string result = EvaluateJS("document.getElementById('dict-output').innerHTML");
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Server Response : " + result, LOG_INSTANCE_NAME);
			if (string.IsNullOrWhiteSpace(result) || result == "404: 유효하지 않은 단어입니다.")
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Can't find '" + i + "' in kkutu dict.", LOG_INSTANCE_NAME);
				return false;
			}
			else
			{
				if (result == "검색 중")
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, "Invaild server response. Resend the request.", LOG_INSTANCE_NAME);
					return KkutuOnlineDictCheck(i);
				}
				else
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Successfully Find '" + i + "' in kkutu dict.", LOG_INSTANCE_NAME);
					return true;
				}
			}
		}

		public static async void BatchAddWord(string content, bool verify, bool endword)
		{
			// TODO: Batch Add from File feature
			// TODO: Batch Add End Words feature
			if (string.IsNullOrWhiteSpace(content))
				return;
			if (verify && string.IsNullOrWhiteSpace(EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", MANAGER_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			string[] WordList = content.Trim().Split();
			int SuccessCount = 0;
			int FailedCount = 0;
			ConsoleManager.Log(ConsoleManager.LogType.Info, $"Add {WordList.Length} element to queue.", LOG_INSTANCE_NAME);
			foreach (string i in WordList)
			{
				if (string.IsNullOrWhiteSpace(i))
					continue;
				i.Trim();
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Check kkutu dict : '" + i + "' .", LOG_INSTANCE_NAME);
				if (i.Length <= 1)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, "'" + i + "' is too short to add!", LOG_INSTANCE_NAME);
					FailedCount++;
				}
				else if (!verify || KkutuOnlineDictCheck(i))
				{
					bool isEndword = endword || PathFinder.EndWordList.Contains(i[i.Length - 1].ToString());

					if (isEndword)
						ConsoleManager.Log(ConsoleManager.LogType.Info, "'" + i + "' is End word.", LOG_INSTANCE_NAME);
					else
						ConsoleManager.Log(ConsoleManager.LogType.Info, "'" + i + "' isn't End word.", LOG_INSTANCE_NAME);

					try
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, "Adding'" + i + "' into database...", LOG_INSTANCE_NAME);
						DatabaseManager.AddWord(i, isEndword);
					}
					catch (Exception ex)
					{
						FailedCount++;
						ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to Add '" + i + "' to database : " + ex.ToString(), LOG_INSTANCE_NAME);
						continue;
					}
					SuccessCount++;
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Successfully Add '" + i + "' to database!", LOG_INSTANCE_NAME);
				}
			}
			ConsoleManager.Log(ConsoleManager.LogType.Info, $"Database Operation Complete. {SuccessCount} Success /  {FailedCount} Failed.", LOG_INSTANCE_NAME);
			MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{SuccessCount} 개 성공 / {FailedCount} 개 실패", MANAGER_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		private void Batch_Submit_Click(object sender, RoutedEventArgs e)
		{
			string i = Batch_Input.Text;
			BatchAddWord(i, Batch_Verify.IsChecked ?? false, Batch_EndWord.IsChecked ?? false);
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
			dialog.Title = "단어 목록을 불러올 파일들이 들어 있는 폴더을 선택하세요 (주의: 폴더 내의 파일들과 뿐만 아니라 그 하위 폴더에 모든 파일까지 포함됨)";
			dialog.Multiselect = true;
			dialog.EnsurePathExists = true;
			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				var builder = new StringBuilder();
				foreach (string foldername in dialog.FileNames)
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
				BatchAddWord(builder.ToString(), Batch_Verify.IsChecked ?? false, Batch_EndWord.IsChecked ?? false);
			}
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
