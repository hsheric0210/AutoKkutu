namespace AutoKkutuLib.Game.Enterer;
public enum EntererMode
{
	/// <summary>
	/// 키보드 입력 시뮬레이션 없이 (딜레이가 다 되는 즉시) 내용 전체를 한 번에 입력합니다.
	/// </summary>
	EnterImmediately,

	/// <summary>
	/// 키보드 시뮬레이션을 활성화하고 내용을 입력합니다.
	/// </summary>
	SimulateInputJavaScript,


	/// <summary>
	/// 운영 체제 단에서 Win32 API를 통해 키보드 입력을 시뮬레이트합니다.
	/// </summary>
	/// <remarks>
	/// AutoHotKey, AutoIt 등 여러 매크로 프로그램들이 사용하는 방식과 동일한 방식입니다.
	/// 운영 체제 단에서 키보드 입력 메시지가 처리되기 때문에, 브라우저 단에서 문제를 감지하는 것이 불가능합니다.
	/// 단, 사용 시 반드시 브라우저 창이 최상단에 떠 있어야 하며, 내용을 입력할 텍스트란이 focus되어 있어야 합니다.
	/// </remarks>
	SimulateInputWin32,

	/// <summary>
	/// NOT IMPLEMENTED YET
	/// 키보드 기능 지원이 존재하는 아두이노를 통해 입력을 시뮬레이트합니다.
	/// </summary>
	SimulateInputArduino
}

public static class AutoEnterModeExtension
{
	public static bool ShouldSimulateInput(this EntererMode mode) => mode != EntererMode.EnterImmediately;
}