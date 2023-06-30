/* eslint-disable no-proto */
/* eslint-disable accessor-pairs */
/* eslint-disable no-global-assign */

/* wsHook.js
 * https://github.com/skepticfx/wshook
 * Reference: http://www.w3.org/TR/2011/WD-websockets-20110419/#websocket
 * 
 * Reserved Names:
 * ___wsHook___
 * ___originalWS___
 * ___wsFilter___
 * 
 * Force transform:
 * _addEventListener
 */


// Learned a lot from https://stackoverflow.com/questions/6598945/detect-if-function-is-native-to-browser


var ___wsHook___ = {};
(function () {
    // Mutable MessageEvent.
    // Subclasses MessageEvent and makes data, origin and other MessageEvent properites mutatble.
    function MutableMessageEvent(o) {
        this.bubbles = o.bubbles || false
        this.cancelBubble = o.cancelBubble || false
        this.cancelable = o.cancelable || false
        this.currentTarget = o.currentTarget || null
        this.data = o.data || null
        this.defaultPrevented = o.defaultPrevented || false
        this.eventPhase = o.eventPhase || 0
        this.lastEventId = o.lastEventId || ''
        this.origin = o.origin || ''
        this.path = o.path || new Array(0)
        this.ports = o.parts || new Array(0)
        this.returnValue = o.returnValue || true
        this.source = o.source || null
        this.srcElement = o.srcElement || null
        this.target = o.target || null
        this.timeStamp = o.timeStamp || null
        this.type = o.type || 'message'
        this.__proto__ = o.__proto__ || MessageEvent.__proto__
    }

    var before = ___wsHook___.before = function (data, modJson, wsObject) {
        return data
    }
    var after = ___wsHook___.after = function (e, modJson, wsObject) {
        return e
    }
    ___wsHook___.resetHooks = function () {
        ___wsHook___.before = before
        ___wsHook___.after = after
    }

    window['___wsFilter___'] = {
        'undefined': function (d) { return false; },
        'null': function (d) { return false; },
        'active': false
    };
    //example: ___wsFilter___['welcome'] = function(data){return true;}

    function checkFilter(data) {
        let filterActive = window['___wsFilter___'].active;
        let json = filterActive ? JSON.parse(data) : null;
        let filter = filterActive ? window['___wsFilter___'][json.type] : null;
        let filtered = (filter && typeof (filter) === 'function') ? filter(json) : null;
        if (!filterActive || filtered)
            return filtered === true ? null : filtered; // filtered==true -> pass-thru
        else
            return undefined
    }

    // Decrypt turnEnd packet
    function decodeTE(data) {
        if (data.sum) {
            let keyProp = String.fromCharCode(data.sum - data.score)
            _CONSLOG('Key prop name is', keyProp)
            let xorKey = unescape(atob(data[keyProp]))
            _CONSLOG('Key is', xorKey)
            let value = ''
            for (
                var i = 1;
                i < data.value.length;
                i++
            ) {
                value += String.fromCharCode(
                    data.value.charCodeAt(i) ^
                    xorKey.charCodeAt(i - 1)
                )
            }
            _CONSLOG('Decrypted turnEnd value', value)
        }
        else
            _CONSLOG('TurnEnd found but sum field not available')
    }

    let _CONSLOG = window.console.log
    let _WS = window.WebSocket
    window['___originalWS___'] = _WS;

    // https://stackoverflow.com/a/73156265
    let nativeFunctionPatcher = function (func, funcName, typeName) {
        return new Proxy(func, {
            get(target, prop, receiver) {
                if (prop === "name") {
                    return funcName;
                } else if (prop === Symbol.toPrimitive || prop == 'toString') {
                    if (typeName)
                        return function () { return typeName + "() { [native code] }"; };
                    else if (prop == 'toString')
                        return function () { return "function " + funcName + "() { [native code] }"; };
                    else
                        return function () { return "function () { [native code] }"; };
                }
                return Reflect.get(...arguments);
            }
        });
    }

    let newAddEventListener = function (self, _addEventListener) {
        _CONSLOG("newAddEventListener called")
        return function addEventListener() {
            let eventThis = this
            // if eventName is 'message'
            if (arguments[0] === 'message') {
                arguments[1] = (function (userFunc) {
                    return function instrumentAddEventListener() {
                        let filtered = checkFilter(arguments[0].data);
                        if (filtered !== undefined)
                            arguments[0] = ___wsHook___.after(new MutableMessageEvent(arguments[0]), filtered, self)
                        if (arguments[0] === null) return
                        if (arguments[0]?.data)
                        {
                            let parsed = JSON.parse(arguments[0].data)
                            _CONSLOG("WebSocket receive AddEvtLstn", arguments[0].data)
                            if (parsed.type == 'turnEnd')
                                decodeTE(parsed)
                        }
                        userFunc.apply(eventThis, arguments)
                    }
                })(arguments[1])
            }
            return _addEventListener.apply(this, arguments)
        }
    }

    let newSend = function (self, _send) {
        _CONSLOG("newSend called")
        return function send(data) {
            let filtered = checkFilter(data);
            if (filtered !== undefined)
                arguments[0] = ___wsHook___.before(data, filtered, self)
            _CONSLOG("WebSocket send", data)
            _send.apply(this, arguments)
        }
    }

    window.WebSocket = nativeFunctionPatcher(function (url, protocols) {
        let WSObject
        this.url = url
        this.protocols = protocols
        _CONSLOG("WebSocket creation for: ", url, protocols)
        if (!this.protocols) { WSObject = new _WS(url) } else { WSObject = new _WS(url, protocols) }

        Object.defineProperty(WSObject, 'onmessage', {
            configurable: true,
            enumerable: true,
            'set': function () {
                // Kkutu updates onmessage every turn starts...
                // https://github.com/JJoriping/KKuTu/blob/a2c240bc31fe2dea31d26fb1cf7625b4645556a6/Server/lib/Web/lib/kkutu/rule_classic.js#L55C17-L55C27
                _CONSLOG("onmessage.set called on ws", this.url)
                let eventThis = this
                let userFunc = arguments[0]
                let onMessageHandler = function () {
                    let filtered = checkFilter(arguments[0].data);
                    if (filtered !== undefined) {
                        arguments[0] = ___wsHook___.after(new MutableMessageEvent(arguments[0]), filtered, WSObject)
                    }
                    if (arguments[0] === null) return
                    if (arguments[0]?.data)
                    {
                        let parsed = JSON.parse(arguments[0].data)
                        _CONSLOG("WebSocket receive OnMsg", arguments[0].data)
                        if (parsed.type == 'turnEnd')
                            decodeTE(parsed)
                    }
                    userFunc.apply(eventThis, arguments)
                }

                if (!WSObject._injEventListeners)
                    WSObject._injEventListeners = []

                for (let prevInj in WSObject._injEventListeners) {
                    _CONSLOG("dropped prev handler", WSObject._injEventListeners[prevInj])
                    WSObject.removeEventListener('message', WSObject._injEventListeners[prevInj], false)
                }
                WSObject._injEventListeners = []
                
                WSObject._addEventListener.apply(this, ['message', onMessageHandler, false])
                WSObject._injEventListeners.push(onMessageHandler)
            }
        })

        return WSObject
    }, 'WebSocket', 'WebSocket')
    window.WebSocket.prototype = _WS.prototype; // Overwrite prototype
    window.WebSocket.prototype._send = window.WebSocket.prototype.send;
    window.WebSocket.prototype.send = nativeFunctionPatcher(newSend(window.WebSocket.prototype, window.WebSocket.prototype._send), 'send');
    window.WebSocket.prototype.__proto__._addEventListener = window.WebSocket.prototype.__proto__.addEventListener
    window.WebSocket.prototype.addEventListener = nativeFunctionPatcher(newAddEventListener(window.WebSocket.prototype, window.WebSocket.prototype.__proto__._addEventListener), 'addEventListener');
})();

// Perfectly bypassing KKUTU.IO WebSocket manipulation check.
// All WebSocket calls are perfectly intercepted, even 'WebSocket.prototype.send.*', 'WebSocket.prototype.addEventListener.*' calls. :)

// TODO: Reverse-implement this back to wsHook.js

// StackOverflow's
// https://stackoverflow.com/questions/6598945/detect-if-function-is-native-to-browser/73156265#73156265
// https://stackoverflow.com/questions/36372611/how-to-test-if-an-object-is-a-proxy
