(function () {
    // This code is safe both executed on global context and in IIFE template '(function(){return <code>;})()'

    /* 스크립트 오류내기 싫으면 IIFE 함수 시작 전 부분에 공백이나 주석 넣지 마셈.
     * 만약 넣으면 코드가 내부적으로 (function(){
     *   return
     *   // ...
     *   <IIFE>()
     * })();
     * 이런 식으로 변해서 실행 시 오류남. (JavaScript는 줄 끝에 ';' 붙이지 않더라도 단지 줄바꿈만으로도 코드 줄 끝을 인식하기 때문에 그럼)
     */

    // 여기 정의된 함수들은 먼저 실행된 스크립트(사이트 별 우회, 특화 기능 추가 등)들에 의해 언제든지 override될 수 있다.
    // 실수로라도 override된 함수들을 다시 덮어씌워 버리지 않기 위해 이미 함수가 정의되어 있는지 매 정의 시마다 검사를 수행한다.

    if (!___getGameMode___) {
        ___getGameMode___ = function () {
            let s = ___getElementsByClassName___.call(document, 'room-head-mode')[0]?.textContent?.split('/')[0]?.trim();
            return s?.substring(s.indexOf(' ') + 1) || ''
        }
    }

    if (!___getPresentWord___) {
        ___getPresentWord___ = function () {
            if (!___gameDisplay___)
                ___gameDisplay___ = ___getElementsByClassName___.call(document, 'jjo-display ellipse')[0];

            return ___gameDisplay___?.textContent || ''
        }
    }

    if (!___getWordLength___) {
        ___getWordLength___ = function () {
            if (!___wordLengthDisplay___)
                ___wordLengthDisplay___ = ___getElementsByClassName___.call(document, 'jjo-display-word-length')[0];

            return ___wordLengthDisplay___?.textContent?.substring(1, 2) || '3'
        }
    }

    if (!___isMyTurn___) {
        ___isMyTurn___ = function () {
            if (!___myInputDisplay___)
                ___myInputDisplay___ = ___getElementsByClassName___.call(document, 'game-input')[0];

            return ___myInputDisplay___ != undefined && ___myInputDisplay___.style.display != 'none'
        }
    }

    if (!___getTurnError___) {
        ___getTurnError___ = function () {
            return ___getElementsByClassName___.call(document, 'game-fail-text')[0]?.textContent || ''
        }
    }

    if (!___getTurnTime___) {
        ___getTurnTime___ = function () {
            if (!___turnTimeDisplay___)
                ___turnTimeDisplay___ = ___querySelector___.call(document, "[class='graph jjo-turn-time']>[class='graph-bar']");

            let s = ___turnTimeDisplay___?.textContent;
            return s?.substring(0, s.length - 1) || ''
        }
    }

    if (!___getRoundTime___) {
        ___getRoundTime___ = function () {
            if (!___roundTimeDisplay___)
                ___roundTimeDisplay___ = ___querySelector___.call(document, "[class='graph jjo-round-time']>[class='graph-bar']");

            let s = ___roundTimeDisplay___?.textContent;
            return s?.substring(0, s.length - 1) || ''
        }
    }

    if (!___getRoundIndex___) {
        ___getRoundIndex___ = function () {
            return Array.from(___querySelectorAll___.call(document, '#Middle>div.GameBox.Product>div>div.game-head>div.rounds>label')).indexOf(___querySelector___.call(document, '.rounds-current'))
        }
    }

    if (!___getTurnHint___) {
        ___getTurnHint___ = function () {
            if (!___gameDisplay___)
                ___gameDisplay___ = ___getElementsByClassName___.call(document, 'jjo-display ellipse')[0];

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
            let s = ___getElementsByClassName___.call(document, 'items')[0];
            return s && s.style.opacity >= 1 ? s.textContent : ''
        }
    }

    if (!___getWordHistory___) {
        ___getWordHistory___ = function () {
            return Array.prototype.map.call(___getElementsByClassName___.call(document, 'ellipse history-item expl-mother'), v => v.childNodes[0].textContent)
        }
    }

    if (!___getChatBox___) {
        ___getChatBox___ = function () {
            if (!___chatBox___)
                ___chatBox___ = ___querySelector___.call(document, '[id=\"Talk\"]');
            return ___chatBox___;
        }
    }

    if (!___getTurnIndex___) {
        ___getTurnIndex___ = function () {
            return Array.from(___querySelectorAll___.call(document, 'div.game-body>div')).indexOf(___querySelector___.call(document, '.game-user-current'))
        }
    }

    if (!___getUserId___) {
        ___getUserId___ = function () {
            let myUserName = ___getElementsByClassName___.call(document, 'my-stat-name')[0]?.textContent;
            if (myUserName) {
                let key = Array.from(___querySelectorAll___.call(document, 'div.UserListBox.Product>div>div>div.users-name.ellipse')).find(t => t.textContent == myUserName);
                if (key) {
                    return key.parentElement.id.substring(11); // 'users-item-'.length
                }
            }
            return null; // Not found
        }
    }

    if (!___getGameSeq___) {
        ___getGameSeq___ = function () {
            let gameBoxStyle = ___getElementsByClassName___.call(document, 'GameBox Product')[0]?.style;
            if (gameBoxStyle?.display ? gameBoxStyle.display != 'none' : gameBoxStyle?.height != '') // GameBox shown
            {
                return Array.from(___getElementsByClassName___.call(document, 'game-user')).map(t => t.id.substring(10)); // 'game-user-'.length
            }
            return []
        }
    }

    if (!___sendKeyEvents___) {
        ___sendKeyEvents___ = function (key, shift, hangul, upDelay) {
            function evt(type, param) {
                ___dispatchEvent___.call(document, new KeyboardEvent('key' + type, param));
            }

            let kc = key.toUpperCase().charCodeAt(0);
            if (!___shiftState___ && shift) {
                evt('down', { 'key': 'Shift', 'shiftKey': true, 'keyCode': 16 });
                ___shiftState___ = true;
            }
            else if (___shiftState___ && !shift) {
                if (hangul) evt('up', { 'key': 'Process', 'keyCode': 229 });
                evt('up', { 'key': 'Shift', 'shiftKey': true, 'keyCode': 16 });
                ___shiftState___ = false;
            }

            if (hangul) {
                evt('down', { 'key': 'Process', 'shiftKey': shift, 'keyCode': 229 });
            }
            else {
                evt('down', { 'key': key, 'shiftKey': shift, 'keyCode': kc });
            }

            ___setTimeout___.call(window, function () {
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
        }
    }

    if (!___updateChat___) {
        ___updateChat___ = function (input) {
            ___getChatBox___().value = input
        }
    }

    if (!___clickSubmit___) {
        ___clickSubmit___ = function () {
            if (!___chatBtn___)
                ___chatBtn___ = ___getElementById___.call(document, 'ChatBtn');

            ___chatBtn___.click()
        }
    }

    if (!___appendChat___) {
        ___appendChat___ = function (textUpdate, sendEvents, key, shift, hangul, upDelay) {
            let str = ___getChatBox___().value || '';
            if (sendEvents) {
                ___sendKeyEvents___(key, shift, hangul, upDelay); //https://stackoverflow.com/a/31415820
            }
            if (textUpdate.startsWith('_')) {
                str = str.slice(0, -1) + textUpdate.slice(1)
            }
            else {
                str += textUpdate;
            }
            ___getChatBox___().value = str;
        }
    }

    if (!___focusChat___) {
        ___focusChat___ = function () {
            ___getChatBox___().focus()
        }
    }

    ___funcRegistered___ = true;
    return true;
})();
