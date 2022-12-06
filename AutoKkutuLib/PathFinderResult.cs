namespace AutoKkutuLib;

public enum PathFindResult
{
	Found,
	NotFound,
	Error
}

// TODO: 미션 글자가 두 글자 이상일 경우에 대한 핸들링
public sealed record PathFinderParameter(PresentedWord Word, string MissionChar, PathFinderOptions Options, int MaxDisplayed);
