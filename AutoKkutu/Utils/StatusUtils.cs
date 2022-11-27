using AutoKkutu.Constants;
using System;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoKkutu.Utils
{
	public static class StatusUtils
	{
		public static void UpdateStatusMessage(this MainWindow mainWindow, StatusMessage status, params object?[] formatterArgs)
		{
			if (mainWindow == null)
				throw new ArgumentNullException(nameof(mainWindow));

			Color StatusColor;
			string StatusContent;
			string ImageName;
			switch (status)
			{
				case StatusMessage.Normal:
					StatusColor = ColorDefinitions.NormalColor;
					StatusContent = "준비";
					ImageName = "waiting";
					break;

				case StatusMessage.NotFound:
					StatusColor = ColorDefinitions.WarningColor;
					StatusContent = "이 턴에 낼 수 있는 단어를 데이터 집합에서 찾을 수 없었습니다. 수동으로 입력하십시오.";
					ImageName = "warning";
					break;

				case StatusMessage.EndWord:
					StatusColor = ColorDefinitions.ErrorColor;
					StatusContent = "더 이상 이 턴에 낼 수 있는 단어가 없습니다.";
					ImageName = "skull";
					break;

				case StatusMessage.Error:
					StatusColor = ColorDefinitions.ErrorColor;
					StatusContent = "프로그램에 오류가 발생하였습니다. 자세한 사항은 콘솔을 참조하십시오.";
					ImageName = "error";
					break;

				case StatusMessage.Searching:
					StatusColor = ColorDefinitions.WarningColor;
					StatusContent = "단어 찾는 중...";
					ImageName = "searching";
					break;

				case StatusMessage.AutoEntered:
					StatusColor = ColorDefinitions.NormalColor;
					StatusContent = "단어 자동 입력됨: {0}";
					ImageName = "ok";
					break;

				case StatusMessage.DatabaseIntegrityCheck:
					StatusColor = ColorDefinitions.WarningColor;
					StatusContent = "데이터베이스 검증 작업 진행 중...";
					ImageName = "cleaning";
					break;

				case StatusMessage.DatabaseIntegrityCheckDone:
					StatusColor = ColorDefinitions.NormalColor;
					StatusContent = "데이터베이스 검증 작업 완료: {0}";
					ImageName = "ok";
					break;

				case StatusMessage.BatchJob:
					StatusColor = ColorDefinitions.WarningColor;
					StatusContent = "단어 일괄 추가 작업 중 ({0})...";
					ImageName = "cleaning";
					break;

				case StatusMessage.BatchJobDone:
					StatusColor = ColorDefinitions.NormalColor;
					StatusContent = "단어 일괄 추가 작업 ({0}) 완료: {1}";
					ImageName = "ok";
					break;

				case StatusMessage.Delaying:
					StatusColor = ColorDefinitions.NormalColor;
					StatusContent = "단어 찾음! 딜레이 대기 중: {0}ms";
					ImageName = "waiting";
					break;

				case StatusMessage.AutoEnterToggled:
					StatusColor = ColorDefinitions.NormalColor;
					StatusContent = "자동 입력 기능: {0}";
					ImageName = "ok";
					break;

				case StatusMessage.DelayToggled:
					StatusColor = ColorDefinitions.NormalColor;
					StatusContent = "딜레이: {0}";
					ImageName = "ok";
					break;

				case StatusMessage.AllDelayToggled:
					StatusColor = ColorDefinitions.NormalColor;
					StatusContent = "모든 종류의 딜레이: {0}";
					ImageName = "ok";
					break;

				default:
					StatusColor = ColorDefinitions.WaitColor;
					StatusContent = "게임 참가를 기다리는 중.";
					ImageName = "waiting";
					break;
			}

			mainWindow.Dispatcher.Invoke(() =>
			{
				mainWindow.StatusGrid.Background = new SolidColorBrush(StatusColor);
				mainWindow.StatusLabel.Content = string.Format(StatusContent, formatterArgs);
				var img = new BitmapImage();
				img.BeginInit();
				img.UriSource = new Uri($@"images\{ImageName}.png", UriKind.Relative);
				img.EndInit();
				mainWindow.StatusIcon.Source = img;
			});
		}
	}

	public enum StatusMessage
	{
		/// <summary>
		/// 준비됨
		/// </summary>
		Normal,

		/// <summary>
		/// 단어 검색 중
		/// </summary>
		Searching,

		/// <summary>
		/// 단어 자동 입력됨
		/// </summary>
		AutoEntered,

		/// <summary>
		/// 단어를 찾을 수 없음
		/// </summary>
		NotFound,

		/// <summary>
		/// 오류 발생
		/// </summary>
		Error,

		/// <summary>
		/// 한방 단어에 당함
		/// </summary>
		EndWord,

		/// <summary>
		/// 게임 참가 기다리는 중
		/// </summary>
		Wait,

		/// <summary>
		/// 데이터베이스 검사 중
		/// </summary>
		DatabaseIntegrityCheck,

		/// <summary>
		/// 데이터베이스 검사 완료
		/// </summary>
		DatabaseIntegrityCheckDone,

		/// <summary>
		/// 일괄 작업 중
		/// </summary>
		BatchJob,

		/// <summary>
		/// 일괄 작업 완료
		/// </summary>
		BatchJobDone,

		/// <summary>
		/// 딜레이 기다리는 중
		/// </summary>
		Delaying,

		/// <summary>
		/// 자동 입력 토글됨
		/// </summary>
		AutoEnterToggled,

		/// <summary>
		/// 딜레이 토글됨
		/// </summary>
		DelayToggled,

		/// <summary>
		/// 모든 딜레이 토글됨
		/// </summary>
		AllDelayToggled
	}
}
