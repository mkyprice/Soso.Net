using Soso.Net.Objects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Soso.Net.Editor
{
    [CustomPropertyDrawer(typeof(NetworkInstance))]
    public class NetworkInstancePropertyDrawer : PropertyDrawer
    {

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var foldout = new Foldout()
            {
                text = property.displayName,
                value = false
            };
            root.Add(foldout);

            var idProp = property.FindPropertyRelative("_id");
            var remoteInfoProp = property.FindPropertyRelative("_remoteInfo");
            var identityRefProp = property.FindPropertyRelative(nameof(NetworkInstance.Identity));

            var idField = new PropertyField(idProp, "Id");
            foldout.Add(idField);

            var remoteInfoField = new PropertyField(remoteInfoProp, "Remote Info");
            foldout.Add(remoteInfoField);

            var identityRefField = new PropertyField(identityRefProp, "Identity");
            identityRefField.SetEnabled(false);
            foldout.Add(identityRefField);

            return root;
        }
    }
}