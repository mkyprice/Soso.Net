#if UNITY_EDITOR

using Soso.Net.Objects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Soso.Net.Editor
{
    [CustomEditor(typeof(SpawnList), true)]
    public class BaseSpawnListEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            
            // Serialize elements
            SerializedProperty spawnablesProp = serializedObject.FindProperty(nameof(SpawnList.Spawnables));
            PropertyField animationsField = new PropertyField(spawnablesProp);
            root.Add(animationsField);
            
            return root;
        }
    }
}

#endif