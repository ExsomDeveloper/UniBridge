using UnityEngine;

namespace UniBridge
{
    [CreateAssetMenu(
        fileName = nameof(UniBridgeRateConfig),
        menuName = "UniBridge/Rate Configuration")]
    public class UniBridgeRateConfig : ScriptableObject
    {
        [Header("Общие настройки")]
        public bool AutoInitialize = true;
        public string PreferredRateAdapter;
    }
}
