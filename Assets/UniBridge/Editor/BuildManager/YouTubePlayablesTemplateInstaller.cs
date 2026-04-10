using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniBridge.Editor
{
    public static class YouTubePlayablesTemplateInstaller
    {
        private const string SourceDir = "Assets/UniBridge/Editor/WebGLTemplates~/YouTubePlayables";
        private const string TargetDir = "Assets/WebGLTemplates/YouTubePlayables";

        private static readonly string[] Files = { "index.html", "thumbnail.png", "README.txt" };

        public static bool IsInstalled() => File.Exists(Path.Combine(TargetDir, "index.html"));

        public static void EnsureInstalled()
        {
            if (IsInstalled()) return;

            if (!Directory.Exists(SourceDir))
            {
                Debug.LogWarning($"[UniBridge] YouTube Playables template source not found: {SourceDir}");
                return;
            }

            Directory.CreateDirectory(TargetDir);

            foreach (var file in Files)
            {
                var src = Path.Combine(SourceDir, file);
                var dst = Path.Combine(TargetDir, file);
                if (File.Exists(src) && !File.Exists(dst))
                    File.Copy(src, dst);
            }

            AssetDatabase.Refresh();
            Debug.Log("[UniBridge] YouTube Playables WebGL template installed in Assets/WebGLTemplates/YouTubePlayables/");
        }
    }
}
