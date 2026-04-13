using UnityEngine;
using UniBridgeAdsFacade = global::UniBridge.UniBridge;

namespace UniBridge
{
    public enum UniBridgeSystemCheck { None, Share, Leaderboards, Purchases, Rate, Auth, Ads }

    public class DisableIfNotSupported : MonoBehaviour
    {
        [SerializeField] private UniBridgeSystemCheck _system = UniBridgeSystemCheck.Share;
        [SerializeField] private PlatformId[] _disableOnPlatforms = new PlatformId[0];

        private bool _subscribed;

        private void Awake()
        {
            // AdapterName == UniBridgeAdapterKeys.None means the facade's _source is null
            // (PreferredXxxAdapter == UNIBRIDGE_NONE or no adapter registered). In that case
            // OnInitSuccess/OnInitFailed will never fire — decide now.
            if (_system == UniBridgeSystemCheck.None || IsInitialized() || AdapterName() == UniBridgeAdapterKeys.None)
            {
                ApplyVisibility();
            }
            else
            {
                _subscribed = true;
                Subscribe(OnInitResult);
            }
        }

        private void OnDestroy()
        {
            if (!_subscribed) return;
            Unsubscribe(OnInitResult);
        }

        private void OnInitResult()
        {
            _subscribed = false;
            Unsubscribe(OnInitResult);
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            if (!IsSupported() || IsPlatformDisabled())
                gameObject.SetActive(false);
        }

        private bool IsPlatformDisabled()
        {
            if (_disableOnPlatforms == null || _disableOnPlatforms.Length == 0)
                return false;
            var currentId = UniBridgeEnvironment.GetPlatformId();
            if (string.IsNullOrEmpty(currentId))
                return false;
            for (int i = 0; i < _disableOnPlatforms.Length; i++)
                if (_disableOnPlatforms[i].ToStringId() == currentId)
                    return true;
            return false;
        }

        private bool IsInitialized() => _system switch
        {
            UniBridgeSystemCheck.Share        => UniBridgeShare.IsInitialized,
            UniBridgeSystemCheck.Leaderboards => UniBridgeLeaderboards.IsInitialized,
            UniBridgeSystemCheck.Purchases    => UniBridgePurchases.IsInitialized,
            UniBridgeSystemCheck.Rate         => UniBridgeRate.IsInitialized,
            UniBridgeSystemCheck.Auth         => UniBridgeAuth.IsInitialized,
            UniBridgeSystemCheck.Ads          => UniBridgeAdsFacade.IsInitialized,
            _                           => false
        };

        private string AdapterName() => _system switch
        {
            UniBridgeSystemCheck.Share        => UniBridgeShare.AdapterName,
            UniBridgeSystemCheck.Leaderboards => UniBridgeLeaderboards.AdapterName,
            UniBridgeSystemCheck.Purchases    => UniBridgePurchases.AdapterName,
            UniBridgeSystemCheck.Rate         => UniBridgeRate.AdapterName,
            UniBridgeSystemCheck.Auth         => UniBridgeAuth.AdapterName,
            UniBridgeSystemCheck.Ads          => UniBridgeAdsFacade.AdapterName,
            _                           => UniBridgeAdapterKeys.None
        };

        private bool IsSupported() => _system switch
        {
            UniBridgeSystemCheck.None         => true,
            UniBridgeSystemCheck.Share        => UniBridgeShare.IsSupported,
            UniBridgeSystemCheck.Leaderboards => UniBridgeLeaderboards.IsSupported,
            UniBridgeSystemCheck.Purchases    => UniBridgePurchases.IsSupported,
            UniBridgeSystemCheck.Rate         => UniBridgeRate.IsSupported,
            UniBridgeSystemCheck.Auth         => UniBridgeAuth.IsSupported,
            UniBridgeSystemCheck.Ads          => UniBridgeAdsFacade.IsSupported,
            _                           => false
        };

        private void Subscribe(System.Action handler)
        {
            switch (_system)
            {
                case UniBridgeSystemCheck.Share:
                    UniBridgeShare.OnInitSuccess += handler;
                    UniBridgeShare.OnInitFailed  += handler;
                    break;
                case UniBridgeSystemCheck.Leaderboards:
                    UniBridgeLeaderboards.OnInitSuccess += handler;
                    UniBridgeLeaderboards.OnInitFailed  += handler;
                    break;
                case UniBridgeSystemCheck.Purchases:
                    UniBridgePurchases.OnInitSuccess += handler;
                    UniBridgePurchases.OnInitFailed  += handler;
                    break;
                case UniBridgeSystemCheck.Rate:
                    UniBridgeRate.OnInitSuccess += handler;
                    UniBridgeRate.OnInitFailed  += handler;
                    break;
                case UniBridgeSystemCheck.Auth:
                    UniBridgeAuth.OnInitSuccess += handler;
                    UniBridgeAuth.OnInitFailed  += handler;
                    break;
                case UniBridgeSystemCheck.Ads:
                    UniBridgeAdsFacade.OnInitSuccess += handler;
                    UniBridgeAdsFacade.OnInitFailed  += handler;
                    break;
            }
        }

        private void Unsubscribe(System.Action handler)
        {
            switch (_system)
            {
                case UniBridgeSystemCheck.Share:
                    UniBridgeShare.OnInitSuccess -= handler;
                    UniBridgeShare.OnInitFailed  -= handler;
                    break;
                case UniBridgeSystemCheck.Leaderboards:
                    UniBridgeLeaderboards.OnInitSuccess -= handler;
                    UniBridgeLeaderboards.OnInitFailed  -= handler;
                    break;
                case UniBridgeSystemCheck.Purchases:
                    UniBridgePurchases.OnInitSuccess -= handler;
                    UniBridgePurchases.OnInitFailed  -= handler;
                    break;
                case UniBridgeSystemCheck.Rate:
                    UniBridgeRate.OnInitSuccess -= handler;
                    UniBridgeRate.OnInitFailed  -= handler;
                    break;
                case UniBridgeSystemCheck.Auth:
                    UniBridgeAuth.OnInitSuccess -= handler;
                    UniBridgeAuth.OnInitFailed  -= handler;
                    break;
                case UniBridgeSystemCheck.Ads:
                    UniBridgeAdsFacade.OnInitSuccess -= handler;
                    UniBridgeAdsFacade.OnInitFailed  -= handler;
                    break;
            }
        }
    }
}
