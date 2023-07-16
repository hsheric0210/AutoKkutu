(function () {
    ___wsFilter___['___turnEnd___'] = function (data) {
        if (data.sum) {
            let keyProp = String.fromCharCode(data.sum - data.score);
            let xorKey = unescape(atob(data[keyProp]));
            let value = '';
            for (let i = 1, j = data.value.length; i < j; i++) {
                value += String.fromCharCode(data.value.charCodeAt(i) ^ xorKey.charCodeAt(i - 1))
            }
            ___consoleLog___("Decoded turnEnd packet", data.value, "to", value)
            data.value = value;
        }
        return data;
    };
    ___wsFilter___.registered = true;
    ___wsFilter___.active = true;
})()