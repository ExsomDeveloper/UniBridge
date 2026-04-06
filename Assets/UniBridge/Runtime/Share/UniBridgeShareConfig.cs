using UnityEngine;

namespace UniBridge
{
    [CreateAssetMenu(
        fileName = nameof(UniBridgeShareConfig),
        menuName = "UniBridge/Share Configuration")]
    public class UniBridgeShareConfig : ScriptableObject
    {
        [Header("Общие настройки")]
        public bool AutoInitialize = true;
        public string PreferredShareAdapter;
    }
}
