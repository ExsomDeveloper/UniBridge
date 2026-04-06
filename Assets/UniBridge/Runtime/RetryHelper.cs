using System;
using System.Collections;
using UnityEngine;

namespace UniBridge
{
    public class RetryHelper : MonoBehaviour
    {
        private static RetryHelper _instance;

        public static void InvokeAfter(float delay, Action action)
        {
            if (_instance == null)
            {
                var go = new GameObject(nameof(RetryHelper));
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<RetryHelper>();
            }

            _instance.StartCoroutine(_instance.InvokeRoutine(delay, action));
        }

        private IEnumerator InvokeRoutine(float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            action?.Invoke();
        }
    }
}
