namespace AutoKkutuLib;

// TODO: 미션 글자가 두 글자 이상일 경우에 대한 핸들링
public sealed record PathFinderParameter(PresentedWord Word, string MissionChar, PathFinderFlags Options, bool ReuseAlreadyUsed, int MaxDisplayed);
