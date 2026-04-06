using System.Collections.Generic;
using UnityEngine;

namespace UniBridge
{
    [CreateAssetMenu(fileName = nameof(UniBridgePurchasesConfig), menuName = "UniBridge/Purchases Configuration")]
    public class UniBridgePurchasesConfig : ScriptableObject
    {
        [Header("General")]
        public bool AutoInitialize = true;

        [Header("Adapter")]
        public string PreferredPurchaseAdapter;

        [Header("Product Catalog")]
        [SerializeField] private List<ProductDefinition> _products = new List<ProductDefinition>();
        public IReadOnlyList<ProductDefinition> Products => _products;

        public ProductDefinition FindByProjectId(string projectId)
        {
            foreach (var def in _products)
                if (def.ProjectId == projectId) return def;
            return null;
        }

        public ProductDefinition FindByStoreProductId(string storeId)
        {
            foreach (var def in _products)
                if (def.ProductId == storeId) return def;
            return null;
        }

        [Header("Unity IAP (Google Play / App Store)")]
        [SerializeField] private UnityIAPSettings _unityIAPSettings = new UnityIAPSettings();
        public UnityIAPSettings UnityIAPSettings => _unityIAPSettings;

        [Header("RuStore Billing (Android / RuStore)")]
        [SerializeField] private RuStoreSettings _ruStoreSettings = new RuStoreSettings();
        public RuStoreSettings RuStoreSettings => _ruStoreSettings;
    }
}
