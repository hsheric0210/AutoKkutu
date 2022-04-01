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
			InitializeComponent();
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
		}

		private void Manual_Submit_Click(object sender, RoutedEventArgs e)
		{
			bool flag = string.IsNullOrWhiteSpace(Manual_Input.Text) || Manual_Input.Text == "단어를 입력하세요.";
			if (!flag)
			{
				try
				{
					DatabaseManager.AddWord(Manual_Input.Text.Trim(), Manual_EndWord.IsChecked.Value);
				}
				catch (Exception ex)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to add word '" + Manual_Input.Text.Trim() + "'! : " + ex.ToString(), _loginstancename);
					MessageBox.Show("작업을 수행하는 도중 문제가 발생했습니다\n자세한 사항은 콘솔을 참조하십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
				Manual_Input.Text = "";
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Successfully add word '" + Manual_Input.Text.Trim() + "'!", _loginstancename);
				MessageBox.Show("성공적으로 작업을 수행했습니다.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}

		private void Node_Submit_Click(object sender, RoutedEventArgs e)
		{
			bool flag = string.IsNullOrWhiteSpace(Node_Input.Text) || Node_Input.Text == "노드를 입력하세요.";
			if (!flag)
			{
				try
				{
					DatabaseManager.AddEndWord(Node_Input.Text.Trim());
				}
				catch (Exception ex)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, string.Format("Failed to add Endword '{0}'! : {1}", Node_Input.Text[0], ex.ToString()), _loginstancename);
					MessageBox.Show("작업을 수행하는 도중 문제가 발생했습니다\n자세한 사항은 콘솔을 참조하십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
				ConsoleManager.Log(ConsoleManager.LogType.Error, string.Format("Successfully add Endword '{0}'!", Node_Input.Text[0]), _loginstancename);
				Node_Input.Text = "";
				MessageBox.Show("성공적으로 작업을 수행했습니다.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}

		public static bool KkutuDicCheck(string i)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Find '" + i + "' in kkutu dict...", _loginstancename);
			ExecuteScript("document.getElementById('dict-input').value ='" + i + "'");
			ExecuteScript("document.getElementById('dict-search').click()");
			Thread.Sleep(1500);
			string result = ExecuteScript("document.getElementById('dict-output').innerHTML");
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Server Response : " + result, _loginstancename);
			bool flag = string.IsNullOrWhiteSpace(result) || result == "404: 유효하지 않은 단어입니다.";
			bool result2;
			if (flag)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Can't find '" + i + "' in kkutu dict.", _loginstancename);
				result2 = false;
			}
			else
			{
				bool flag2 = result == "검색 중";
				if (flag2)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, "Invaild server response. Resend the request.", _loginstancename);
					result2 = KkutuDicCheck(i);
				}
				else
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Successfully Find '" + i + "' in kkutu dict.", _loginstancename);
					result2 = true;
				}
			}
			return result2;
		}

		public static async void AutoAddWord(string content)
		{
			if (string.IsNullOrWhiteSpace(content))
			{
				return;
			}
			if (string.IsNullOrWhiteSpace(ExecuteScript("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			string[] WordList = content.Trim().Split();
			int SuccessCount = 0;
			int FailedCount = 0;
			ConsoleManager.Log(ConsoleManager.LogType.Info, $"Add {WordList.Length} element to queue.", _loginstancename);
			string[] array = WordList;
			foreach (string i in array)
			{
				if (string.IsNullOrWhiteSpace(i))
				{
					continue;
				}
				i.Trim();
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Check kkutu dict : '" + i + "' .", _loginstancename);
				if (i.Length <= 1)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, "'" + i + "' is too short to add!", _loginstancename);
					FailedCount++;
				}
				else if (KkutuDicCheck(i))
				{
					bool isEndword;
					if (PathFinder.EndWordList.Contains(i[i.Length - 1].ToString()))
					{
						isEndword = true;
						ConsoleManager.Log(ConsoleManager.LogType.Info, "'" + i + "' is End word.", _loginstancename);
					}
					else
					{
						isEndword = false;
						ConsoleManager.Log(ConsoleManager.LogType.Info, "'" + i + "' isn't End word.", _loginstancename);
					}
					try
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, "Adding'" + i + "' into database...", _loginstancename);
						DatabaseManager.AddWord(i, isEndword);
					}
					catch (Exception ex)
					{
						FailedCount++;
						ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to Add '" + i + "' to database : " + ex.ToString(), _loginstancename);
						continue;
					}
					SuccessCount++;
					ConsoleManager.Log(ConsoleManager.LogType.Error, "Successfully Add '" + i + "' to database!", _loginstancename);
				}
			}
			ConsoleManager.Log(ConsoleManager.LogType.Error, $"Database Operation Complete. {SuccessCount} Success /  {FailedCount} Failed.", _loginstancename);
			MessageBox.Show("성공적으로 작업을 수행했습니다. \n{SuccessCount} 개 성공 / {FailedCount} 개 실패", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		private void Auto_Submit_Click(object sender, RoutedEventArgs e)
		{
			string i = Auto_Input.Text;
			AutoAddWord(i);
		}

		public static string ExecuteScript(string script)
		{
			string result2;
			try
			{
				string result = MainWindow.browser.EvaluateScriptAsync(script, null, null).Result.Result.ToString();
				if (result == null)
					result2 = " ";
				else
					result2 = result;
			}
			catch (NullReferenceException)
			{
				result2 = " ";
			}
			catch (Exception e2)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to run script on site. Expection : \n" + e2.ToString(), _loginstancename);
				result2 = " ";
			}
			return result2;
		}

		private void Manual_Delete_Click(object sender, RoutedEventArgs e)
		{
			if (!(string.IsNullOrWhiteSpace(Manual_Input.Text) || Manual_Input.Text == "단어를 입력하세요."))
			{
				DatabaseManager.DeleteWord(Manual_Input.Text);
				Manual_Input.Text = "";
				MessageBox.Show("성공적으로 작업을 수행했습니다.\n자세한 사항은 콘솔을 참조하십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}

		private void CheckDB_Start_Click(object sender, RoutedEventArgs e) => DatabaseManager.CheckDB(Use_OnlineDic.IsChecked.Value);

		private static readonly string _loginstancename = "DatabaseManagement";
	}
}
