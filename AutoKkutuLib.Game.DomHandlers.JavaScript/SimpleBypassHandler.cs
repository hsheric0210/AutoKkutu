using AutoKkutuLib.Browser;
using Microsoft.Win32.SafeHandles;
using Serilog;

namespace AutoKkutuLib.Handlers.JavaScript;

internal class SimpleBypassHandler : JavaScriptHandlerBase
{
	public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] {
		new Uri("https://kkutu.co.kr/"),
		new Uri("https://bfkkutu.kr/")
	};

	public override string HandlerName => "Simple Fake-element Bypassing Handler";

	public SimpleBypassHandler(BrowserBase jsEvaluator) : base(jsEvaluator)
	{
	}

	public override async Task RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		var chatBoxCacheName = Browser.GenerateScriptTypeName(1023);
		var chatBtnCacheName = Browser.GenerateScriptTypeName(1024);
		await Browser.EvaluateJavaScriptAsync(@$"(function(){{if(window.{chatBoxCacheName}===undefined){{Object.defineProperty(window,'{chatBoxCacheName}',{{value:null,writable:true,configurable:true,enumerable:false}});}}
		if(window.{chatBtnCacheName}===undefined){{Object.defineProperty(window,'{chatBtnCacheName}',{{value:null,writable:true,configurable:true,enumerable:false}});}}}})()");
		// 어차피 웬만한 사이트들은 '게임 도중에 버튼 ID는 유지하고 display 여부만 바꿔치기'하지 않음. 덕분에 DOM Element를 미리 캐싱해놨다가 변경 발생 시에만 갱신할 수 있음.
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.UpdateChat, "input", $"let e;if(!window.{chatBoxCacheName}||!document.getElementById(window.{chatBoxCacheName}.id)){{e=window.{chatBoxCacheName}=Array.prototype.find.call(document.querySelectorAll('#Middle>div.ChatBox.Product>div.product-body>input'),e=>window.getComputedStyle(e).display!='none');window.jQueryGrand83506=e;}}else{{e=window.{chatBoxCacheName};}}if(e)e.value=input;");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.ClickSubmit, "", $"let e;if(!window.{chatBtnCacheName}||!document.getElementById(window.{chatBtnCacheName}.id)){{e=window.{chatBtnCacheName}=Array.prototype.find.call(document.querySelectorAll('#Middle>div.ChatBox.Product>div.product-body>button'),e=>window.getComputedStyle(e).display!='none');}}else{{e=window.{chatBtnCacheName};}};e?.click()");
		await base.RegisterInGameFunctions(alreadyRegistered);
	}
}
