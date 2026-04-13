using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniBridge
{
    public static class UniBridgeAnalytics
    {
        public static bool   IsInitialized { get; private set; }
        public static string AdapterName   => _source?.GetType().Name ?? UniBridgeAdapterKeys.None;

        public static event Action OnInitSuccess;
        public static event Action OnInitFailed;

        private static IAnalyticsSource    _source;
        private static UniBridgeAnalyticsConfig  _config;

        // ── Auto-initialization ──────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            IsInitialized = false;
            _source       = null;

            if (_config == null)
                _config = LoadConfig();

            if (_config != null && _config.AutoInitialize)
                SetupAnalytics();
        }

        // ── Manual initialization ────────────────────────────────────────────

        public static void Initialize()
        {
            if (_config == null)
                _config = LoadConfig();

            if (_config == null)
            {
                Debug.LogError(
                    $"[{nameof(UniBridgeAnalytics)}]: UniBridgeAnalyticsConfig не найден! " +
                    "Создайте через Assets > Create > UniBridge > Analytics Configuration");
                return;
            }

            SetupAnalytics();
        }

        // ── Public API ───────────────────────────────────────────────────────

        public static void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (_source == null || !IsInitialized) return;
            _source.LogEvent(eventName, parameters);
        }

        public static void ReportPurchase(string productId, decimal price, string currency, string transactionId = null)
        {
            if (_source == null || !IsInitialized) return;
            _source.ReportPurchase(productId, price, currency, transactionId);
        }

        // ── Private methods ──────────────────────────────────────────────────

        private static void OnPurchaseSuccessHandler(PurchaseResult result)
        {
            if (!result.IsSuccess || _source == null || !IsInitialized) return;

            var products = UniBridgePurchases.Products;
            if (products == null) return;

            foreach (var p in products)
            {
                if (p.ProductId == result.ProductId)
                {
                    _source.ReportPurchase(p.ProductId, p.LocalizedPrice, p.IsoCurrencyCode, result.TransactionId);
                    return;
                }
            }
        }

        private static void SetupAnalytics()
        {
            if (IsInitialized)
                return;

            var builder = new AnalyticsSourceBuilder();
            _source = builder.Build(_config);

            if (_source == null)
            {
                Debug.Log($"[{nameof(UniBridgeAnalytics)}]: Analytics system disabled.");
                return;
            }

            UniBridgePurchases.OnPurchaseSuccess -= OnPurchaseSuccessHandler;
            UniBridgePurchases.OnPurchaseSuccess += OnPurchaseSuccessHandler;

            _source.Initialize(
                _config,
                onSuccess: () =>
                {
                    IsInitialized = true;
                    Debug.Log(
                        $"[{nameof(UniBridgeAnalytics)}]: " +
                        $"Инициализирован с {_source.GetType().Name}");
                    OnInitSuccess?.Invoke();
                },
                onFailed: () =>
                {
                    Debug.LogError(
                        $"[{nameof(UniBridgeAnalytics)}]: Ошибка инициализации");
                    OnInitFailed?.Invoke();
                });
        }

        private static UniBridgeAnalyticsConfig LoadConfig()
        {
            var config = Resources.Load<UniBridgeAnalyticsConfig>(nameof(UniBridgeAnalyticsConfig));

            if (config == null)
                Debug.LogWarning(
                    $"[{nameof(UniBridgeAnalytics)}]: " +
                    "UniBridgeAnalyticsConfig не найден в папке Resources.");

            return config;
        }
    }
}
