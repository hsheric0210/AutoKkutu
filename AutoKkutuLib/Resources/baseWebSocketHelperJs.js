(function () {
    ___roomMode2GameMode___ = function (id) {
        if (!___ruleKeys___)
            ___ruleKeys___ = Object.keys(JSON.parse(___getElementById___.call(document, 'RULE').textContent));
        return ___ruleKeys___[id];
    }

    ___helperRegistered___ = true;
    return true;
})();