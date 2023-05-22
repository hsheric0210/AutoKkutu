using AutoKkutuLib.Extension;
using Serilog;

namespace AutoKkutuLib.Handlers.JavaScript.Handlers;

public abstract class JavaScriptHandlerBase : HandlerBase
{
	#region Frequently-used function names
	protected const string WriteInputFunc = "WriteInputFunc";

	protected const string ClickSubmitFunc = "ClickSubmitFunc";

	protected const string CurrentRoundIndexFunc = "CurrentRoundIndexFunc";
	#endregion

	private readonly Dictionary<string, string> RegisteredFunctionNames = new();

	public override BrowserBase Browser { get; }

	protected JavaScriptHandlerBase(BrowserBase browser) => Browser = browser;

	#region Handler implementation
	protected virtual string CurrentRoundIndexFuncCode => "return Array.from(document.querySelectorAll('#Middle > div.GameBox.Product > div > div.game-head > div.rounds label')).indexOf(document.querySelector('.rounds-current'));";

	public override bool IsGameInProgress
	{
		get
		{
			var display = EvaluateJS("document.getElementsByClassName('GameBox Product')[0].style.display", nameof(IsGameInProgress));
			var height = EvaluateJS("document.getElementsByClassName('GameBox Product')[0].style.height", nameof(IsGameInProgress));
			return string.IsNullOrWhiteSpace(display) ? !string.IsNullOrWhiteSpace(height) : !display.Equals("none", StringComparison.OrdinalIgnoreCase);
		}
	}

	public override bool IsMyTurn
	{
		get
		{
			var element = EvaluateJS("document.getElementsByClassName('game-input')[0]", nameof(IsMyTurn));
			if (string.Equals(element, "undefined", StringComparison.Ordinal))
				return false;

			var displayOpt = EvaluateJS("document.getElementsByClassName('game-input')[0].style.display", nameof(IsMyTurn));
			return !string.IsNullOrWhiteSpace(displayOpt) && !displayOpt.Equals("none", StringComparison.Ordinal);
		}
	}

	public override string PresentedWord => EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].textContent", nameof(PresentedWord)).Trim();

	public override string RoundText => EvaluateJS("document.getElementsByClassName('rounds-current')[0].textContent", nameof(RoundText)).Trim();

	public override int RoundIndex => EvaluateJSInt($"{GetRegisteredJSFunctionName(CurrentRoundIndexFunc)}()", nameof(RoundIndex));

	public override string UnsupportedWord => EvaluateJS("document.getElementsByClassName('game-fail-text')[0]", "GetUnsupportedWord") != "undefined" ? EvaluateJS("document.getElementsByClassName('game-fail-text')[0].textContent", nameof(UnsupportedWord)).Trim() : "";

	public override GameMode GameMode
	{
		get
		{
			var roomMode = EvaluateJS("document.getElementsByClassName('room-head-mode')[0].textContent", nameof(GameMode));
			if (!string.IsNullOrWhiteSpace(roomMode))
			{
				var trimmed = roomMode.Split('/')[0].Trim();
				switch (trimmed[(trimmed.IndexOf(' ', StringComparison.Ordinal) + 1)..])
				{
					case "앞말잇기":
						return GameMode.FirstAndLast;

					case "가운뎃말잇기":
						return GameMode.MiddleAndFirst;

					case "쿵쿵따":
						return GameMode.KungKungTta;

					case "끄투":
						return GameMode.Kkutu;

					case "전체":
						return GameMode.All;

					case "자유":
						return GameMode.Free;

					case "자유 끝말잇기":
						return GameMode.LastAndFirstFree;

					case "타자 대결":
						return GameMode.TypingBattle;
				}
			}
			return GameMode.LastAndFirst;
		}
	}

	public override float TurnTime
	{
		get
		{
			return float.TryParse(EvaluateJS("document.querySelector('[class=\"graph jjo-turn-time\"] > [class=\"graph-bar\"]').textContent", nameof(TurnTime)).TrimEnd('초'), out var time)
				? time
				: 150;
		}
	}

	public override float RoundTime
	{
		get
		{
			return float.TryParse(EvaluateJS("document.querySelector('[class=\"graph jjo-round-time\"] > [class=\"graph-bar round-extreme\"]').textContent", nameof(RoundTime)).TrimEnd('초'), out var time)
				? time
				: 150;
		}
	}

	public override string ExampleWord
	{
		get
		{
			var innerHTML = EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].innerHTML", nameof(ExampleWord));
			var content = EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].textContent", nameof(ExampleWord));
			return innerHTML.Contains("label", StringComparison.OrdinalIgnoreCase)
				&& innerHTML.Contains("color", StringComparison.OrdinalIgnoreCase)
				&& innerHTML.Contains("170,", StringComparison.Ordinal)
				&& content.Length > 1 ? content : "";
		}
	}

	public override string MissionChar => EvaluateJS("document.getElementsByClassName('items')[0].style.opacity", "GetMissionWord") == "1" ? EvaluateJS("document.getElementsByClassName('items')[0].textContent", nameof(MissionChar)).Trim() : "";

	public override string GetWordInHistory(int index)
	{
		if (index is < 0 or >= 6)
			throw new ArgumentOutOfRangeException($"index: {index}");
		return EvaluateJS($"document.getElementsByClassName('ellipse history-item expl-mother')[{index}]", nameof(GetWordInHistory)).Equals("undefined", StringComparison.OrdinalIgnoreCase)
			? ""
			: EvaluateJS($"document.getElementsByClassName('ellipse history-item expl-mother')[{index}].innerHTML", nameof(GetWordInHistory));
	}

	public override void UpdateChat(string input) => ExecuteJS($"document.querySelector('[id=\"Talk\"]').value='{input?.Trim()}'", nameof(UpdateChat));

	public override void ClickSubmit() => ExecuteJS("document.getElementById('ChatBtn').click()", nameof(ClickSubmit));
	#endregion

	#region Javascript function registration
	protected void RegisterJSFunction(string funcName, string funcArgs, string funcBody)
	{
		if (!RegisteredFunctionNames.ContainsKey(funcName))
			RegisteredFunctionNames[funcName] = $"__{Random.Shared.GenerateRandomString(64, true)}";

		var realFuncName = RegisteredFunctionNames[funcName];
		if (EvaluateJSBool($"typeof {realFuncName} != 'function'")) // check if already registered
		{
			if (EvaluateJSReturnError($"function {realFuncName}({funcArgs}) {{{funcBody}}}", out var error))
				Log.Error("Failed to register JavaScript function {funcName} : {error}", funcName, error);
			else
				Log.Information("Registered JavaScript function {funcName} : {realFuncName}()", funcName, realFuncName);
		}
	}

	protected string GetRegisteredJSFunctionName(string funcName) => RegisteredFunctionNames[funcName];

	public override void RegisterRoundIndexFunction() => RegisterJSFunction(CurrentRoundIndexFunc, "", CurrentRoundIndexFuncCode);
	#endregion

	#region Javascript execute methods
	protected void ExecuteJS(string javaScript, string? moduleName = null) => Browser.ExecuteJavaScript(javaScript, "Error on " + moduleName);

	protected bool EvaluateJSReturnError(string javaScript, out string error) => Browser.EvaluateJSAndGetError(javaScript, out error);

	protected string EvaluateJS(string javaScript, string? moduleName = null, string defaultResult = " ") => Browser.EvaluateJavaScript(javaScript, defaultResult, "Error on " + moduleName);

	protected int EvaluateJSInt(string javaScript, string? moduleName = null, int defaultResult = -1) => Browser.EvaluateJavaScriptInt(javaScript, defaultResult, "Error on " + moduleName);

	protected bool EvaluateJSBool(string javaScript, string? moduleName = null, bool defaultResult = false) => Browser.EvaluateJavaScriptBool(javaScript, defaultResult, "Error on " + moduleName);
	#endregion
}
