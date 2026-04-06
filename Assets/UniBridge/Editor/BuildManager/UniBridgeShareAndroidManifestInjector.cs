using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace UniBridge.Editor
{
    /// <summary>
    /// Injects the FileProvider &lt;provider&gt; declaration and unibridgeshare_file_paths.xml
    /// directly into the generated unityLibrary Gradle manifest after Unity generates
    /// the Gradle project. This is the most reliable approach — it bypasses Unity's
    /// manifest merging entirely and directly writes to the final Gradle project files.
    /// </summary>
    public class UniBridgeShareAndroidManifestInjector : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;

        private const string ProviderSuffix = ".unibridgeshare.provider";
        private const string ProviderName   = "androidx.core.content.FileProvider";
        private const string AndroidNs      = "http://schemas.android.com/apk/res/android";

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var shareAdapter = GetPreferredShareAdapter();
            if (shareAdapter == AdapterDefines.NoneAdapterKey || shareAdapter == "UNIBRIDGESHARE_MOCK")
                return;

            // path = generated unityLibrary directory
            var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning("[UniBridge] UniBridgeShare: unityLibrary AndroidManifest.xml not found, skipping injection.");
                return;
            }

            var authority = PlayerSettings.applicationIdentifier + ProviderSuffix;

            // 1. Inject <provider> into the manifest
            InjectProvider(manifestPath, authority);

            // 2. Ensure unibridgeshare_file_paths.xml exists in res/xml/
            var resXmlDir = Path.Combine(path, "src/main/res/xml");
            if (!Directory.Exists(resXmlDir))
                Directory.CreateDirectory(resXmlDir);

            var filePathsPath = Path.Combine(resXmlDir, "unibridgeshare_file_paths.xml");
            if (!File.Exists(filePathsPath))
                File.WriteAllText(filePathsPath, FilePathsContent);

            Debug.Log($"[UniBridge] UniBridgeShare FileProvider configured (authority={authority}).");
        }

        private static void InjectProvider(string manifestPath, string authority)
        {
            var doc = new XmlDocument();
            doc.Load(manifestPath);

            var nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("android", AndroidNs);

            // Skip if already declared
            if (doc.SelectSingleNode($"//provider[@android:authorities='{authority}']", nsMgr) != null)
            {
                Debug.Log("[UniBridge] UniBridgeShare: FileProvider already present in manifest.");
                return;
            }

            // Find or create <application>
            var application = doc.SelectSingleNode("//application") as XmlElement;
            if (application == null)
            {
                application = doc.CreateElement("application");
                doc.DocumentElement?.AppendChild(application);
                if (application.ParentNode == null) return;
            }

            // <provider>
            var provider = doc.CreateElement("provider");
            SetAttr(doc, provider, "name",                 ProviderName);
            SetAttr(doc, provider, "authorities",          authority);
            SetAttr(doc, provider, "exported",             "false");
            SetAttr(doc, provider, "grantUriPermissions",  "true");

            // <meta-data>
            var metaData = doc.CreateElement("meta-data");
            SetAttr(doc, metaData, "name",     "android.support.FILE_PROVIDER_PATHS");
            SetAttr(doc, metaData, "resource", "@xml/unibridgeshare_file_paths");
            provider.AppendChild(metaData);

            application.AppendChild(provider);
            doc.Save(manifestPath);
        }

        private static string GetPreferredShareAdapter()
        {
            var config = Resources.Load<Object>("UniBridgeShareConfig");
            if (config == null) return "";
            return new SerializedObject(config).FindProperty("PreferredShareAdapter")?.stringValue ?? "";
        }

        private static void SetAttr(XmlDocument doc, XmlElement elem, string localName, string value)
        {
            var attr = doc.CreateAttribute("android", localName, AndroidNs);
            attr.Value = value;
            elem.Attributes.Append(attr);
        }

        private const string FilePathsContent =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<!-- UNIBRIDGE_UNIBRIDGESHARE_GENERATED -->
<paths>
    <cache-path name=""unibridgeshare"" path=""."" />
</paths>
";
    }
}
