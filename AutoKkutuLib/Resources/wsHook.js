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
 * ___nativeSend___
 * ___nativeAddEventListener___
 * ___passthru___
 */
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
    var modifyUrl = ___wsHook___.modifyUrl = function (url) {
        return url
    }
    ___wsHook___.resetHooks = function () {
        ___wsHook___.before = before
        ___wsHook___.after = after
        ___wsHook___.modifyUrl = modifyUrl
    }

    window['___wsFilter___'] = {
        'undefined': function (d) { return false; },
        'null': function (d) { return false; },
        'active': false
    };

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

    if (!window.WebSocket) {
        console.log("WebSocket unavailable @", location)
    }
    let _WS = window.WebSocket
    window['___WSProtoBackup___'] = _WS?.prototype

    // https://stackoverflow.com/a/73156265
    let fakeAsNative = function (func, name, customType) {
        return new Proxy(func, {
            get(target, prop, receiver) {
                if (prop === "name") {
                    return name;
                } else if (prop === Symbol.toPrimitive || prop == 'toString') {
                    if (customType)
                        return function () { return customType + "() { [native code] }"; };
                    else if (prop == 'toString')
                        return function () { return "function " + name + "() { [native code] }"; };
                    else
                        return function () { return "function () { [native code] }"; };
                }
                return Reflect.get(...arguments); // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Proxy
            }
        });
    }

    let newSend = function (self, nativeSend) {
        return function (data) {
            if (!this['___passthru___']) {
                let filtered = checkFilter(data);
                if (filtered !== undefined)
                    arguments[0] = ___wsHook___.before(data, filtered, self)
            }
            nativeSend.apply(this, arguments)
        }
    }
    let newAddEventListener = function (self, nativeAddEventListener) {
        return function () {
            let eventThis = this
            if (!this['___passthru___'] && arguments[0] === 'message') {
                arguments[1] = (function (userFunc) {
                    return function instrumentAddEventListener() {
                        let filtered = checkFilter(arguments[0].data);
                        if (filtered !== undefined)
                            arguments[0] = ___wsHook___.after(new MutableMessageEvent(arguments[0]), filtered, self)
                        if (arguments[0] === null) return
                        userFunc.apply(eventThis, arguments)
                    }
                })(arguments[1])
            }
            return nativeAddEventListener.apply(this, arguments)
        }
    }
    window.WebSocket = fakeAsNative(function (url, protocols) {
        let WSObject
        url = ___wsHook___.modifyUrl(url) || url
        this.url = url
        this.protocols = protocols
        if (!this.protocols) { WSObject = new _WS(url) } else { WSObject = new _WS(url, protocols) }

        WSObject.send = fakeAsNative(newSend(WSObject, _WS.prototype['___nativeSend___']), 'send')

        WSObject.addEventListener = fakeAsNative(newAddEventListener(WSObject, _WS.prototype.__proto__['___nativeAddEventListener___']), 'addEventListener')

        // Events needs to be proxied and bubbled down.
        Object.defineProperty(WSObject, 'onmessage', {
            configurable: true,
            enumerable: true,
            'set': function () {
                let eventThis = this
                let userFunc = arguments[0]
                let onMessageHandler = function () {
                    let filtered = checkFilter(arguments[0].data);
                    if (filtered !== undefined) {
                        arguments[0] = ___wsHook___.after(new MutableMessageEvent(arguments[0]), filtered, WSObject)
                    }
                    if (arguments[0] === null) return
                    userFunc.apply(eventThis, arguments)
                }
                WSObject['___nativeAddEventListener___'].apply(this, ['message', onMessageHandler, false])
            }
        })

        return WSObject
    }, 'WebSocket', 'WebSocket')

    // Create plain WebSocket which is not hooked
    window['___originalWS___'] = fakeAsNative(function (url, protocols) {
        let WSObject
        if (!protocols) { WSObject = new _WS(url) } else { WSObject = new _WS(url, protocols) }
        WSObject['___passthru___'] = true
        return WSObject
    }, '___originalWS___', '___originalWS___')

    if (window['___WSProtoBackup___']) {
        // Overwrite default prototype
        window.WebSocket.prototype = window['___WSProtoBackup___']

        // Overwrite default prototype functions -> there is no way to bypass hook without correct '___passthru___' key, which is randomized every launch
        window.WebSocket.prototype['___nativeSend___'] = window['___WSProtoBackup___'].send;
        window.WebSocket.prototype.send = fakeAsNative(newSend(window.WebSocket.prototype, window['___WSProtoBackup___']['___nativeSend___']), 'send');

        window.WebSocket.prototype.__proto__['___nativeAddEventListener___'] = window['___WSProtoBackup___'].__proto__.addEventListener
        window.WebSocket.prototype.addEventListener = fakeAsNative(newAddEventListener(window.WebSocket.prototype, window['___WSProtoBackup___'].__proto__['___nativeAddEventListener___']), 'addEventListener');
    }
})();
