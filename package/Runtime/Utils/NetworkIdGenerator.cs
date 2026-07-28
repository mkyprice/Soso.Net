using System.Collections.Generic;
using Soso.Net.Logging;

namespace Soso.Net.Utils
{
    public class NetworkIdGenerator
    {
        private readonly ushort _sessionId;
        private readonly Dictionary<ushort, ulong> _sequencePerScene = new Dictionary<ushort, ulong>();

        public NetworkIdGenerator(ushort sessionId)
        {
            _sessionId = sessionId;
        }

        public NetworkInstanceId GetNextId(ushort sceneId)
        {
            ulong sequence = IncrementSequence(sceneId);
            return new NetworkInstanceId(_sessionId, sceneId, sequence);
        }
        public ulong IncrementSequence(ushort sceneId)
        {
            if (_sequencePerScene.TryGetValue(sceneId, out ulong sequence) == false)
            {
                sequence = 0;
            }
            sequence++;
            _sequencePerScene[sceneId] = sequence;
            return sequence;
        }

        public void SetSequenceOffset(ushort sceneId, ulong offset)
        {
            if (_sequencePerScene.TryGetValue(sceneId, out ulong currentSequence))
            {
                NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Tried to set sequence offset for scene {id} but it was not 0", sceneId);
            }
            _sequencePerScene[sceneId] = offset;
        }
    }
}