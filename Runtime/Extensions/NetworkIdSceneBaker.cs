#if UNITY_EDITOR
using System.Linq;
using Soso.Net.Behaviors;
using Soso.Net.Logging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net.Extensions
{
    public static class NetworkIdSceneBaker
    {
        [MenuItem("SosoNet/Bake Scene IDs")]
        public static void BakeSceneIDs()
        {
            NetworkIdentity[] identities = Object.FindObjectsByType<NetworkIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            identities = identities
                .OrderBy(i => i.gameObject.GetFullPathName())
                .ToArray();
            
            uint totalbaked = 0;
            ulong currentSequence = 0;
            foreach (NetworkIdentity identity in identities)
            {
                // Skip prefabs
                if (identity.gameObject.scene.rootCount == 0) continue;

                if (identity.IsServerAuthority == false)
                {
                    NetworkLogger.Warn(CHANNEL.Default, "Identity {name} is not marked as ServerAuthority", identity.gameObject.name);
                    continue;
                }
                
                currentSequence++;
                totalbaked++;
                identity.BakeSceneId(currentSequence);
                NetworkLogger.Info(CHANNEL.Default, "Baked {name}'s identity as {id}", identity.gameObject.name, identity.InstanceId);
            }

            NetworkLogger.Info(CHANNEL.Default, "Successfully baked {total} identities", totalbaked);
            
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
#endif