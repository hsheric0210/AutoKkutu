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
window['___wsHook___'].before = (function (data, url, ws) {
    (async function (data) {
        await window['___jsbGlobal___']['bindObjectAsync']('___jsbObj___');
        window['___jsbObj___'].onSend(data)
    })(data);
    return data;
});

window['___wsHook___'].after = (function (data, url, ws) {
    (async function (data) {
        await window['___jsbGlobal___']['bindObjectAsync']('___jsbObj___');
        window['___jsbObj___'].onReceive(data)
    })(data.data);
    return data
});
