using UnityEditor;

namespace UniBridge.Editor
{
    public class YouTubePlayablesChecklist : IChecklistProvider
    {
        public string Title => "YouTube Playables";

        public ChecklistItem[] GetItems()
        {
            bool isWebGL       = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
            bool isInstalled   = YouTubePlayablesTemplateInstaller.IsInstalled();
            bool isYTTemplate  = PlayerSettings.WebGL.template == "PROJECT:YouTubePlayables";

            return new[]
            {
                new ChecklistItem("Build Target: WebGL", isWebGL,
                    "Переключите Build Target на WebGL в File > Build Settings"),
                new ChecklistItem("Шаблон установлен", isInstalled,
                    "Нажмите «Выбрать» ещё раз — шаблон установится автоматически"),
                new ChecklistItem("Шаблон WebGL: YouTubePlayables", isYTTemplate,
                    "Выберите шаблон YouTubePlayables в Player Settings > WebGL > Resolution and Presentation"),
            };
        }
    }
}
