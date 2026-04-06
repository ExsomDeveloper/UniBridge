using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniBridge.Editor
{
    public class UniBridgeShareChecklist : IChecklistProvider
    {
        private const string ManifestPath  = "Assets/Plugins/Android/UniBridgeMobileKit/UniBridgeShareAndroid.androidlib/AndroidManifest.xml";
        private const string FilePathsPath = "Assets/Plugins/Android/UniBridgeMobileKit/UniBridgeShareAndroid.androidlib/res/xml/unibridgeshare_file_paths.xml";

        public string Title => "UniBridgeShare Android (FileProvider)";

        public ChecklistItem[] GetItems()
        {
            var shareAdapter = GetShareAdapter();
            if (shareAdapter != "UNIBRIDGESHARE_ANDROID")
                return System.Array.Empty<ChecklistItem>();

            bool manifestOk  = File.Exists(ManifestPath);
            bool filePathsOk = File.Exists(FilePathsPath);

            return new[]
            {
                new ChecklistItem(
                    "AndroidManifest.xml создан (FileProvider)",
                    manifestOk,
                    "Нажмите «Выбрать» с адаптером Android Native Share — создаётся автоматически"),
                new ChecklistItem(
                    "res/xml/unibridgeshare_file_paths.xml создан",
                    filePathsOk,
                    "Нажмите «Выбрать» с адаптером Android Native Share — создаётся автоматически"),
            };
        }

        private static string GetShareAdapter()
        {
            var config = Resources.Load<Object>("UniBridgeShareConfig");
            if (config == null) return "";
            return new SerializedObject(config).FindProperty("PreferredShareAdapter")?.stringValue ?? "";
        }
    }
}
