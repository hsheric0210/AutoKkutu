using AutoKkutuGui.Constants;
using System;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoKkutuGui;

public static class StatusUtils
{
	public static void UpdateStatusMessage(this MainWindow window, StatusMessage status, params object?[] formatterArgs)
	{
		if (window == null)
			throw new ArgumentNullException(nameof(window));

		Color color;
		string explain;
		string image;
		switch (status)
		{
			case StatusMessage.Normal:
				color = ColorDefinitions.NormalColor;
				explain = "준비";
				image = "waiting";
				break;

			case StatusMessage.NotFound:
				color = ColorDefinitions.WarningColor;
				explain = "이 턴에 낼 수 있는 단어를 데이터 집합에서 찾을 수 없었습니다. 수동으로 입력하십시오.";
				image = "warning";
				break;

			case StatusMessage.AllWordTimeOver:
				color = ColorDefinitions.WarningColor;
				explain = "이번 턴 시간 {0}ms 안에 입력할 수 있는 단어가 없습니다!";
				image = "waiting";
				break;

			case StatusMessage.EndWord:
				color = ColorDefinitions.ErrorColor;
				explain = "한방 단어에 당했습니다!";
				image = "skull";
				break;

			case StatusMessage.Error:
				color = ColorDefinitions.ErrorColor;
				explain = "프로그램에 오류가 발생하였습니다! 자세한 사항은 콘솔을 참조하십시오.";
				image = "error";
				break;

			case StatusMessage.Searching:
				color = ColorDefinitions.WarningColor;
				explain = "단어 찾는 중...";
				image = "searching";
				break;

			case StatusMessage.EnterFinished:
				color = ColorDefinitions.NormalColor;
				explain = "단어 자동 입력됨: {0}";
				image = "ok";
				break;

			case StatusMessage.DatabaseIntegrityCheck:
				color = ColorDefinitions.WarningColor;
				explain = "데이터베이스 검증 작업 진행 중...";
				image = "cleaning";
				break;

			case StatusMessage.DatabaseIntegrityCheckDone:
				color = ColorDefinitions.NormalColor;
				explain = "데이터베이스 검증 작업 완료: {0}";
				image = "ok";
				break;

			case StatusMessage.BatchJob:
				color = ColorDefinitions.WarningColor;
				explain = "단어 일괄 추가 작업 중 ({0})...";
				image = "cleaning";
				break;

			case StatusMessage.BatchJobDone:
				color = ColorDefinitions.NormalColor;
				explain = "단어 일괄 추가 작업 ({0}) 완료: {1}";
				image = "ok";
				break;

			case StatusMessage.Delaying:
				color = ColorDefinitions.NormalColor;
				explain = "단어 찾음! 딜레이 대기 중: {0}ms";
				image = "waiting";
				break;

			case StatusMessage.AutoEnterToggled:
				color = ColorDefinitions.NormalColor;
				explain = "자동 입력 기능: {0}";
				image = "ok";
				break;

			case StatusMessage.DelayToggled:
				color = ColorDefinitions.NormalColor;
				explain = "딜레이: {0}";
				image = "ok";
				break;

			case StatusMessage.AllDelayToggled:
				color = ColorDefinitions.NormalColor;
				explain = "모든 종류의 딜레이: {0}";
				image = "ok";
				break;

			default:
				color = ColorDefinitions.WaitColor;
				explain = "게임 참가를 기다리는 중.";
				image = "waiting";
				break;
		}

		window.Dispatcher.Invoke(() =>
		{
			window.StatusGrid.Background = new SolidColorBrush(color);
			try
			{
				window.StatusLabel.Content = string.Format(CultureInfo.CurrentCulture, explain, formatterArgs);
			}
			catch
			{
				window.StatusLabel.Content = explain;
			}
			var img = new BitmapImage();
			img.BeginInit();
			img.UriSource = new Uri($@"images\{image}.png", UriKind.Relative);
			img.EndInit();
			window.StatusIcon.Source = img;
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
	EnterFinished,

	/// <summary>
	/// 단어를 찾을 수 없음
	/// </summary>
	NotFound,

	/// <summary>
	/// 제 시간 안에 입력 가능한 단어 찾을 수 없음
	/// </summary>
	AllWordTimeOver,

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
