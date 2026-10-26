using System;
using Soso.Net.Behaviors;
using Soso.Net.Behaviors.Rpc;
using UnityEngine;
using UnityEngine.InputSystem;

public class ExampleReceiver : INetworkReceiver
{
    [SerializeField] public float MovementForce = 10;
    [SerializeField] public Rigidbody2D Rb;
    [SerializeField] public InputAction Movement;
    [SerializeField] private bool debugViewSyncable;

    public SosoSync<bool> SyncMe = new SosoSync<bool>();
    private Vector2 _input;

    public override void OnSpawn()
    {
        SyncMe.OnChanged += OnSyncMeChanged;
    }

    protected override void Initialize()
    {
        base.Initialize();

        if (NetId.IsOwner)
        {
            Movement.performed += OnMovementInput;
            Movement.canceled += OnMovementInput;
            Movement.Enable();
        }
    }

    public override void OnDespawn()
    {
        SyncMe.OnChanged -= OnSyncMeChanged;
        
        Movement.performed -= OnMovementInput;
        Movement.canceled -= OnMovementInput;
        Movement.Disable();
    }

    private void FixedUpdate()
    {
        if (_input != Vector2.zero)
        {
            Rb.AddForce(_input * MovementForce);
        }
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
    
    private void OnMovementInput(InputAction.CallbackContext obj)
    {
        _input = obj.ReadValue<Vector2>().normalized;
    }
}
