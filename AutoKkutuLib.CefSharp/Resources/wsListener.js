/* ws_listener.js
 * 
 * Reserved names:
 * ___wsHook___
 * ___jsbGlobal___
 * ___jsbObj___
 * onSend
 * onReceive
 * bindObjectAsync
 */
window['___wsHook___'].before = (function (d, modJson, ws) {
    (async function (data, json) {
        let fn = window['___jsbGlobal___']['bindObjectAsync'] ?? window['___jsbGlobal___']['BindObjectAsync'];
        await fn('___jsbObj___');
        window['___jsbObj___'].onSend(json ? JSON.stringify(json) : data)
    })(d, modJson);
    return d;
});

window['___wsHook___'].after = (function (d, modJson, ws) {
    (async function (data, json) {
        let fn = window['___jsbGlobal___']['bindObjectAsync'] ?? window['___jsbGlobal___']['BindObjectAsync'];
        await fn('___jsbObj___');
        window['___jsbObj___'].onReceive(json ? JSON.stringify(json) : data)
    })(d.data, modJson);
    return d;
});
