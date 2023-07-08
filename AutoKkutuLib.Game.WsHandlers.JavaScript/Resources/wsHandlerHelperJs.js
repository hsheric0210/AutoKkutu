
___roomMode2GameMode___ = function (id) {
    if (!___ruleKeys___)
        ___ruleKeys___ = Object.keys(JSON.parse(document.getElementById('RULE').textContent));
    return ___ruleKeys___[id];
}

___helperRegistered___ = true;