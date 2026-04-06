using UnityEngine;

namespace UniBridge
{
    [CreateAssetMenu(fileName = nameof(UniBridgeSavesConfig), menuName = "UniBridge/Saves Configuration")]
    public class UniBridgeSavesConfig : ScriptableObject
    {
        public bool AutoInitialize = true;

        /// <summary>
        /// SDK define string: "UNIBRIDGESAVES_GPGS" | "UNITY_IOS_ICLOUD" | "UNIBRIDGESAVES_SIMULATED" | "UNIBRIDGE_PLAYGAMA" | "UNIBRIDGE_NONE" | "" (LocalSaveSource)
        /// Written by Build Manager when "Select" is clicked.
        /// </summary>
        public string PreferredSavesAdapter;
    }
}
