using System.IO;
using UnityEditor;
using UpmPackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace UniBridge.Editor
{
    public static class YouTubePlayablesTemplateInstaller
    {
        private const string PackageName    = "com.unibridge.core";
        private const string TemplateSubDir = "Editor/WebGLTemplates~/YouTubePlayables";
        private const string TargetDir      = "Assets/WebGLTemplates/YouTubePlayables";

        private static readonly string[] Files = { "index.html", "thumbnail.png", "README.txt" };

        public static bool IsInstalled() => File.Exists(Path.Combine(TargetDir, "index.html"));

        public static void EnsureInstalled() => Install(force: false);

        [MenuItem("UniBridge/WebGL Templates/Reinstall YouTube Playables (overwrite)")]
        public static void ForceReinstall() => Install(force: true);

        private static void Install(bool force)
        {
            if (!force && IsInstalled()) return;

            var sourceDir = FindSourceDir();
            if (sourceDir == null || !Directory.Exists(sourceDir))
            {
                Debug.LogWarning($"[UniBridge] YouTube Playables template source not found. Searched:\n" +
                                 $"  UPM: Packages/{PackageName}/{TemplateSubDir}\n" +
                                 $"  Embedded: Assets/UniBridge/{TemplateSubDir}");
                return;
            }

            Directory.CreateDirectory(TargetDir);

            int copied = 0;
            foreach (var file in Files)
            {
                var src = Path.Combine(sourceDir, file);
                var dst = Path.Combine(TargetDir, file);
                if (!File.Exists(src)) continue;
                if (File.Exists(dst) && !force) continue;
                File.Copy(src, dst, overwrite: true);
                copied++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[UniBridge] YouTube Playables WebGL template {(force ? "reinstalled" : "installed")}: {copied} files copied to {TargetDir}");

            if (!force)
                EditorUtility.DisplayDialog("UniBridge",
                    "WebGL-шаблон YouTubePlayables установлен.\n\n" +
                    "Перезапустите Unity, чтобы шаблон появился в Player Settings > WebGL > Resolution and Presentation.",
                    "OK");
        }

        private static string FindSourceDir()
        {
            // UPM: package resolved via PackageInfo
            var info = UpmPackageInfo.FindForAssetPath($"Packages/{PackageName}/package.json");
            if (info != null)
            {
                var dir = Path.Combine(info.resolvedPath, TemplateSubDir);
                if (Directory.Exists(dir)) return dir;
            }

            // Embedded in Assets (development)
            var assetsDir = Path.Combine("Assets/UniBridge", TemplateSubDir);
            if (Directory.Exists(assetsDir)) return assetsDir;

            return null;
        }
    }
}
