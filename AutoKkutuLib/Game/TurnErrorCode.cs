namespace AutoKkutuLib.Game;
public enum TurnErrorCode
{
	None = 0,
	DatabaseError = 400,
	NoEndWordOnBegin = 402,
	EndWord = 403,
	NotFound = 404,
	Loanword = 405,
	Strict = 406,
	WrongSubject = 407,
	AlreadyUsed = 409
}