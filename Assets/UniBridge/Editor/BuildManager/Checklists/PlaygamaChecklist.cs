using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace UniBridge.Editor
{
    public class PlaygamaChecklist : IChecklistProvider
    {
        private const string BridgeConfigPath =
            "Assets/WebGLTemplates/Bridge/playgama-bridge-config.json";

        private static readonly Regex PlatformIdRegex = new Regex(
            @"""(gameId|appId|devId|publisherId|projectId|publicToken|adsgramBlockId|xsollaProjectId|adSenseId|hostId|channelId|adsenseId)""\s*:\s*""[^""]+""",
            RegexOptions.Compiled);

        public string Title => "Playgama";

        public ChecklistItem[] GetItems()
        {
            bool isWebGL          = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
            bool isBridgeTemplate = PlayerSettings.WebGL.template == "PROJECT:Bridge";
            bool hasPlatformId    = CheckBridgePlatformId();

            return new[]
            {
                new ChecklistItem("Build Target: WebGL", isWebGL,
                    "Переключите Build Target на WebGL в File > Build Settings"),
                new ChecklistItem("Шаблон WebGL: Bridge", isBridgeTemplate,
                    "Выберите шаблон Bridge в Player Settings > WebGL > Resolution and Presentation"),
                new ChecklistItem("ID платформы заполнен в Bridge конфиге", hasPlatformId,
                    "Заполните ID платформы в Assets/WebGLTemplates/Bridge/playgama-bridge-config.json"),
            };
        }

        private static bool CheckBridgePlatformId()
        {
            if (!File.Exists(BridgeConfigPath)) return false;
            var content = File.ReadAllText(BridgeConfigPath);
            return PlatformIdRegex.IsMatch(content);
        }
    }
}
