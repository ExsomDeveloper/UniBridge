using System;
using System.Collections;
using UnityEngine;

namespace UniBridge
{
    /// <summary>
    /// Service MonoBehaviour for running coroutines from static classes.
    /// Created lazily on first access.
    /// </summary>
    internal class ShareCoroutineRunner : MonoBehaviour
    {
        private static ShareCoroutineRunner _instance;

        private static ShareCoroutineRunner Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var go = new GameObject("[UniBridgeShare] CoroutineRunner");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<ShareCoroutineRunner>();
                return _instance;
            }
        }

        public static void Run(IEnumerator routine) => Instance.StartCoroutine(routine);

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
