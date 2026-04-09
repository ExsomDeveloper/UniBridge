using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace UniBridge
{
    /// <summary>
    /// Simulation cloud save adapter. Uses PlayerPrefs as backend.
    /// Virtual key "UNIBRIDGESAVES_SIMULATED" — always available in Editor and Runtime.
    /// Use for testing cloud save behavior without a real platform SDK.
    /// </summary>
    [Preserve]
    public class SimulatedSaveSource : ISaveSource
    {
        private const string KeyPrefix = "unibridge_saves_sim_";

        public void Save(string key, string json, Action<bool> onComplete)
        {
            try
            {
                PlayerPrefs.SetString(KeyPrefix + key, json);
                PlayerPrefs.Save();
                Debug.Log($"[{nameof(SimulatedSaveSource)}]: Saved key '{key}'");
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SimulatedSaveSource)}]: Failed to save key '{key}': {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        public void Load(string key, Action<bool, string> onComplete)
        {
            try
            {
                string prefKey = KeyPrefix + key;
                if (!PlayerPrefs.HasKey(prefKey))
                {
                    onComplete?.Invoke(false, null);
                    return;
                }

                string json = PlayerPrefs.GetString(prefKey);
                Debug.Log($"[{nameof(SimulatedSaveSource)}]: Loaded key '{key}'");
                onComplete?.Invoke(true, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SimulatedSaveSource)}]: Failed to load key '{key}': {e.Message}");
                onComplete?.Invoke(false, null);
            }
        }

        public void Delete(string key, Action<bool> onComplete)
        {
            try
            {
                PlayerPrefs.DeleteKey(KeyPrefix + key);
                PlayerPrefs.Save();
                Debug.Log($"[{nameof(SimulatedSaveSource)}]: Deleted key '{key}'");
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SimulatedSaveSource)}]: Failed to delete key '{key}': {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        public void HasKey(string key, Action<bool> onComplete)
        {
            try
            {
                onComplete?.Invoke(PlayerPrefs.HasKey(KeyPrefix + key));
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SimulatedSaveSource)}]: Failed to check key '{key}': {e.Message}");
                onComplete?.Invoke(false);
            }
        }
    }
}
