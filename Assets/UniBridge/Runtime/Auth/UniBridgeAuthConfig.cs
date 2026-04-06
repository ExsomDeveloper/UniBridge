using UnityEngine;

namespace UniBridge
{
    [CreateAssetMenu(
        fileName = nameof(UniBridgeAuthConfig),
        menuName = "UniBridge/Auth Configuration")]
    public class UniBridgeAuthConfig : ScriptableObject
    {
        [Header("Общие настройки")]
        public bool AutoInitialize = true;
        public string PreferredAuthAdapter;
    }
}
