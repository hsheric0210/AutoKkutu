/* ws_listener.js
 * 
 * Reserved names:
 * ___wsHook___
 * ___wsAddr___
 * ___originalWS___
 * ___wsGlobal___
 * ___wsBuffer___
 */

// Workaround for constructor renaming bug
window.___wsGlobal___ = new window['___originalWS___']('___wsAddr___');
window.___wsBuffer___ = []
let open = false
___wsGlobal___.onopen = function () {
    console.log('WebSocket connected.');
    ___wsBuffer___.forEach(msg => ___wsGlobal___.send(msg));
    ___wsBuffer___.length = 0;
    open = true;
};
___wsHook___.before = function (data, url, ws) {
    let msg = 's' + data;
    if (open) ___wsGlobal___.send(msg);
    else ___wsBuffer___.push(msg)
    return data;
};

___wsHook___.after = function (data, url, ws) {
    let msg = 'r' + data.data;
    if (open) ___wsGlobal___.send(msg)
    else ___wsBuffer___.push(msg)
    return data
};
