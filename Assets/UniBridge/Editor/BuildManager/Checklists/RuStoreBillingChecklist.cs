using System.IO;

namespace UniBridge.Editor
{
    public class RuStoreBillingChecklist : IChecklistProvider
    {
        private const string DefaultDeeplink = "yourapp://rustore";
        private const string ManifestPath    = "Assets/Plugins/Android/UniBridgeMobileKit/RuStoreUnityPay/AndroidManifest.xml";

        public string Title => "RuStore Pay";

        public ChecklistItem[] GetItems()
        {
            var config       = RuStoreAndroidConfigurator.LoadConfig();
            string consoleId = config?.RuStoreSettings.ConsoleApplicationId ?? "";
            string deeplink  = config?.RuStoreSettings.DeeplinkPrefix ?? "";

            bool consoleOk  = !string.IsNullOrEmpty(consoleId);
            bool deeplinkOk = !string.IsNullOrEmpty(deeplink) && deeplink != DefaultDeeplink;
            bool manifestOk = File.Exists(ManifestPath);

            return new[]
            {
                new ChecklistItem("Console Application ID заполнен", consoleOk,  "Откройте UniBridge > Purchases > Settings"),
                new ChecklistItem("Deeplink Prefix настроен",         deeplinkOk, "Задайте уникальный deeplink в UniBridge > Purchases > Settings"),
                new ChecklistItem("AndroidManifest.xml создан",       manifestOk, "Переключитесь на RuStore — файл создаётся автоматически"),
            };
        }
    }
}
