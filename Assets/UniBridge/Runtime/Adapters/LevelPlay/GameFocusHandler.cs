#if UNIBRIDGE_LEVELPLAY
using Unity.Services.LevelPlay;
using UnityEngine;

namespace UniBridge
{
    public class GameFocusHandler : MonoBehaviour
    {
        public void OnApplicationPause(bool isPaused) => LevelPlay.SetPauseGame(isPaused);
    }
}
#endif
