using System;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Core.Instances;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;

namespace Soso.Net.Core.Sync
{
    internal interface ISosoSync
    {
        void Process(SyncCall sync);
        void Initialize(RpcManager manager);
        void Setup(string propName, INetworkProcessor target);
        int SyncId { get; }
        string Name { get; }
    }

    public class SosoSync<T> : ISosoSync
        where T : unmanaged
    {
        /// <summary>
        /// Old value, New value
        /// </summary>
        public Action<T, T> OnChanged;
        public readonly bool SyncTime;
        public T Value
        {
            get
            {
                return _value;
            }
            set => Set(value);
        }

        public T DirtyValue
        {
            get => _dirtyValue;
            set => Set(value);
        }

        public int SyncId => _syncId;
        public string Name => _name;
        
        private RpcManager _manager;
        private T _value;
        private T _dirtyValue;
        private string _name;
        private int _syncId;
        private INetworkProcessor _target;
        private bool _isDirty = false;
        private long _lastSync;
        private bool _requireAuthority = false;

        /// <summary>
        /// Sync the given value
        /// </summary>
        /// <param name="syncTime"></param>
        /// <param name="requireAuthority"></param>
        public SosoSync(bool syncTime = false, bool requireAuthority = false)
        {
            SyncTime = syncTime;
            _requireAuthority = requireAuthority;
        }

        public void Set(T value)
        {
            if (_isDirty && Equals(_dirtyValue, value))
            {
                NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "{name} is already sending value {v}", _name, value);
                return;
            }
            
            if (_requireAuthority && (_target == null || _target.IsAuthority() == false))
            {
                NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "{name} is not authority", _name);
                _isDirty = false;
                _dirtyValue = _value;
                return;
            }
            _isDirty = true;
            _dirtyValue = value;
            SendValue();
        }

        public void Reset()
        {
            _value = default;
            _dirtyValue = _value;
            _lastSync = 0;
            _isDirty = false;
        }

        private bool SetInternal(T value, long time)
        {
            if (time <= _lastSync)
            {
                NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Counter was behind on {name}. Current: {c} given: {cn}", _name, _lastSync, time);
                return false;
            }

            var oldValue = _value;
            var newValue = value;
            _value = newValue;
            _dirtyValue = value;
            _isDirty = false;
            _lastSync = time;
            NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "{name}:{id} - Set from {old} to {new}", nameof(SosoSync<T>), Name, oldValue, newValue);
            OnChanged?.Invoke(oldValue, newValue);
            return true;
        }

        public bool IsDirty()
        {
            return _isDirty;
        }

        public void Process(SyncCall sync)
        {
            var source = sync.SourceInstance;
            var localTime = NetworkTime.ToLocalTime(source.SessionId, sync.Time);
            var value = (T)sync.Arg;
            if (sync.Destination == DESTINATION.Server)
            {
                if (_requireAuthority && _target.AuthorityId == source.SessionId)
                {
                    NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Source validation failed for {name}:{source}", _name, source);
                    return;
                }
                
                NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "Server sync {name} received value {v} from {source}", _name, value, source.SessionId);
                if (SetInternal(value, localTime))
                {
                    SyncCall call = new SyncCall()
                    {
                        Destination = DESTINATION.Client,
                        SyncTime = SyncTime,
                        Time = NetworkTime.Time,
                        TargetId = _target.GetLocalId(),
                        SyncId = SyncId,
                        Arg = value,
                    };
                    _manager.Send(call, 0);
                }
            }
            else
            {
                NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "Client sync {name} received value {v}", _name, value);
                SetInternal(value, localTime);
            }
        }
        
        private void SendValue()
        {
            if (_manager == null)
            {
                return;
            }

            var time = NetworkTime.Time;
            SyncCall call = new SyncCall()
            {
                Destination = DESTINATION.Server,
                SyncTime = SyncTime,
                Time = time,

                TargetId = _target.GetLocalId(),
                SyncId = SyncId,
                Arg = _dirtyValue,
            };
            _manager.Send(call, 0);
            _isDirty = false;
        }

        public void Setup(string propName, INetworkProcessor target)
        {
            _name = propName;
            _syncId = _name.GetHashCode();
            _target = target;
            _lastSync = 0;
        }

        public void Initialize(RpcManager manager)
        {
            _manager = manager;
            if (IsDirty())
            {
                SendValue();
            }
        }

        public static implicit operator T (SosoSync<T> sync) => sync.Value;
    }
}