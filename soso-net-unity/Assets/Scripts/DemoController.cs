using System;
using DefaultNamespace;
using DefaultNamespace.Utils;
using Soso.Net;
using Soso.Net.Logging;
using Soso.Net.Transports.Default;
using Soso.Utils.Logging;
using UnityEngine;

public class DemoController : MonoBehaviour
{
    [SerializeField] public DefaultNetworkManager NetworkManager;
    [SerializeField] public DemoSpawner Spawner;

    private async void Start()
    {
        Application.runInBackground = true;
        NetworkManager.PlayerId = (ulong)Guid.NewGuid().ToString().GetHashCode();
        
        NetworkLogger.Logger = new SosoLogger<NetworkLogger.CHANNEL>(new UnityLogFormatter());
        NetworkLogger.Logger.ActiveChannels = NetworkLogger.CHANNEL.Default;
        NetworkLogger.Logger.Level = LOG_LEVEL.Info;
        
        await NetworkManager.Initialize();
    }

    private async Awaitable OnDestroy()
    {
        await Shutdown();
    }

    public void OnHost()
    {
        _ = Host();
    }

    public void OnJoin()
    {
        _ = Join();
    }


    public void OnShutdown()
    {
        _ = Shutdown();
    }

    public void OnSpawn()
    {
        Spawn();
    }

    public async Awaitable Host()
    {
        Debug.Log("Hosting...");
        bool result = false;
        try
        {
            result = await NetworkManager.CreateSocketServer(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        Debug.Log("Host result" + result);
    }

    public async Awaitable Join()
    {
        Debug.Log("Joining...");
        bool result = false;
        try
        {
            result = await NetworkManager.JoinSocketServer(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        Debug.Log("Join result " + result);
    }

    private async Awaitable Shutdown()
    {
        Debug.Log("Shutting down...");
        await NetworkManager.ShutDown();
        Debug.Log("Shut down");
    }

    public void Spawn()
    {
        Spawner.Spawn(gameObject.scene, 0, Vector3.zero, Quaternion.identity);
    }
}
