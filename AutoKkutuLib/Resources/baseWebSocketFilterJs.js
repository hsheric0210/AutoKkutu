(function () {
    ___wsFilter___['___welcome___'] = function (d) {
        return { 'type': 'welcome', 'id': d.id };
    };
    ___wsFilter___['___turnStart___'] = function (d) { return true; };
    ___wsFilter___['___turnEnd___'] = function (d) { return true; };
    ___wsFilter___['___turnError___'] = function (d) { return true; };
    ___wsFilter___.registered = true;
    ___wsFilter___.active = true;
})()