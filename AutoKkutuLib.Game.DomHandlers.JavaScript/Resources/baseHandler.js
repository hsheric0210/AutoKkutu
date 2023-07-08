// This code is safe both executed on global and in IIFE

// 여기 정의된 함수들은 먼저 실행된 스크립트(사이트 별 우회, 특화 기능 추가 등)들에 의해 언제든지 override될 수 있다.
// 실수로라도 override된 함수들을 다시 덮어씌워 버리지 않기 위해 이미 함수가 정의되어 있는지 매 정의 시마다 검사를 수행한다.

if (!___gameInProgress___) {
    ___gameInProgress___ = function () {
        let s = document.getElementsByClassName('GameBox Product')[0]?.style;
        return s != undefined && (s?.display ? s.display != 'none' : s.height != '');
    }
}


if (!___getGameMode___) {
    ___getGameMode___ = function () {
        let s = document.getElementsByClassName('room-head-mode')[0]?.textContent?.split('/')[0]?.trim();
        return s?.substring(s.indexOf(' ') + 1) || ''
    }
}

if (!___getPresentWord___) {
    ___getPresentWord___ = function () {
        if (!___gameDisplay___)
            ___gameDisplay___ = document.getElementsByClassName('jjo-display ellipse')[0];

        return ___gameDisplay___?.textContent || ''
    }
}

if (!___isMyTurn___) {
    ___isMyTurn___ = function () {
        if (!___myInputDisplay___)
            ___myInputDisplay___ = document.getElementsByClassName('game-input')[0];

        return ___myInputDisplay___ != undefined && ___myInputDisplay___.style.display != 'none'
    }
}

if (!___getTurnError___) {
    ___getTurnError___ = function () {
        return document.getElementsByClassName('game-fail-text')[0]?.textContent || ''
    }
}

if (!___getTurnTime___) {
    ___getTurnTime___ = function () {
        if (!___turnTimeDisplay___)
            ___turnTimeDisplay___ = document.querySelector("[class='graph jjo-turn-time']>[class='graph-bar']");

        let s = ___turnTimeDisplay___?.textContent;
        return s?.substring(0, s.length - 1) || ''
    }
}

if (!___getRoundTime___) {
    ___getRoundTime___ = function () {
        if (!___roundTimeDisplay___)
            ___roundTimeDisplay___ = document.querySelector("[class='graph jjo-round-time']>[class='graph-bar']");

        let s = ___roundTimeDisplay___?.textContent;
        return s?.substring(0, s.length - 1) || ''
    }
}

if (!___getRoundIndex___) {
    ___getRoundIndex___ = function () {
        return Array.from(document.querySelectorAll('#Middle>div.GameBox.Product>div>div.game-head>div.rounds>label')).indexOf(document.querySelector('.rounds-current'))
    }
}

if (!___getTurnHint___) {
    ___getTurnHint___ = function () {
        if (!___gameDisplay___)
            ___gameDisplay___ = document.getElementsByClassName('jjo-display ellipse')[0];

        if (___gameDisplay___) {
            let inner = ___gameDisplay___.innerHTML;
            if (inner.includes('label') && inner.includes('color') && inner.includes('170,'))
                return ___gameDisplay___.textContent;
        }
        return '';
    }
}

if (!___getMissionChar___) {
    ___getMissionChar___ = function () {
        let s = document.getElementsByClassName('items')[0];
        return s && s.style.opacity >= 1 ? s.textContent : ''
    }
}

if (!___getWordHistory___) {
    ___getWordHistory___ = function () {
        return Array.prototype.map.call(document.getElementsByClassName('ellipse history-item expl-mother'), v => v.childNodes[0].textContent)
    }
}

if (!___sendKeyEvents___) {
    ___sendKeyEvents___ = function (key, shift, hangul, upDelay, shiftUpDelay) {
        function evt(type, param) {
            document.dispatchEvent(new KeyboardEvent('key' + type, param));
        }

        let kc = key.toUpperCase().charCodeAt(0);
        if (shift) {
            evt('down', { 'key': 'Shift', 'shiftKey': true, 'keyCode': 16 });
        }

        if (hangul) {
            evt('down', { 'key': 'Process', 'shiftKey': shift, 'keyCode': 229 });
        }
        else {
            evt('down', { 'key': key, 'shiftKey': shift, 'keyCode': kc });
        }

        window.setTimeout(function () {
            if (hangul) {
                evt('up',
                    {
                        'key': 'Process',
                        'shiftKey': shift,
                        'keyCode': 229
                    });
            }
            evt('up', { 'key': key, 'shiftKey': shift, 'keyCode': kc });
        }, upDelay);

        if (shift) {
            window.setTimeout(function () {
                if (hangul) evt('up', { 'key': 'Process', 'keyCode': 229 });
                evt('up', { 'key': 'Shift', 'shiftKey': true, 'keyCode': 16 });
            }, shiftUpDelay);
        }
    }
}

if (!___updateChat___) {
    ___updateChat___ = function (input) {
        if (!___chatBox___)
            ___chatBox___ = document.querySelector('[id=\"Talk\"]');

        ___chatBox___.value = input
    }
}

if (!___clickSubmit___) {
    ___clickSubmit___ = function () {
        if (!___chatBtn___)
            ___chatBtn___ = document.getElementById('ChatBtn');

        ___chatBtn___.click()
    }
}

___funcRegistered___ = true;
