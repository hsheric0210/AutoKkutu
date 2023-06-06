// 치명적인 문제점: 'isTrusted = false'라서 만약이라도 서버가 이 속성을 감지하기 시작한다면 그대로 망함
// 'isTrusted = true'로 하기 위해선,
// * cefsharp: 'browser.GetDevToolsClient().Input.DispatchKeyEventAsync()'
// * selenium: 'driver.GetDevToolsSession().GetVersionSpecificDomains<DevToolsSessionDomains>().Input.DispatchKeyEvent'
// 와 같이 DevTools Protocol을 사용하면 가능하나... 이는 속도가 느리다.

//wsHook.js에서 사용된 방법처럼 onkeyup, onkeydown 이벤트를 Object.defineProperty 'set' 을 통해 재정의하여 이벤트를 중간에서 가로챌 수 있다!
//이를 통해 강제로 isTrusted 속성의 값을 변경하여 넘겨주면 끝이다!

function simipt(info,ishangul){

let index=0;
//https://stackoverflow.com/a/30866420
function append()
{
    if (index==info.length)
    {
        ___SendChat___();
        return;
    }
    let current = info[index];
    let str = ___GetChat___();
    ___CallKeyEvent___(current.key.toLowerCase(), ishangul, current.key == current.key.toUpperCase() && current.key != current.key.toLowerCase()); //https://stackoverflow.com/a/31415820
    if(current.ch.startsWith('_'))
    {
        str += current.ch[1];
    }
    else
    {
        str = str.slice(0, -1) + ch; // https://masteringjs.io/tutorials/fundamentals/remove-last-character
    }
    ___SetChat___(current.ch);
    index++;
    window.setTimeout(append, Math.round(delay + delayRandom * 2 * Math.random() - delayRandom));
}
append()
}

function simipt(info,delay,delayRandom,ishangul){

    let index=0;
    let elem=document.getElementById('search2');
    //https://stackoverflow.com/a/30866420
    function append()
    {
        if (index==info.length)
        {
            console.log('enter!');
            return;
        }
        let current = info[index];
        let str = elem.value || '';
        console.log("call keyevent",current[0]);
        //___CallKeyEvent___(current.key.toLowerCase(), ishangul, current.key == current.key.toUpperCase() && current.key != current.key.toLowerCase()); //https://stackoverflow.com/a/31415820
        if(current[1].startsWith('_'))
        {
            str += current[1][1];
        }
        else
        {
            str = str.slice(0, -1) + current[1]; // https://masteringjs.io/tutorials/fundamentals/remove-last-character
        }
        console.log('text update', str);
        elem.value = str;
        index++;
        let dly = Math.round(delay + delayRandom * 2 * Math.random() - delayRandom);
        console.log('delay',dly);
        window.setTimeout(append, dly);
    }
    append()
}

simipt([['r','_ㄱ'],
['k','가'],
['s','_ㄴ'],
['k','나']],50,50,false)
