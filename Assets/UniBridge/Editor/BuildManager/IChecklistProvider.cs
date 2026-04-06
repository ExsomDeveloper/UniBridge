namespace UniBridge.Editor
{
    public interface IChecklistProvider
    {
        string Title { get; }
        ChecklistItem[] GetItems();
    }
}
