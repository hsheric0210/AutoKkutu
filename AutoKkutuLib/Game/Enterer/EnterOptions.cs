using System.Text;

namespace AutoKkutuLib.Game.Enterer;

public readonly struct EnterOptions : IEquatable<EnterOptions>
{
	/// <summary>
	/// 입력 딜레이를 사용할지의 여부를 나타냅니다.
	/// </summary>
	public readonly bool DelayEnabled { get; }

	/// <summary>
	/// 단어 입력을 시작하기 전에 기다릴 시간을 밀리초(ms) 단위로 나타냅니다.
	/// </summary>
	public readonly int StartDelay { get; } = 0;

	/// <summary>
	/// 단어 입력을 시작하기 전에 기다릴 시간의 랜덤화 정도를 백분율로 나타냅니다.
	/// </summary>
	/// <remarks>
	/// 일례로, 딜레이가 <c>200ms</c>이고 시작 딜레이 랜덤이 <c>50%</c>라면 실제 딜레이는 <c>150-250ms</c> 사이에서 랜덤하게 적용됩니다.
	/// </remarks>
	public readonly int StartDelayRandom { get; } = 0;

	/// <summary>
	/// 단어 입력 시 한 글자를 입력하고 나서 다음 글자를 입력하기까지 기다릴 시간을 밀리초(ms) 단위로 나타냅니다.
	/// </summary>
	public readonly int DelayBeforeNextChar { get; } = 0;

	/// <summary>
	/// 단어 입력 시 한 글자를 입력하고 나서 다음 글자를 입력하기까지 기다릴 시간의 랜덤화 정도를 백분율로 나타냅니다.
	/// </summary>
	/// <remarks>
	/// 일례로, 딜레이가 <c>50ms</c>이고 시작 딜레이 랜덤이 <c>50%</c>라면 실제 딜레이는 <c>25-75ms</c> 사이에서 랜덤하게 적용됩니다.
	/// </remarks>
	public readonly int DelayBeforeNextCharRandom { get; } = 0;

	/// <summary>
	/// 단어 입력 시 키를 누른 채로 유지할 시간(키를 누르고 떼기까지 걸리는 시간)을 밀리초(ms) 단위로 나타냅니다.
	/// </summary>
	public int DelayBeforeKeyUp { get; } = 0;

	/// <summary>
	/// 단어 입력 시 키를 누른 채로 유지할 시간(키를 누르고 떼기까지 걸리는 시간)의 랜덤화 정도를 백분율로 나타냅니다.
	/// </summary>
	/// <remarks>
	/// 일례로, 딜레이가 <c>50ms</c>이고 시작 딜레이 랜덤이 <c>50%</c>라면 실제 딜레이는 <c>25-75ms</c> 사이에서 랜덤하게 적용됩니다.
	/// </remarks>
	public int DelayBeforeKeyUpRandom { get; } = 0;

	/// <summary>
	/// 입력기에 넘길 추가적인 매개 변수 클래스를 나타냅니다.
	/// 기본값은 <c>null</c>이며, 매개 변수 타입은 기본적으로 입력기 클래스 내에 <c>Parameter</c>라는 이름을 가진 <c>struct</c>입니다.
	/// 만약 지원되지 않는 탕비의 매개 변수 클래스를 넘길 경우, 오류가 발생하는 대신 조용히 무시됩니다.
	/// </summary>
	public object? CustomParameters { get; } = null;

	public EnterOptions(
		bool delayEnabled,
		int startDelay,
		int startDelayRandom,
		int delayBeforeNextChar,
		int delayBeforeNextCharRandom,
		int delayBeforeKeyUp,
		int delayBeforeKeyUpRandom,
		object? customParameter)
	{
		DelayEnabled = delayEnabled;
		if (DelayEnabled)
		{
			StartDelay = startDelay;
			if (StartDelay > 0)
				StartDelayRandom = startDelayRandom;

			DelayBeforeNextChar = delayBeforeNextChar;
			if (DelayBeforeNextChar > 0)
				DelayBeforeNextCharRandom = delayBeforeNextCharRandom;

			DelayBeforeKeyUp = delayBeforeKeyUp;
			if (DelayBeforeKeyUp > 0)
				DelayBeforeKeyUpRandom = delayBeforeKeyUpRandom;

			CustomParameters = customParameter;
		}
	}

	private int GetDelay(int delay, int random)
	{
		if (!DelayEnabled)
			return 0;
		if (delay <= 0)
			return delay;
		return Random.Shared.Next(delay - delay * random / 100, delay + delay * random / 100 + 1); // +1 to bypass exclusive-maxValue restriction
	}

	private int GetMaxDelay(int delay, int random) => delay + delay * random / 100;

	private int GetMinDelay(int delay, int random) => delay - delay * random / 100;

	/// <summary>
	/// 입력 시작 지연 시간에 랜덤값을 적용하여 반환합니다.
	/// </summary>
	public int GetStartDelay() => GetDelay(StartDelay, StartDelayRandom);

	/// <summary>
	/// 다음 글자 입력 딜레이에 랜덤값을 적용하여 반환합니다.
	/// </summary>
	public int GetDelayBeforeNextChar() => GetDelay(DelayBeforeNextChar, DelayBeforeNextCharRandom);

	/// <summary>
	/// 키 입력 유지 시간에 랜덤값을 적용하여 반환합니다.
	/// </summary>
	public int GetDelayBeforeKeyUp() => GetDelay(DelayBeforeKeyUp, DelayBeforeKeyUpRandom);

	public int GetMaxStartDelay() => GetMaxDelay(StartDelay, StartDelayRandom);

	public int GetMaxDelayBeforeNextChar() => GetMaxDelay(DelayBeforeNextChar, DelayBeforeNextCharRandom);

	public int GetMaxDelayBeforeKeyUp() => GetMaxDelay(DelayBeforeKeyUp, DelayBeforeKeyUpRandom);

	public int GetMinStartDelay() => GetMinDelay(StartDelay, StartDelayRandom);

	public int GetMinDelayBeforeNextChar() => GetMinDelay(DelayBeforeNextChar, DelayBeforeNextCharRandom);

	public int GetMinDelayBeforeKeyUp() => GetMinDelay(DelayBeforeKeyUp, DelayBeforeKeyUpRandom);

	/// <summary>
	/// 주어진 단어에 대해 최악의 경우(입력 시작 시간과 글자 입력 간 지연, 키 입력 유지 시간이 최대)를 가정하고 최대 입력 지연 시간을 계산하여 반환합니다.
	/// </summary>
	/// <param name="input">입력하려는 내용</param>
	public int GetMaxDelay(string? input) => DelayEnabled ? GetMaxStartDelay() + (string.IsNullOrEmpty(input) ? 0 : input.Length * (GetMaxDelayBeforeNextChar() + GetMaxDelayBeforeKeyUp())) : 0;

	/// <summary>
	/// 주어진 단어에 대해 랜덤값 적용된 입력 시작 지연과 랜덤값 적용된 글자 입력 간 지연, 키 입력 유시 시간을 고려하여 적용될 것으로 예상되는 총 딜레이를 반환합니다.
	/// 랜덤값이 적용되었기에 호출 때마다 값이 달라진다는 점에 유의하세요.
	/// </summary>
	/// <param name="input">입력하려는 단어</param>
	public int GetDelayFor(string? input) => GetStartDelay() + (string.IsNullOrEmpty(input) ? 0 : input.Length * (GetDelayBeforeNextChar() + GetDelayBeforeKeyUp()));

	/// <summary>
	/// 주어진 단어에 대해 최적의 경우(입력 시작 시간과 글자 입력 간 지연 시간이 최소)를 가정하고 최소 입력 지연 시간을 계산하여 반환합니다.
	/// </summary>
	/// <param name="input">입력하려는 단어</param>
	public int GetMinDelay(string? input) => DelayEnabled ? GetMinStartDelay() + (string.IsNullOrEmpty(input) ? 0 : input.Length * (GetMinDelayBeforeNextChar() + GetMinDelayBeforeKeyUp())) : 0;

	public override string ToString()
	{
		var builder = new StringBuilder().Append(nameof(EnterOptions)).Append('{');
		builder.Append(nameof(DelayEnabled)).Append(": ").Append(DelayEnabled).Append(", ");
		builder.Append(nameof(StartDelay)).Append(": ").Append(StartDelay).Append(" (random: ").Append(StartDelayRandom).Append("%), ");
		builder.Append(nameof(DelayBeforeNextChar)).Append(": ").Append(DelayBeforeNextChar).Append(" (random: ").Append(DelayBeforeNextCharRandom).Append("%), ");
		builder.Append(nameof(DelayBeforeKeyUp)).Append(": ").Append(DelayBeforeKeyUp).Append(" (random: ").Append(DelayBeforeKeyUpRandom).Append("%), ");
		builder.Append(nameof(CustomParameters)).Append(": ").Append(CustomParameters);
		return builder.Append('}').ToString();
	}

	public override bool Equals(object? obj) => obj is EnterOptions options && Equals(options);
	public bool Equals(EnterOptions other) => DelayEnabled == other.DelayEnabled && StartDelay == other.StartDelay && StartDelayRandom == other.StartDelayRandom && DelayBeforeNextChar == other.DelayBeforeNextChar && DelayBeforeNextCharRandom == other.DelayBeforeNextCharRandom && DelayBeforeKeyUp == other.DelayBeforeKeyUp && DelayBeforeKeyUpRandom == other.DelayBeforeKeyUpRandom && EqualityComparer<object?>.Default.Equals(CustomParameters, other.CustomParameters);
	public override int GetHashCode() => HashCode.Combine(DelayEnabled, StartDelay, StartDelayRandom, DelayBeforeNextChar, DelayBeforeNextCharRandom, DelayBeforeKeyUp, DelayBeforeKeyUpRandom, CustomParameters);

	public static bool operator ==(EnterOptions left, EnterOptions right) => left.Equals(right);
	public static bool operator !=(EnterOptions left, EnterOptions right) => !(left == right);
}