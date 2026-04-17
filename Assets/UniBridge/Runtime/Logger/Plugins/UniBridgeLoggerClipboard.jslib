mergeInto(LibraryManager.library, {

    UniBridgeLogger_CopyToClipboard: function (textPtr, onOk, onFail) {
        try {
            var text = UTF8ToString(textPtr);
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text).then(function () {
                    {{{ makeDynCall('vi', 'onOk') }}}(1);
                }).catch(function (e) {
                    console.warn('[UniBridgeLogger] clipboard.writeText failed:', e);
                    {{{ makeDynCall('vi', 'onFail') }}}(0);
                });
            } else {
                console.warn('[UniBridgeLogger] navigator.clipboard unavailable');
                {{{ makeDynCall('vi', 'onFail') }}}(0);
            }
        } catch (e) {
            console.error('[UniBridgeLogger] copy failed:', e);
            {{{ makeDynCall('vi', 'onFail') }}}(0);
        }
    },

    UniBridgeLogger_DownloadAsFile: function (textPtr, namePtr) {
        try {
            var text = UTF8ToString(textPtr);
            var name = UTF8ToString(namePtr);
            var blob = new Blob([text], { type: 'text/plain;charset=utf-8' });
            var url  = URL.createObjectURL(blob);
            var a = document.createElement('a');
            a.href = url;
            a.download = name;
            a.style.display = 'none';
            document.body.appendChild(a);
            a.click();
            setTimeout(function () {
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
            }, 1000);
        } catch (e) {
            console.error('[UniBridgeLogger] download failed:', e);
        }
    }
});
