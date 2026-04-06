using UnityEditor;
using UnityEngine;

namespace UniBridge.Editor
{
    internal static class EditorConfigHelper
    {
        private const string ResourcesFolder = "Assets/UniBridge/Resources";

        /// <summary>
        /// Loads a config by name. If the asset is inside the package (read-only),
        /// automatically copies it to the project's Assets/UniBridge/Resources/ and returns
        /// a writable copy.
        /// </summary>
        internal static T EnsureProjectAsset<T>(string name) where T : ScriptableObject
        {
            // 1. Project copy already exists — always use it
            //    (including after a package update — the project does not lose its settings)
            string destPath = $"{ResourcesFolder}/{name}.asset";
            var projectAsset = AssetDatabase.LoadAssetAtPath<T>(destPath);
            if (projectAsset != null)
                return projectAsset;

            // 2. No project copy: search via Resources.Load (will find the package default)
            var packageAsset = Resources.Load<T>(name);
            if (packageAsset == null)
                return null;

            string assetPath = AssetDatabase.GetAssetPath(packageAsset);
            if (!assetPath.StartsWith("Packages/"))
                return packageAsset; // В проекте, но по другому пути (edge case)

            // 3. Config from package: copy to project
            if (!AssetDatabase.IsValidFolder("Assets/UniBridge"))
                AssetDatabase.CreateFolder("Assets", "UniBridge");
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets/UniBridge", "Resources");

            AssetDatabase.CopyAsset(assetPath, destPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[UniBridge] Конфиг '{name}' скопирован из пакета в {destPath}");
            return AssetDatabase.LoadAssetAtPath<T>(destPath);
        }
    }
}
