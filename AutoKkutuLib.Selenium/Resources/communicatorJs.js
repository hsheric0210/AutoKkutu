/* communicatorJs : AutoKkutu - Selenium communicator
 * 
 * Reserved names:
 * ___wsAddr___
 * ___originalWS___
 * ___wsGlobal___
 * ___wsBuffer___
 * ___commSend___
 * ___commRecv___
 */

(function () {
    window.___wsGlobal___ = new ___originalWS___('___wsAddr___');
    window.___wsBuffer___ = []
    let open = false
    ___wsGlobal___.onopen = function () {
        console.log('WebSocket connected.');
        ___wsBuffer___.forEach(msg => ___wsGlobal___.send(msg));
        ___wsBuffer___.length = 0;
        open = true;
    };
    ___commSend___ = (async function (_msg) {
        let msg = 's' + _msg;
        if (open) ___wsGlobal___.send(msg);
        else ___wsBuffer___.push(msg)
        return _msg;
    });

    ___commRecv___ = (async function (_msg) {
        let msg = 'r' + _msg;
        if (open) ___wsGlobal___.send(msg)
        else ___wsBuffer___.push(msg)
        return _msg
    });
    return true;
})();
