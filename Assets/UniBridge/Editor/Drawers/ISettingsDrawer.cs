namespace UniBridge.Editor
{
    public interface ISettingsDrawer
    {
        void DrawInspector();
    }

    public class EmptySettingsDrawer : ISettingsDrawer
    {
        public void DrawInspector()
        {
            OnDrawInspector();
        }

        protected virtual void OnDrawInspector() { }
    }
}
