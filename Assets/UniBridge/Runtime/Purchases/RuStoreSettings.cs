using System;
using UnityEngine;

namespace UniBridge
{
    [Serializable]
    public class RuStoreSettings
    {
        [SerializeField] private string _consoleApplicationId;
        [SerializeField] private string _deeplinkPrefix = "yourapp://rustore";

        public string ConsoleApplicationId => _consoleApplicationId;
        public string DeeplinkPrefix => _deeplinkPrefix;
    }
}
