using System;
using Soso.Net.Behaviors;
using Soso.Net.Behaviors.Rpc;
using UnityEngine;

public class ExampleReceiver : INetworkReceiver
{
    [SerializeField] private bool debugViewSyncable;

    public SosoSync<bool> SyncMe = new SosoSync<bool>();

    public override void OnSpawn()
    {
        SyncMe.OnChanged += OnSyncMeChanged;
    }

    public override void OnDespawn()
    {
        SyncMe.OnChanged -= OnSyncMeChanged;
    }

    private void OnValidate()
    {
        SyncMe.Value = debugViewSyncable;
        debugViewSyncable = SyncMe.Value;
    }

    private void OnSyncMeChanged(bool oldValue, bool newValue)
    {
        debugViewSyncable = newValue;
    }
}
