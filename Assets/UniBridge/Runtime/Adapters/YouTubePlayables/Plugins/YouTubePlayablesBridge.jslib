mergeInto(LibraryManager.library, {

    // Unified log tag for easy filtering. Grep the overlay for "[YT:jslib]" to see only JS-side traces.
    // Status codes for *_FirstFrameReady / *_GameReady / Register*Callback ack:
    //   0 = ytgame undefined
    //   1 = required submodule (ytgame.game / ytgame.system) missing
    //   2 = OK (call dispatched)
    //   3 = native threw

    // ── Lifecycle ────────────────────────────────────────────────────────────

    YTPlayables_FirstFrameReady: function (statusCb) {
        console.log('[YT:jslib] FirstFrameReady enter');
        var code = 0;
        try {
            if (typeof ytgame === 'undefined') {
                code = 0;
                console.warn('[YT:jslib] FirstFrameReady: ytgame is undefined');
            } else if (!ytgame.game) {
                code = 1;
                console.warn('[YT:jslib] FirstFrameReady: ytgame.game is missing (keys=' + Object.keys(ytgame).join(',') + ')');
            } else {
                ytgame.game.firstFrameReady();
                code = 2;
                console.log('[YT:jslib] ytgame.game.firstFrameReady() dispatched');
            }
        } catch (e) {
            code = 3;
            console.error('[YT:jslib] FirstFrameReady threw:', e);
        }
        console.log('[YT:jslib] FirstFrameReady exit code=' + code);
        if (statusCb) {{{ makeDynCall('vi', 'statusCb') }}}(code);
    },

    YTPlayables_GameReady: function (statusCb) {
        console.log('[YT:jslib] GameReady enter');
        var code = 0;
        try {
            if (typeof ytgame === 'undefined') {
                code = 0;
                console.warn('[YT:jslib] GameReady: ytgame is undefined');
            } else if (!ytgame.game) {
                code = 1;
                console.warn('[YT:jslib] GameReady: ytgame.game is missing');
            } else {
                ytgame.game.gameReady();
                code = 2;
                console.log('[YT:jslib] ytgame.game.gameReady() dispatched');
            }
        } catch (e) {
            code = 3;
            console.error('[YT:jslib] GameReady threw:', e);
        }
        console.log('[YT:jslib] GameReady exit code=' + code);
        if (statusCb) {{{ makeDynCall('vi', 'statusCb') }}}(code);
    },

    YTPlayables_InPlayablesEnv: function () {
        var defined = (typeof ytgame !== 'undefined');
        var inEnv   = defined && !!ytgame.IN_PLAYABLES_ENV;
        // Not logging per-call — this is often called every frame. Log once via a latch.
        if (!Module._ub_yt_env_logged) {
            Module._ub_yt_env_logged = true;
            console.log('[YT:jslib] InPlayablesEnv first-check: ytgameDefined=' + defined + ' inEnv=' + inEnv);
        }
        return inEnv ? 1 : 0;
    },

    // ── System ───────────────────────────────────────────────────────────────

    YTPlayables_IsAudioEnabled: function () {
        if (typeof ytgame !== 'undefined' && ytgame.system) {
            try {
                var v = ytgame.system.isAudioEnabled() ? 1 : 0;
                if (!Module._ub_yt_audio_logged) {
                    Module._ub_yt_audio_logged = true;
                    console.log('[YT:jslib] IsAudioEnabled first-check → ' + v);
                }
                return v;
            } catch (e) {
                console.error('[YT:jslib] IsAudioEnabled threw:', e);
                return 1;
            }
        }
        if (!Module._ub_yt_audio_logged) {
            Module._ub_yt_audio_logged = true;
            console.warn('[YT:jslib] IsAudioEnabled: ytgame.system missing, defaulting to 1');
        }
        return 1;
    },

    YTPlayables_GetLanguage: function () {
        var lang = Module._ytplayables_language || 'en';
        var bufferSize = lengthBytesUTF8(lang) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(lang, buffer, bufferSize);
        if (!Module._ub_yt_lang_logged) {
            Module._ub_yt_lang_logged = true;
            console.log('[YT:jslib] GetLanguage first-read → "' + lang + '"');
        }
        return buffer;
    },

    YTPlayables_FreeString: function (ptr) {
        if (ptr) _free(ptr);
    },

    YTPlayables_FetchLanguage: function (callbackPtr) {
        console.log('[YT:jslib] FetchLanguage enter');
        if (typeof ytgame === 'undefined') {
            console.warn('[YT:jslib] FetchLanguage: ytgame undefined — cached default "en"');
            Module._ytplayables_language = Module._ytplayables_language || 'en';
            return;
        }
        if (!ytgame.system) {
            console.warn('[YT:jslib] FetchLanguage: ytgame.system missing');
            return;
        }
        try {
            ytgame.system.getLanguage().then(function (lang) {
                Module._ytplayables_language = lang;
                console.log('[YT:jslib] FetchLanguage resolved: "' + lang + '"');
                if (callbackPtr) {
                    var bufferSize = lengthBytesUTF8(lang) + 1;
                    var buffer = _malloc(bufferSize);
                    stringToUTF8(lang, buffer, bufferSize);
                    {{{ makeDynCall('vi', 'callbackPtr') }}}(buffer);
                    _free(buffer);
                }
            }).catch(function (e) {
                console.warn('[YT:jslib] FetchLanguage rejected:', e);
                Module._ytplayables_language = Module._ytplayables_language || 'en';
            });
        } catch (e) {
            console.error('[YT:jslib] FetchLanguage threw:', e);
        }
    },

    YTPlayables_RegisterAudioCallback: function (callbackPtr, statusCb) {
        console.log('[YT:jslib] RegisterAudioCallback enter');
        var code = 0;
        try {
            if (typeof ytgame === 'undefined') {
                code = 0; console.warn('[YT:jslib] RegisterAudioCallback: ytgame undefined');
            } else if (!ytgame.system) {
                code = 1; console.warn('[YT:jslib] RegisterAudioCallback: ytgame.system missing');
            } else {
                ytgame.system.onAudioEnabledChange(function (isEnabled) {
                    console.log('[YT:jslib] onAudioEnabledChange fired → ' + isEnabled);
                    {{{ makeDynCall('vi', 'callbackPtr') }}}(isEnabled ? 1 : 0);
                });
                code = 2;
                console.log('[YT:jslib] onAudioEnabledChange subscribed');
            }
        } catch (e) { code = 3; console.error('[YT:jslib] RegisterAudioCallback threw:', e); }
        if (statusCb) {{{ makeDynCall('vi', 'statusCb') }}}(code);
    },

    YTPlayables_RegisterPauseCallback: function (callbackPtr, statusCb) {
        console.log('[YT:jslib] RegisterPauseCallback enter');
        var code = 0;
        try {
            if (typeof ytgame === 'undefined') {
                code = 0; console.warn('[YT:jslib] RegisterPauseCallback: ytgame undefined');
            } else if (!ytgame.system) {
                code = 1; console.warn('[YT:jslib] RegisterPauseCallback: ytgame.system missing');
            } else {
                ytgame.system.onPause(function () {
                    console.log('[YT:jslib] onPause fired');
                    {{{ makeDynCall('vi', 'callbackPtr') }}}(1);
                });
                code = 2;
                console.log('[YT:jslib] onPause subscribed');
            }
        } catch (e) { code = 3; console.error('[YT:jslib] RegisterPauseCallback threw:', e); }
        if (statusCb) {{{ makeDynCall('vi', 'statusCb') }}}(code);
    },

    YTPlayables_RegisterResumeCallback: function (callbackPtr, statusCb) {
        console.log('[YT:jslib] RegisterResumeCallback enter');
        var code = 0;
        try {
            if (typeof ytgame === 'undefined') {
                code = 0; console.warn('[YT:jslib] RegisterResumeCallback: ytgame undefined');
            } else if (!ytgame.system) {
                code = 1; console.warn('[YT:jslib] RegisterResumeCallback: ytgame.system missing');
            } else {
                ytgame.system.onResume(function () {
                    console.log('[YT:jslib] onResume fired');
                    {{{ makeDynCall('vi', 'callbackPtr') }}}(1);
                });
                code = 2;
                console.log('[YT:jslib] onResume subscribed');
            }
        } catch (e) { code = 3; console.error('[YT:jslib] RegisterResumeCallback threw:', e); }
        if (statusCb) {{{ makeDynCall('vi', 'statusCb') }}}(code);
    },

    // ── Cloud Saves ──────────────────────────────────────────────────────────

    YTPlayables_LoadData: function (callbackSuccessPtr, callbackFailPtr) {
        console.log('[YT:jslib] LoadData enter');
        if (typeof ytgame === 'undefined') {
            console.warn('[YT:jslib] LoadData: ytgame undefined → fail');
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        if (!ytgame.game) {
            console.warn('[YT:jslib] LoadData: ytgame.game missing → fail');
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        try {
            ytgame.game.loadData().then(function (data) {
                if (data == null) data = '';
                console.log('[YT:jslib] LoadData resolved: ' + data.length + ' chars');
                var bufferSize = lengthBytesUTF8(data) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(data, buffer, bufferSize);
                {{{ makeDynCall('vi', 'callbackSuccessPtr') }}}(buffer);
                _free(buffer);
            }).catch(function (e) {
                console.warn('[YT:jslib] LoadData rejected:', e);
                {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            });
        } catch (e) {
            console.error('[YT:jslib] LoadData threw:', e);
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
        }
    },

    YTPlayables_SaveData: function (dataPtr, callbackSuccessPtr, callbackFailPtr) {
        console.log('[YT:jslib] SaveData enter');
        if (typeof ytgame === 'undefined') {
            console.warn('[YT:jslib] SaveData: ytgame undefined → fail');
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        if (!ytgame.game) {
            console.warn('[YT:jslib] SaveData: ytgame.game missing → fail');
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        var data = UTF8ToString(dataPtr);
        var bytes = lengthBytesUTF8(data);
        console.log('[YT:jslib] SaveData payload ' + bytes + ' bytes');
        // Spec: saveData payload must be well-formed UTF-16, max 3 MiB.
        var MAX_BYTES = 3 * 1024 * 1024;
        if (typeof data.isWellFormed === 'function' && !data.isWellFormed()) {
            console.error('[YT:jslib] SaveData rejected: malformed UTF-16 (lone surrogate)');
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        if (bytes > MAX_BYTES) {
            console.error('[YT:jslib] SaveData rejected: payload ' + bytes + ' bytes > 3 MiB limit');
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        try {
            ytgame.game.saveData(data).then(function () {
                console.log('[YT:jslib] SaveData resolved OK');
                {{{ makeDynCall('vi', 'callbackSuccessPtr') }}}(1);
            }).catch(function (e) {
                console.warn('[YT:jslib] SaveData rejected:', e);
                {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            });
        } catch (e) {
            console.error('[YT:jslib] SaveData threw:', e);
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
        }
    },

    // ── Ads ──────────────────────────────────────────────────────────────────

    YTPlayables_RequestInterstitialAd: function (callbackSuccessPtr, callbackFailPtr) {
        console.log('[YT:jslib] RequestInterstitialAd enter');
        if (typeof ytgame === 'undefined') {
            console.warn('[YT:jslib] RequestInterstitialAd: ytgame undefined → fail');
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        if (!ytgame.ads) {
            console.warn('[YT:jslib] RequestInterstitialAd: ytgame.ads missing → fail');
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        try {
            ytgame.ads.requestInterstitialAd().then(function () {
                console.log('[YT:jslib] RequestInterstitialAd resolved OK');
                {{{ makeDynCall('vi', 'callbackSuccessPtr') }}}(1);
            }).catch(function (e) {
                console.warn('[YT:jslib] RequestInterstitialAd rejected:', e);
                {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            });
        } catch (e) {
            console.error('[YT:jslib] RequestInterstitialAd threw:', e);
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
        }
    },

    // ── Engagement ───────────────────────────────────────────────────────────

    YTPlayables_SendScore: function (score, callbackSuccessPtr, callbackFailPtr) {
        console.log('[YT:jslib] SendScore enter value=' + score);
        if (typeof ytgame === 'undefined') {
            console.warn('[YT:jslib] SendScore: ytgame undefined → fail');
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        if (!ytgame.engagement) {
            console.warn('[YT:jslib] SendScore: ytgame.engagement missing → fail');
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            return;
        }
        try {
            ytgame.engagement.sendScore({ value: score }).then(function () {
                console.log('[YT:jslib] SendScore resolved OK');
                {{{ makeDynCall('vi', 'callbackSuccessPtr') }}}(1);
            }).catch(function (e) {
                console.warn('[YT:jslib] SendScore rejected:', e);
                {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
            });
        } catch (e) {
            console.error('[YT:jslib] SendScore threw:', e);
            {{{ makeDynCall('vi', 'callbackFailPtr') }}}(0);
        }
    }
});
