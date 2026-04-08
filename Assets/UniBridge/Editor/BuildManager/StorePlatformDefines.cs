using System;
using UnityEditor;

namespace UniBridge.Editor
{
    public static class StorePlatformDefines
    {
        // ── Constants (used for default presets) ─────────────────────────────

        public const string STORE_GOOGLEPLAY = "UNIBRIDGE_STORE_GOOGLEPLAY";
        public const string STORE_RUSTORE    = "UNIBRIDGE_STORE_RUSTORE";
        public const string STORE_APPSTORE   = "UNIBRIDGE_STORE_APPSTORE";
        public const string STORE_PLAYGAMA   = "UNIBRIDGE_STORE_PLAYGAMA";
        public const string STORE_YOUTUBE    = "UNIBRIDGE_STORE_YOUTUBE";
        public const string STORE_EDITOR     = "UNIBRIDGE_STORE_EDITOR";

        // ── Dynamic API (reads from StorePresetsManager) ──────────────────────

        public static string[] AllStoreDefines
        {
            get
            {
                var presets = StorePresetsManager.Load();
                var result  = new string[presets.Count];
                for (int i = 0; i < presets.Count; i++)
                    result[i] = presets[i].define;
                return result;
            }
        }

        public static BuildTarget GetExpectedBuildTarget(string define)
        {
            foreach (var p in StorePresetsManager.Load())
            {
                if (p.define != define) continue;
                if (p.buildTarget == "Editor" || string.IsNullOrEmpty(p.buildTarget))
                    return BuildTarget.NoTarget;
                if (Enum.TryParse<BuildTarget>(p.buildTarget, out var target))
                    return target;
                throw new ArgumentException(
                    $"[UniBridge] Store preset '{define}' has invalid buildTarget '{p.buildTarget}'");
            }
            throw new ArgumentException($"[UniBridge] Unknown store define: {define}");
        }

        public static string GetDisplayName(string define)
        {
            foreach (var p in StorePresetsManager.Load())
                if (p.define == define) return p.displayName;
            return define; // fallback: show the raw define string
        }

        public static string GetCurrentStoreDefine()
        {
            var group   = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

            foreach (var storeDefine in AllStoreDefines)
            {
                if (ArrayContainsDefine(defines, storeDefine))
                    return storeDefine;
            }

            return null;
        }

        public static void SetStoreDefine(string define)
        {
            // Remove all known store defines
            foreach (var d in AllStoreDefines)
                ScriptingDefinesManager.RemoveDefine(d);

            // Clean up obsolete adapter defines from previous implementation
            foreach (var d in AdapterDefines.ObsoleteAdapterDefines)
                ScriptingDefinesManager.RemoveDefine(d);

            // Add the selected store define
            if (!string.IsNullOrEmpty(define))
                ScriptingDefinesManager.AddDefine(define);
        }

        private static bool ArrayContainsDefine(string defines, string define)
        {
            var parts = defines.Split(';');
            foreach (var part in parts)
            {
                if (part.Trim() == define)
                    return true;
            }
            return false;
        }
    }
}
