namespace UniBridge.Editor
{
    public class AppMetricaChecklist : IChecklistProvider
    {
        public string Title => "AppMetrica";

        public ChecklistItem[] GetItems() => new[]
        {
            new ChecklistItem("Трекинг покупок (UniBridgePurchases → ReportRevenue)", true),
            new ChecklistItem("Логирование событий (UniBridgeAnalytics.LogEvent)",    true),
        };
    }
}
