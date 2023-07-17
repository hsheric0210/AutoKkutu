(function () {
    ___wsFilter___['___room___'] = function (d) {
        if (d && d.room && d.room.players && Array.prototype.includes.call(d.room.players, '___userId___')) {
            function onlyPlayerId(plist) { if (!plist) { return []; } return Array.prototype.map.call(plist, p => typeof p === 'string' ? p : p.id.toString()); }
            return {
                'type': 'room',
                'room': {
                    'players': onlyPlayerId(d.room.players),
                    'gaming': d.room.gaming,
                    'mode': d.room.mode,
                    'game': {
                        'seq': onlyPlayerId(d.room.game?.seq)
                    }
                }
            }
        }
        return null;
    };
})()