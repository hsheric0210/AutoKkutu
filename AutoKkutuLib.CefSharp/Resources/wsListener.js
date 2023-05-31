/* ws_listener.js
 * 
 * Reserved names:
 * ___wsHook___
 * ___jsbGlobal___
 * ___jsbObj___
 * onSend
 * onReceive
 * BindObjectAsync
 */
___wsHook___.before = (function (data, url, ws) {
    (async function (data) {
        await ___jsbGlobal___.BindObjectAsync('___jsbObj___');
        ___jsbObj___.onSend(data)
    })(data);
    return data;
});

___wsHook___.after = (function (data, url, ws) {
    (async function (data) {
        await ___jsbGlobal___.BindObjectAsync('___jsbObj___');
        ___jsbObj___.onReceive(data)
    })(data.data);
    return data
});
