using System.Collections.Immutable;

namespace AutoKkutuLib.Hangul;

/// <summary>
/// 분해된 한글을 다시 재조합하여 입력하는 기능을 구한하는 클래스입니다.
/// </summary>
public sealed class HangulRecomposer
{
	private IImmutableList<HangulSplit> pieces;
	private KeyboardLayout layout;

	public HangulRecomposer(KeyboardLayout layout, IImmutableList<HangulSplit> pieces)
	{
		this.layout = layout;
		this.pieces = pieces;
		LibLogger.Debug<HangulRecomposer>("Recomposing pieces: {pieces}", string.Join(',', pieces));
	}

	// return (<prevSplitModified>, <newSplitGenerated>, <newInputGenerated>)
	private (HangulSplit, HangulSplit?, InputCommand) Append(HangulSplit prevSplit, InputCommand? lastInput, JamoType jamo, char ch)
	{
		(var keyboardKey, var requireShift) = layout.HangulToAlphabet(ch);
		var shiftState = requireShift ? ShiftState.Press : ShiftState.Release;
		HangulSplit? newSplit = null;
		InputCommand input;
		switch (jamo)
		{
			case JamoType.None:
				newSplit = HangulSplit.NonHangul(ch);
				var isUpperAlpha = ch is >= 'A' and <= 'Z';
				var isAlpha = ch is >= 'a' and <= 'z' || isUpperAlpha;
				input = InputCommand.KeyInput(isAlpha ? ImeState.English : ImeState.None, isUpperAlpha ? ShiftState.Press : ShiftState.Release, char.ToLowerInvariant(ch), $"{((HangulSplit)newSplit).Merge()}");
				break;
			case JamoType.Consonant:
			{
				if (prevSplit.HasInitialConsonant && prevSplit.HasMedial && lastInput?.Type != InputCommandType.ImeCompositionTermination && !ch.IsBlacklistedFromFinalConsonant() && HangulCluster.Consonant.TryMergeCluster(prevSplit.FinalConsonant, ch, out var merged))
				{
					// 이전 글자에 초성, 중성 모두 존재하고, 종성에 들어갈 수 없는 문자('ㄸ' 등)이(가) 아니며 및 종성 조합 시도가 성공했을 때
					prevSplit = prevSplit with { FinalConsonant = merged };
					input = InputCommand.KeyInput(ImeState.Korean, shiftState, keyboardKey, $"_{prevSplit.Merge()}");
				}
				else
				{
					// 위 if 조건에 대하여:
					// (1) 이전 글자에 초성 없이 중성만 존재하거나, 아예 이전 글자 자체가 존재하지 않는 등...
					// (2) 이전 글자에 초성은 있지만 중성이 없음
					// (3) 이전 글자 종성에 자음이 끼어들어가는 현상 방지하기 위해 명시적으로 조합을 중단하였을 때
					// (4) 글자가 종성에 들어갈 수 없는 글자일 때 ('ㄸ', 'ㅉ', 'ㅃ')
					// (5) 이전 글자 종성 조합 실패

					// 조합 중단; 다음 글자 초성 배치
					newSplit = HangulSplit.Hangul(ch);
					input = InputCommand.KeyInput(ImeState.Korean, shiftState, keyboardKey, $"{((HangulSplit)newSplit).Merge()}");
				}
				break;
			}
			case JamoType.Medial:
			{
				if (prevSplit.HasInitialConsonant && prevSplit.HasMedial && prevSplit.HasFinalConsonant)
				{
					// 이전 글자에 초중종이 모두 존재함
					// 도깨비불 현상: 이전 글자 종성을 뺏어올 수 있음
					// 이전 글자 종성 중 마지막 원소 뺏어오기
					var prevSplitFinal = HangulCluster.Consonant.SplitCluster(prevSplit.FinalConsonant);

					// 이전 글자 종성 마지막자 제거
					prevSplit = prevSplit with { FinalConsonant = prevSplitFinal.Count == 1 ? ' ' : prevSplitFinal[0] };

					// 새 글자에 종성 마지막자 갖다놓기
					var prevSplitFinalSteal = prevSplitFinal[prevSplitFinal.Count - 1];
					newSplit = HangulSplit.Hangul(prevSplitFinalSteal, ch);
					input = InputCommand.KeyInput(ImeState.Korean, shiftState, keyboardKey, $"_{prevSplit.Merge()}{((HangulSplit)newSplit).Merge()}");
				}
				else if (prevSplit.HasInitialConsonant && !prevSplit.HasFinalConsonant && HangulCluster.Vowel.TryMergeCluster(prevSplit.Medial, ch, out var merged))
				{
					// 이전 글자에 초성이 존재하고 종성이 존재하지 않음 - 모음 조합 가능
					// 이전 글자 모음 조합
					prevSplit = prevSplit with { Medial = merged };
					input = InputCommand.KeyInput(ImeState.Korean, shiftState, keyboardKey, $"_{prevSplit.Merge()}");
				}
				else
				{
					// (1) 도깨비불 현상 발현 불가 / (2) 모음 조합 실패
					// 조합 중단; 다음 글자에 중성 배치
					newSplit = HangulSplit.Hangul(medial: ch);
					input = InputCommand.KeyInput(ImeState.Korean, shiftState, keyboardKey, $"{((HangulSplit)newSplit).Merge()}");
				}
				break;
			}
			default:
				throw new ArgumentException("Unknown Jamo type: " + jamo, nameof(jamo));
		}

		return (prevSplit, newSplit, input);
	}

	private JamoType Consonant2Jamo(ConsonantType cons) => cons switch
	{
		ConsonantType.None => JamoType.None,
		ConsonantType.Initial or ConsonantType.Final => JamoType.Consonant,
		ConsonantType.Medial => JamoType.Medial,
		_ => throw new ArgumentException("Unknown consonant type: " + cons, nameof(cons))
	};

	public IImmutableList<InputCommand> Recompose()
	{
		if (pieces.Count == 0)
			return ImmutableList<InputCommand>.Empty;

		var builder = ImmutableList.CreateBuilder<InputCommand>();

		// Process remaining chars
		var prevSplit = HangulSplit.EmptyHangul;
		InputCommand? lastInput = null;
		foreach (var piece in pieces)
		{
			// 이전 글자 종성에 초성이 끼어들어가 버려서 입력이 제대로 되지 않을 것이 예상되는 경우 (예시: 믈ㅅ셕)
			if (prevSplit.HasInitialConsonant && prevSplit.HasMedial && prevSplit.HasFinalConsonant && HangulCluster.Consonant.TryMergeCluster(prevSplit.FinalConsonant, piece.InitialConsonant, out _))
			{
				var terminate = InputCommand.ImeCompositionTermination();
				lastInput = terminate;
				builder.Add(terminate); // 강제로 조합 중단 (오른쪽 화살표 키를 누른다던지...)
			}

			foreach ((var _type, var ch) in piece.Serialize())
			{
				var type = Consonant2Jamo(_type);
				(var prevSplitNew, var newSplit, var input) = Append(prevSplit, lastInput, type, ch);
				prevSplit = prevSplitNew;

				LibLogger.Verbose<HangulRecomposer>("Composition simuation of appending {type} char {ch} -> {prev: {prev}, new: {new}, input: {input}}", type, ch, prevSplitNew, newSplit, input);

				if (newSplit is HangulSplit newSplitNonNull)
					prevSplit = newSplitNonNull;

				lastInput = input;
				builder.Add(input);
			}
		}
		return builder.ToImmutable();
	}
}

public readonly struct InputCommand
{
	/// <summary>
	/// 현재 입력에서 요구하는 IME의 상태를 나타냅니다.
	/// </summary>
	/// <remarks>
	/// 한글 입력 처리자는 이 속성을 검사하여 만약 IME 상태가 이와 일치하지 않는다면 IME 상태를 변경해야 합니다.
	/// (예시: 한글을 입력해야 하는데 IME가 영어 모드로 설정되어 있을 경우 IME를 한글 모드로 전환)
	/// </remarks>
	public readonly ImeState ImeState { get; }

	/// <summary>
	/// 현재 입력에서 요구하는 SHIFT 키의 상태를 나타냅니다.
	/// 두벌식 자판 기준 쌍자음이나 특정 모음들을 입력할 때는 SHIFT키를 눌러야 하기에 존재합니다.
	/// </summary>
	/// <remarks>
	/// 한글 입력 처리자는 이 속성을 검사하여 만약 현재 저장된 SHIFT 키의 상태가 이와 일치하지 않는다면 상태를 변경해야 합니다.
	/// </remarks>
	public readonly ShiftState ShiftState { get; }

	/// <summary>
	/// 누를 키보드 키. 한글 자판 기준이 아닌, 영어 자판 기준입니다.
	/// </summary>
	/// <remarks>
	/// 예시: QWERTY 자판 기준 'ㄱ' -> 'r'
	/// </remarks>
	public readonly char Key { get; }

	/// <summary>
	/// 추가될 문자열을 나타냅니다. 문자열이 '_'로 시작하는 경우, 지금까지 입력된 문자열 맨 마지막 문자를 '_' 바로 뒤 문자로 덮어씁니다.
	/// 문자열에서 '_'은 맨 앞에, 최대 1개까지만 등장할 수 있습니다.
	/// </summary>
	/// <remarks>
	/// 예시: 현재까지 입력된 내용이 '가나다'이고 <c>TextUpdate</c>가 'ㄹ' 이면 입력된 내용은 '가나다ㄹ'가 되지만,
	/// <c>TextUpdate</c>가 '_ㄹ'일 경우 입력된 내용은 '가나ㄹ'가 되어야 합니다.
	/// 또한, <c>TextUpdate</c>가 '_라마사ㄷ' 일 경우, 입력된 내용은 '가나라마사ㄷ'가 되어야 합니다.
	/// </remarks>
	public string TextUpdate { get; }

	/// <summary>
	/// 이번 입력이 한글 키 입력이 아닌, IME 조합 중단인지를 나타냅니다.
	/// </summary>
	/// <remarks>
	/// 두벌식 자판에서 일부 단어들을 입력하기 위해서는 IME 조합 중단이 필요합니다.
	/// 일례로, '믈ㅅ셕'을 입력하기 위해서는 'ㅁㅡㄹ(조합중단)ㅅㅅㅕㄱ' 순으로 입력해야 합니다.
	/// 'ㅁㅡㄹㅅㅅㅕㄱ' 순으로 입력 시 '믌셕'이 되어 버리기 때문입니다.
	/// </remarks>
	public readonly InputCommandType Type { get; }

	private InputCommand(InputCommandType type, ImeState imeState, ShiftState shiftState, char key, string textUpdate)
	{
		Type = type;
		ImeState = imeState;
		ShiftState = shiftState;
		Key = key;
		TextUpdate = textUpdate;
	}

	public static InputCommand KeyInput(ImeState imeState, ShiftState shiftState, char key, string textUpdate) => new(InputCommandType.KeyInput, imeState, shiftState, key, textUpdate);

	public static InputCommand ImeCompositionTermination() => new(InputCommandType.ImeCompositionTermination, ImeState.None, ShiftState.None, ' ', "");

	public override string ToString() => $"Input{{Type: {Type}, IME: {ImeState}, Shift: {ShiftState}, Key: {Key}, TextUpdate: {TextUpdate}}}";
}

public enum ShiftState
{
	/// <summary>
	/// SHIFT 키 상태에 신경 쓰지 않습니다.
	/// </summary>
	None,

	/// <summary>
	/// SHIFT 키가 눌러진 상태를 의미합니다.
	/// </summary>
	Press,

	/// <summary>
	/// SHIFT 키가 떼어진 상태를 의미합니다.
	/// </summary>
	Release
}

public enum ImeState
{
	/// <summary>
	/// IME 상태에 신경 쓰지 않습니다.
	/// </summary>
	None,

	/// <summary>
	/// 영어 입력 모드 IME를 나타냅니다.
	/// </summary>
	English,

	/// <summary>
	/// 한글 입력 모드 IME를 나타냅니다.
	/// </summary>
	Korean
}

public enum InputCommandType
{
	/// <summary>
	/// 키 입력
	/// </summary>
	KeyInput,

	/// <summary>
	/// IME 조합 중단
	/// </summary>
	ImeCompositionTermination
}