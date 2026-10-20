using System;
using System.Globalization;
using Soso.Net.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Soso.Net.Editor
{
    [CustomPropertyDrawer(typeof(RemoteInfo))]
    public class RemoteInfoPropertyDrawer : PropertyDrawer
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

            var idProp = property.FindPropertyRelative(nameof(RemoteInfo.SessionId));
            var remoteTimeProp = property.FindPropertyRelative(nameof(RemoteInfo.RemoteTime));
            var timeDiffProp = property.FindPropertyRelative(nameof(RemoteInfo.TimeDifference));
            var lastPingProp = property.FindPropertyRelative(nameof(RemoteInfo.LastPingTime));
            var pingProp = property.FindPropertyRelative(nameof(RemoteInfo.Ping));

            CreateField(foldout, idProp, "Session Id:", p => p.uintValue.ToString());
            CreateSeparator(foldout);
            CreateField(foldout, remoteTimeProp, "Remote Time:", p => p.doubleValue.ToString(CultureInfo.InvariantCulture));
            CreateSeparator(foldout);
            CreateField(foldout, timeDiffProp, "Time Difference:", p => p.doubleValue.ToString(CultureInfo.InvariantCulture));
            CreateSeparator(foldout);
            CreateField(foldout, lastPingProp, "Last Ping:", p => p.doubleValue.ToString(CultureInfo.InvariantCulture));
            CreateSeparator(foldout);
            CreateField(foldout, pingProp, "Ping:", p => p.doubleValue.ToString(CultureInfo.InvariantCulture));
            CreateSeparator(foldout);

            return root;
        }

        private void CreateField(VisualElement root, SerializedProperty prop, string label, Func<SerializedProperty, string> getText)
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
            
            var valueField = new Label(getText(prop));
            valueField.style.flexGrow = 1;
            valueField.style.marginRight = 4;
            valueField.style.unityTextAlign = TextAnchor.MiddleRight;
            valueField.SetEnabled(false);
            row.Add(valueField);
            
            root.TrackPropertyValue(prop, (p) => valueField.text = getText(p));
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