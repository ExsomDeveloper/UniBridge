using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UniBridge.Editor
{
    /// <summary>
    /// Strips the UB_LOGGER template panel block from the built index.html when
    /// UniBridgeLoggerConfig.Enabled = false. Runs only for WebGL builds.
    /// </summary>
    internal class LoggerWebGLPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 200;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;

            var config = Resources.Load<UniBridgeLoggerConfig>(nameof(UniBridgeLoggerConfig));
            if (config != null && config.Enabled)
            {
                Debug.Log($"[{nameof(LoggerWebGLPostprocessor)}] LoggerConfig.Enabled=true → keeping template log panel in build");
                return;
            }

            var outputPath = report.summary.outputPath;
            if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
            {
                Debug.LogWarning($"[{nameof(LoggerWebGLPostprocessor)}] Build output not found at \"{outputPath}\" — skipping panel strip");
                return;
            }

            var indexPath = Path.Combine(outputPath, "index.html");
            if (!File.Exists(indexPath))
            {
                Debug.LogWarning($"[{nameof(LoggerWebGLPostprocessor)}] index.html not found at \"{indexPath}\" — skipping panel strip");
                return;
            }

            var html = File.ReadAllText(indexPath);
            var stripped = StripMarkers(html, out int blocksRemoved);
            if (blocksRemoved == 0)
            {
                Debug.Log($"[{nameof(LoggerWebGLPostprocessor)}] No UB_LOGGER markers in index.html — nothing to strip (template is not the UniBridge YT template, or already stripped)");
                return;
            }

            File.WriteAllText(indexPath, stripped);
            Debug.Log($"[{nameof(LoggerWebGLPostprocessor)}] LoggerConfig.Enabled=false → stripped {blocksRemoved} UB_LOGGER block(s) from index.html");
        }

        // Removes both <!-- UB_LOGGER_BEGIN --> ... <!-- UB_LOGGER_END --> and
        // /* UB_LOGGER_BEGIN */ ... /* UB_LOGGER_END */ block pairs.
        private static string StripMarkers(string html, out int blocksRemoved)
        {
            int count = 0;

            var htmlPattern = new Regex(@"<!--\s*UB_LOGGER_BEGIN\s*-->.*?<!--\s*UB_LOGGER_END\s*-->",
                RegexOptions.Singleline);
            var cssPattern  = new Regex(@"/\*\s*UB_LOGGER_BEGIN\s*\*/.*?/\*\s*UB_LOGGER_END\s*\*/",
                RegexOptions.Singleline);

            html = htmlPattern.Replace(html, m => { count++; return string.Empty; });
            html = cssPattern.Replace(html, m => { count++; return string.Empty; });

            blocksRemoved = count;
            return html;
        }
    }
}
