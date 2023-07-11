//리오 onInputEvent 로깅 알고리즘에서 가져옴

onInputHistory = []
previousInputEventTime = new Date().getTime();
function onInputEvent(e) {
    var event = e.originalEvent
    console.log('input event!', event);
    if (!event.isTrusted) {
        console.log('untrusted event ignored', event);
        return
    }
    var inputType = event.inputType,
        data = event.data,
        targetValue = e.target.value,
        prevTime = previousInputEventTime,
        time = new Date().getTime(),
        timeDelta = prevTime === 0 ? '-' : time - prevTime
    previousInputEventTime = time
    var testData = {
        ev: 'c',
        v: timeDelta + 'ms | ' + inputType + ' | ' + data + ' | ' + targetValue,
    }
   /* if ($data['_test']) {
        send('test', testData, true) // 실시간 입력 로그 전송
    }*/
    var _historyElement = {
        time: time + ' (' + timeDelta + 'ms)',
        type: inputType,
        data: data,
        result: targetValue,
    }
    var historyElement = _historyElement
    onInputHistory.push(historyElement) // 입력 로그 누적
    onInputHistory = onInputHistory.slice(Math.max(onInputHistory.length - 100, 0)) // 입력 로그 최대 100개로 제한
    console.log('input log', onInputHistory)
}
//$stage.talk.on('input', onInputEvent)
//$('#search').on('input', onInputEvent)
$('[name="search"]').on('input', onInputEvent)
