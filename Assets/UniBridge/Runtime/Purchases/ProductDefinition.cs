using System;
using UnityEngine;

namespace UniBridge
{
    [Serializable]
    public class ProductDefinition
    {
        [SerializeField] private string _projectId;
        [SerializeField] private string _productId;
        [SerializeField] private ProductType _type = ProductType.Consumable;
        [SerializeField] private string _playgamaProductId;
        [SerializeField] private int    _playgamaAmount = 1;

        /// <summary>
        /// Internal project ID used in all game code calls (IsPurchased, Purchase, events).
        /// </summary>
        public string ProjectId => _projectId;

        /// <summary>
        /// Store product ID sent to the SDK (Unity IAP / RuStore).
        /// Not used in game code — only inside adapters.
        /// </summary>
        public string ProductId => _productId;

        public ProductType Type => _type;

        /// <summary>
        /// Optional override for Playgama platform product ID.
        /// If empty, ProductId is used.
        /// </summary>
        public string PlaygamaProductId => string.IsNullOrEmpty(_playgamaProductId) ? _productId : _playgamaProductId;

        /// <summary>
        /// Price in Gam currency for Playgama platform (1 Gam = $0.10 USD).
        /// Written to playgama-bridge-config.json payments array via Settings window.
        /// </summary>
        public int PlaygamaAmount => _playgamaAmount;
    }
}
