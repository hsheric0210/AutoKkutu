/* communicatorJs : AutoKkutu - CefSharp JSB communicator
 * 
 * Reserved names:
 * ___jsbGlobal___
 * ___jsbObj___
 * onSend
 * onReceive
 * bindObjectAsync
 * BindObjectAsync
 * ___commSend___
 * ___commRecv___
 */

___commSend___ = (async function (msg) {
    let fn = window['___jsbGlobal___']['bindObjectAsync'] ?? window['___jsbGlobal___']['BindObjectAsync'];
    await fn('___jsbObj___');
    window['___jsbObj___']['onSend'](msg)
});

___commRecv___ = (async function (msg) {
    let fn = window['___jsbGlobal___']['bindObjectAsync'] ?? window['___jsbGlobal___']['BindObjectAsync'];
    await fn('___jsbObj___');
    window['___jsbObj___']['onReceive'](msg)
});
