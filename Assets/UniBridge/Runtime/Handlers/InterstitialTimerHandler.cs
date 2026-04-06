using System.Collections;
using UnityEngine;

namespace UniBridge
{
    public class InterstitialTimerHandler : ITimerHandler
    {
        private readonly string _placementName;
        private bool _isCompleted;

        public InterstitialTimerHandler(string placementName = "")
        {
            _placementName = placementName;
        }

        public void Initialize() { }

        public IEnumerator Execute()
        {
            _isCompleted = false;

            UniBridge.ShowInterstitial(
                OnInterstitialFinished,
                _placementName
            );

            yield return new WaitUntil(() => _isCompleted);
        }

        private void OnInterstitialFinished(AdStatus status)
        {
            _isCompleted = true;
        }
    }
}
