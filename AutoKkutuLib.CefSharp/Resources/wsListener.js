/* ws_listener.js
 * 
 * Reserved names:
 * ___wsHook___
 * ___jsbGlobal___
 * ___jsbObj___
 * onSend
 * onReceive
 * bindObjectAsync
 * BindObjectAsync
 */
window['___wsHook___'].before = (function (d, modJson, ws) {
    (async function (data, modJson) {
        let fn = window['___jsbGlobal___']['bindObjectAsync'] ?? window['___jsbGlobal___']['BindObjectAsync'];
        await fn('___jsbObj___');
        window['___jsbObj___']['onSend'](modJson == null ? data : JSON.stringify(modJson))
    })(d, modJson);
    return d;
});

window['___wsHook___'].after = (function (d, modJson, ws) {
    (async function (data, modJson) {
        let fn = window['___jsbGlobal___']['bindObjectAsync'] ?? window['___jsbGlobal___']['BindObjectAsync'];
        await fn('___jsbObj___');
        window['___jsbObj___']['onReceive'](modJson == null ? data : JSON.stringify(modJson))
    })(d.data, modJson);
    return d;
});
