/* ws_listener.js
 * 
 * Reserved names:
 * ___wsHook___
 * ___wsAddr___
 * ___originalWS___
 * ___wsVar___
 */

window.___wsVar___ = new window.___originalWS___('___wsAddr___');
console.log('___wsVar___');
console.log(window.___wsVar___);
let open = false
___wsVar___.onmessage = function (event) {
    console.log(event.data);
}
___wsVar___.onclose = function (evt) {
    console.log(event)
}
___wsVar___.onerror = function (err) {
    console.log(err)
}
___wsVar___.onopen = function () { console.log('ws conn'); open = true };
___wsHook___.before = function (data, url, ws) {
    if (open) ___wsVar___.send('s' + data)
    return data;
};

___wsHook___.after = function (data, url, ws) {
    if (open) ___wsVar___.send('r' + data.data)
    return data
};
