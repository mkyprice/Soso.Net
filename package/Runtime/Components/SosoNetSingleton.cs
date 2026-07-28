using Soso.Net.Logging;
using UnityEngine;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net.Components
{
    public abstract class SosoNetSingleton<T> : MonoBehaviour
        where T : SosoNetSingleton<T>
    {
        public enum BEHAVIOR
        {
            DestroyNewest,
            DestroyOldest
        }

        [Header("Singleton")] [SerializeField] public new bool DontDestroyOnLoad;
        [SerializeField] public bool InitializeOnAwake;
        [SerializeField] public bool ShutdownOnDestroy;
        [SerializeField] public BEHAVIOR Behavior = BEHAVIOR.DestroyNewest;

        public static T GetInstance()
        {
            return _instance;
        }

        public static bool TryGetInstance(out T instance)
        {
            instance = _instance;
            return instance != null;
        }

        private static T _instance = null;
        private bool _isInitialized = false;

        private async void Awake()
        {
            if (isActiveAndEnabled == false)
            {
                NetworkLogger.Debug(CHANNEL.Default, "{name} is disabled. Destroying...", name);
                Destroy(this);
                return;
            }

            if (_instance != null)
            {
                if (Behavior == BEHAVIOR.DestroyOldest)
                {
                    Destroy(_instance);
                }
                else
                {
                    Destroy(this);
                    return;
                }
            }

            if (_instance != this)
            {
                _instance = this as T;
                if (DontDestroyOnLoad)
                {
                    DontDestroyOnLoad(_instance);
                }
            }
            
            if (InitializeOnAwake)
            {
                await InitializeAsync();
            }
        }

        private async void OnDestroy()
        {
            if (ShutdownOnDestroy)
            {
                await ShutDown();
            }
        }

        public async Awaitable Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            NetworkLogger.Debug(CHANNEL.Default, "Initializing {typeof(T).Name}", typeof(T).Name);
            await InitializeAsync();
            NetworkLogger.Debug(CHANNEL.Default, "Done initializing {typeof(T).Name}", typeof(T).Name);

            _isInitialized = true;
        }

        public async Awaitable ShutDown()
        {
            NetworkLogger.Debug(CHANNEL.Default, "Shutting down {typeof(T).Name}", typeof(T).Name);
            await ShutdownAsync();
            NetworkLogger.Debug(CHANNEL.Default, "Done shutting down {typeof(T).Name}", typeof(T).Name);
        }

        protected virtual Awaitable InitializeAsync()
        {
            _isInitialized = true;
            return Awaitable.NextFrameAsync();
        }

        protected virtual Awaitable ShutdownAsync()
        {
            return Awaitable.NextFrameAsync();
        }

        protected bool IsInitialized()
        {
            return _isInitialized;
        }
    }
}