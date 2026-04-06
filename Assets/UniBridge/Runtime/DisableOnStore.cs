using UnityEngine;

namespace UniBridge
{
    public class DisableOnStore : MonoBehaviour
    {
        [SerializeField] private StoreTargetMask _disableOnStores = StoreTargetMask.None;

        private void Awake()
        {
            if (IsCurrentStoreSelected())
                gameObject.SetActive(false);
        }

        private bool IsCurrentStoreSelected()
        {
#if UNIBRIDGE_STORE_GOOGLEPLAY
            return (_disableOnStores & StoreTargetMask.GooglePlay) != 0;
#elif UNIBRIDGE_STORE_RUSTORE
            return (_disableOnStores & StoreTargetMask.RuStore) != 0;
#elif UNIBRIDGE_STORE_APPSTORE
            return (_disableOnStores & StoreTargetMask.AppStore) != 0;
#elif UNIBRIDGE_STORE_PLAYGAMA
            return (_disableOnStores & StoreTargetMask.Playgama) != 0;
#elif UNIBRIDGE_STORE_EDITOR
            return (_disableOnStores & StoreTargetMask.Editor) != 0;
#else
            return false;
#endif
        }
    }
}
