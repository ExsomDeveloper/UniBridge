mergeInto(LibraryManager.library, {

    // ── Lifecycle ────────────────────────────────────────────────────────────

    YTPlayables_FirstFrameReady: function () {
        if (typeof ytgame !== 'undefined' && ytgame.game) {
            ytgame.game.firstFrameReady();
        }
    },

    YTPlayables_GameReady: function () {
        if (typeof ytgame !== 'undefined' && ytgame.game) {
            ytgame.game.gameReady();
        }
    },

    YTPlayables_InPlayablesEnv: function () {
        return (typeof ytgame !== 'undefined' && ytgame.IN_PLAYABLES_ENV) ? 1 : 0;
    },

    // ── System ───────────────────────────────────────────────────────────────

    YTPlayables_IsAudioEnabled: function () {
        if (typeof ytgame !== 'undefined' && ytgame.system) {
            return ytgame.system.isAudioEnabled() ? 1 : 0;
        }
        return 1;
    },

    YTPlayables_GetLanguage: function () {
        // getLanguage() returns a Promise — we cache the result and return it synchronously.
        // The actual async fetch is done in YTPlayables_FetchLanguage.
        var lang = Module._ytplayables_language || 'en';
        var bufferSize = lengthBytesUTF8(lang) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(lang, buffer, bufferSize);
        return buffer;
    },

    YTPlayables_FetchLanguage: function (callbackPtr) {
        if (typeof ytgame !== 'undefined' && ytgame.system) {
            ytgame.system.getLanguage().then(function (lang) {
                Module._ytplayables_language = lang;
                if (callbackPtr) {
                    var bufferSize = lengthBytesUTF8(lang) + 1;
                    var buffer = _malloc(bufferSize);
                    stringToUTF8(lang, buffer, bufferSize);
                    {{{ makeDynCall('vi', 'callbackPtr') }}}(buffer);
                    _free(buffer);
                }
            }).catch(function () {
                Module._ytplayables_language = 'en';
            });
        }
    },

    YTPlayables_RegisterAudioCallback: function (callbackPtr) {
        if (typeof ytgame !== 'undefined' && ytgame.system) {
            ytgame.system.onAudioEnabledChange(function (isEnabled) {
                {{{ makeDynCall('vi', 'callbackPtr') }}}(isEnabled ? 1 : 0);
            });
        }
    },

    YTPlayables_RegisterPauseCallback: function (callbackPtr) {
        if (typeof ytgame !== 'undefined' && ytgame.system) {
            ytgame.system.onPause(function () {
                {{{ makeDynCall('vi', 'callbackPtr') }}}(1);
            });
        }
    },

    YTPlayables_RegisterResumeCallback: function (callbackPtr) {
        if (typeof ytgame !== 'undefined' && ytgame.system) {
            ytgame.system.onResume(function () {
                {{{ makeDynCall('vi', 'callbackPtr') }}}(1);
            });
        }
    },

    // ── Cloud Saves ──────────────────────────────────────────────────────────

    YTPlayables_LoadData: function (callbackSuccessPtr, callbackFailPtr) {
        if (typeof ytgame === 'undefined' || !ytgame.game) {
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        ytgame.game.loadData().then(function (data) {
            if (data == null) data = '';
            var bufferSize = lengthBytesUTF8(data) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(data, buffer, bufferSize);
            {{{ makeDynCall('vi', 'callbackSuccessPtr') }}}(buffer);
            _free(buffer);
        }).catch(function () {
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
        });
    },

    YTPlayables_SaveData: function (dataPtr, callbackSuccessPtr, callbackFailPtr) {
        if (typeof ytgame === 'undefined' || !ytgame.game) {
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        var data = UTF8ToString(dataPtr);
        ytgame.game.saveData(data).then(function () {
            {{{ makeDynCall('vi', 'callbackSuccessPtr') }}}(1);
        }).catch(function () {
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
        });
    },

    // ── Ads ──────────────────────────────────────────────────────────────────

    YTPlayables_RequestInterstitialAd: function (callbackSuccessPtr, callbackFailPtr) {
        if (typeof ytgame === 'undefined' || !ytgame.ads) {
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        ytgame.ads.requestInterstitialAd().then(function () {
            {{{ makeDynCall('vi', 'callbackSuccessPtr') }}}(1);
        }).catch(function () {
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
        });
    },

    // ── Engagement ───────────────────────────────────────────────────────────

    YTPlayables_SendScore: function (score, callbackSuccessPtr, callbackFailPtr) {
        if (typeof ytgame === 'undefined' || !ytgame.engagement) {
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        ytgame.engagement.sendScore({ value: score }).then(function () {
            {{{ makeDynCall('vi', 'callbackSuccessPtr') }}}(1);
        }).catch(function () {
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
        });
    }
});
