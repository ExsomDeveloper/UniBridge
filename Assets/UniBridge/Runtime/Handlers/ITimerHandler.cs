using System.Collections;

namespace UniBridge
{
    public interface ITimerHandler
    {
        void Initialize();
        IEnumerator Execute();
    }
}
