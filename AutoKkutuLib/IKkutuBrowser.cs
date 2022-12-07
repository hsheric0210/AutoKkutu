namespace AutoKkutuLib;
public interface IKkutuBrowser
{
	void Load(string url);
	void ShowDevTools();
	void ExecuteScriptAsync(string script);
	Task<JSResponse> EvaluateScriptAsync(string script);
}
