/* mainHelperJs.js : AutoKkutu JavaScript injection
 * Reserved Names:
 * ___originalWS___
 * ___wsFilter___
 * ___nativeSend___
 * ___nativeAddEventListener___
 * ___passthru___
 * ___commSend___
 * ___commRecv___
 * 
 * Global object back-ups:
 * ___getComputedStyle___
 * ___dispatchEvent___
 * ___consoleLog___
 * ___setTimeout___
 * ___setInterval___
 * ___getElementsByClassName___
 * ___querySelector___
 * ___querySelectorAll___
 * ___getElementById___
 */

/* eslint-disable no-proto */
/* eslint-disable accessor-pairs */
/* eslint-disable no-global-assign */

// Backup before being overwritten by some anticheat-like things
___getComputedStyle___ = window.getComputedStyle;
___consoleLog___ = console.log;
___setTimeout___ = window.setTimeout;
___setInterval___ = window.setInterval;

___dispatchEvent___ = document.dispatchEvent;
___getElementsByClassName___ = document.getElementsByClassName;
___querySelector___ = document.querySelector;
___querySelectorAll___ = document.querySelectorAll;
___getElementById___ = document.getElementById;


/* wsHook.js
 * https://github.com/skepticfx/wshook
 * Reference: http://www.w3.org/TR/2011/WD-websockets-20110419/#websocket
 */
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

    // Default message filter
    ___wsFilter___ = {
        'undefined': function (d) { return false; },
        'null': function (d) { return false; },
        'active': false
    };

    // Message filter impl
    function checkFilter(data) {
        let filterActive = ___wsFilter___.active;
        try {
            let json = filterActive ? JSON.parse(data) : null;
            let filter = filterActive ? ___wsFilter___[json.type] : null;
            let filtered = (filter && typeof (filter) === 'function') ? filter(json) : null;
            if (!filterActive || filtered)
                return filtered === true ? null : filtered; // filtered==true -> pass-thru
            else
                return undefined
        }
        catch (exc) {
            return undefined // do not handle
        }
    }

    // Message send handler
    function sniffSend(d, modJson) {
        (async function (data, modJson) {
            await ___commSend___(modJson == null ? data : JSON.stringify(modJson))
        })(d, modJson);
        return d;
    };

    // Message receive handler
    function sniffReceive(d, modJson) {
        (async function (data, modJson) {
            await ___commRecv___(modJson == null ? data : JSON.stringify(modJson))
        })(d.data, modJson);
        return d;
    };


    // Check WebSocket object and backup it
    if (!window.WebSocket) {
        ___consoleLog___("WebSocket unavailable @", location)
    }
    let _WS = window.WebSocket
    ___originalWSPrototype___ = _WS?.prototype

    // Fake property as native function. Completely undetectable.
    // https://stackoverflow.com/a/73156265
    function fakeAsNative(func, name, customType) {
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

    function makeSendHook(self, nativeSend) {
        return function (data) {
            if (!this['___passthru___']) {
                let filtered = checkFilter(data);
                if (filtered !== undefined)
                    arguments[0] = sniffSend(data, filtered)
            }
            nativeSend.apply(this, arguments)
        }
    }

    function makeAddEventListenerHook(self, nativeAddEventListener) {
        return function () {
            let eventThis = this
            if (!this['___passthru___'] && arguments[0] === 'message') {
                arguments[1] = (function (userFunc) {
                    return function instrumentAddEventListener() {
                        let filtered = checkFilter(arguments[0].data);
                        if (filtered !== undefined)
                            arguments[0] = sniffReceive(new MutableMessageEvent(arguments[0]), filtered)
                        if (arguments[0] === null) return
                        userFunc.apply(eventThis, arguments)
                    }
                })(arguments[1])
            }
            return nativeAddEventListener.apply(this, arguments)
        }
    }

    // Overwrite WebSocket object
    window.WebSocket = fakeAsNative(function (url, protocols) {
        let WSObject
        this.url = url
        this.protocols = protocols
        if (!this.protocols) { WSObject = new _WS(url) } else { WSObject = new _WS(url, protocols) }

        WSObject.send = fakeAsNative(makeSendHook(WSObject, _WS.prototype['___nativeSend___']), 'send')

        WSObject.addEventListener = fakeAsNative(makeAddEventListenerHook(WSObject, _WS.prototype.__proto__['___nativeAddEventListener___']), 'addEventListener')

        // Events needs to be proxied and bubbled down.
        Object.defineProperty(WSObject, 'onmessage', {
            configurable: true,
            enumerable: true,
            'set': function () { // Overwrite 'onmessage' property setter
                let eventThis = this
                let userFunc = arguments[0]
                let onMessageHandler = function () {
                    let filtered = checkFilter(arguments[0].data);
                    if (filtered !== undefined) {
                        arguments[0] = sniffReceive(new MutableMessageEvent(arguments[0]), filtered)
                    }
                    if (arguments[0] === null) return
                    userFunc.apply(eventThis, arguments)
                }

                // Clear previous onMessage sniffers to prevent duplicate listener addition
                if (WSObject['___injectedOnMessageListeners___']) {
                    for (let prevInj in WSObject['___injectedOnMessageListeners___']) {
                        WSObject.removeEventListener('message', WSObject['___injectedOnMessageListeners___'][prevInj], false)
                    }
                }
                WSObject['___injectedOnMessageListeners___'] = []

                WSObject['___nativeAddEventListener___'].apply(this, ['message', onMessageHandler, false])
                WSObject['___injectedOnMessageListeners___'].push(onMessageHandler) // Save injected sniffer
            }
        })

        ___commSend___(JSON.stringify({ "type": "Injected", "target": "WebSocket", "url": url }));

        return WSObject
    }, 'WebSocket', 'WebSocket')

    // Create backup vanilla WebSocket object (which bypasses hooks)
    ___originalWS___ = fakeAsNative(function (url, protocols) {
        let WSObject
        if (!protocols) { WSObject = new _WS(url) } else { WSObject = new _WS(url, protocols) }
        WSObject['___passthru___'] = true
        return WSObject
    }, '___originalWS___', '___originalWS___')

    // Overwrite WebSocket prototypes (make them unrecoverable)
    if (___originalWSPrototype___) {
        // Overwrite default prototype
        window.WebSocket.prototype = ___originalWSPrototype___

        // Overwrite default prototype functions -> there is no way to bypass hook without correct '___passthru___' key, which is randomized every launch
        window.WebSocket.prototype['___nativeSend___'] = ___originalWSPrototype___.send;
        window.WebSocket.prototype.send = fakeAsNative(makeSendHook(window.WebSocket.prototype, ___originalWSPrototype___['___nativeSend___']), 'send');

        window.WebSocket.prototype.__proto__['___nativeAddEventListener___'] = ___originalWSPrototype___.__proto__.addEventListener
        window.WebSocket.prototype.addEventListener = fakeAsNative(makeAddEventListenerHook(window.WebSocket.prototype, ___originalWSPrototype___.__proto__['___nativeAddEventListener___']), 'addEventListener');
    }
})();

/* axiosInterceptor.js
 * 
 * Reserved names:
 * ___commSend___
 */
(function () {
    addEventListener("DOMContentLoaded", () => {
        if (window.hasOwnProperty('axios')) {
            ___commSend___(JSON.stringify({ "type": "Injected", "target": "axios" }));
            axios.interceptors.request.use(req => {
                if (req.url == '/o/c') { // k****.c*.k* anticheat packet
                    ___commSend___(JSON.stringify({ "type": "AC", "data": req.data }));
                    ___consoleLog___("AntiCheat blocked!", req.data);
                    return false; // cancel
                }
                return req;
            }, err => { return Promise.reject(err); }, { synchronous: true });
        }
    });
})()
