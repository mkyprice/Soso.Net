using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Soso.Net.Editor
{
    [CustomPropertyDrawer(typeof(NetworkInstanceId))]
    public class NetworkInstanceIdPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var foldout = new Foldout()
            {
                text = property.displayName,
                value = false,
            };
            root.Add(foldout);
            
            SerializedProperty idProp = property.FindPropertyRelative(nameof(NetworkInstanceId.Id));

            var idField = CreateField(foldout, "Id:");
            CreateSeparator(foldout);
            var sceneField = CreateField(foldout, "Scene Id:");
            CreateSeparator(foldout);
            var sessionField = CreateField(foldout, "Session Id:");
            CreateSeparator(foldout);
            var sequenceField = CreateField(foldout, "Sequence:");
            CreateSeparator(foldout);
            
            // Track id
            root.TrackPropertyValue(idProp, (prop) =>
            {
                UpdateFields(prop, idField, sceneField, sessionField, sequenceField);
            });
            UpdateFields(idProp, idField, sceneField, sessionField, sequenceField);

            return root;
        }

        private Label CreateField(VisualElement root, string label)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2;
            row.style.alignItems = Align.Center;
            root.Add(row);
            
            var labelField = new Label(label);
            labelField.style.width = 128;
            labelField.style.unityTextAlign = TextAnchor.MiddleLeft;
            labelField.SetEnabled(false);
            row.Add(labelField);
            
            var valueField = new Label();
            valueField.style.flexGrow = 1;
            valueField.style.marginRight = 4;
            valueField.style.unityTextAlign = TextAnchor.MiddleRight;
            valueField.SetEnabled(false);
            row.Add(valueField);

            return valueField;
        }

        private void UpdateFields(SerializedProperty idProp, Label idLabel, Label sceneLabel, Label sessionLabel, Label sequenceLabel)
        {
            NetworkInstanceId id = (NetworkInstanceId)idProp.ulongValue;

            // Assign to fields
            idLabel.text = id.Id.ToString();
            sceneLabel.text = id.SceneId.ToString();
            sessionLabel.text = id.SessionId.ToString();
            sequenceLabel.text = id.SequenceNumber.ToString();
        }

        private VisualElement CreateSeparator(VisualElement root)
        {
            VisualElement separator = new VisualElement();
            separator.style.height = 1f;
            separator.style.backgroundColor = Color.gray4;
            separator.style.marginTop = 0;
            separator.style.marginBottom = 2;
            separator.style.marginRight = 4;
            separator.style.alignSelf = Align.Center;
            separator.style.width = Length.Percent(100);
            root.Add(separator);
            return separator;
        }
    }
}