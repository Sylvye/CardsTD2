using Towers;
using UnityEditor;
using UnityEngine;

namespace Towers.Editor
{
    [CustomEditor(typeof(TowerDef))]
    public class TowerDefEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                using (new EditorGUI.DisabledScope(property.name == "effectRadius"))
                {
                    EditorGUILayout.PropertyField(property, true);
                }

                if (property.name == "effectRadius")
                {
                    EditorGUILayout.HelpBox(
                        "Tower effect radius is derived from Base Stats > Range.",
                        MessageType.Info
                    );
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
