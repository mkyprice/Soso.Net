using System;
using System.Reflection;
using Soso.Net.Logging;

namespace Soso.Net.Behaviors.Rpc
{
    internal interface ISosoSync
    {
        public bool IsDirty();
        public void Initialize(RpcTarget target, int id);
        public void SetInternal(object value);
    }

    public class SosoSync<T> : ISosoSync
        where T : unmanaged
    {
        public Action<T, T> OnChanged;
        public bool SyncTime;
        public T Value
        {
            get => _value;
            set => Set(value);
        }

        public int Id => _id;
        
        private RpcTarget _target;
        private T _value;
        private T _dirtyValue;
        private int _id;
        private bool _isDirty = false;

        /// <summary>
        /// Sync the given value
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <param name="onChanged">Prev, New</param>
        /// <param name="syncTime"></param>
        public SosoSync(bool syncTime = false)
        {
            SyncTime = syncTime;
        }

        public void Set(T value)
        {
            if (value.Equals(_dirtyValue)) return;
            _isDirty = true;
            _dirtyValue = value;
            SendValue();
        }

        public void SetWithoutNotify(T value)
        {
            var oldValue = _value;
            var newValue = value;
            _value = newValue;
            _dirtyValue = _value;
            NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "{name}:{id} - SetWithoutNotify from {old} to {new}", nameof(SosoSync<T>), Id, oldValue, newValue);
        }

        public void SetInternal(object value)
        {
            var oldValue = _value;
            var newValue = (T)value;
            if (oldValue.Equals(newValue)) return;
            
            _value = newValue;
            _dirtyValue = _value;
            NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "{name}:{id} - Set from {old} to {new}", nameof(SosoSync<T>), Id, oldValue, newValue);
            OnChanged?.Invoke(oldValue, newValue);
        }

        public bool IsDirty()
        {
            return _isDirty;
        }

        private void SendValue()
        {
            _target.SetSync(this, _dirtyValue);
            _isDirty = false;
        }

        public void Initialize(RpcTarget target, int id)
        {
            _target = target;
            _id = id;
            if (IsDirty())
            {
                SendValue();
            }
        }
        
        public static implicit operator T (SosoSync<T> sync) => sync.Value;
    }
}