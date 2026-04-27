using Combat;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CombatSessionDriver))]
public class CombatSessionDriverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty defaultSetup = serializedObject.FindProperty("defaultSetup");
        if (defaultSetup != null)
            EditorGUILayout.PropertyField(defaultSetup, new GUIContent("Configured Defaults"), true);

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        DrawResolvedSession((CombatSessionDriver)target);
    }

    private static void DrawResolvedSession(CombatSessionDriver driver)
    {
        EditorGUILayout.LabelField("Runtime Resolved", EditorStyles.boldLabel);

        CombatSessionSetup setup = driver != null ? driver.ResolvedSetup : null;
        if (setup == null)
        {
            EditorGUILayout.HelpBox("No session data is available.", MessageType.Info);
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Starting Mana", setup.StartingMana);
            EditorGUILayout.IntField("Max Mana", setup.MaxMana);
            EditorGUILayout.FloatField("Mana Regen / Second", setup.ManaRegenPerSecond);
            EditorGUILayout.IntField("Current Health", driver.PlayerState != null ? driver.PlayerState.CurrentHealth : setup.CurrentHealth);
            EditorGUILayout.IntField("Max Health", driver.PlayerState != null ? driver.PlayerState.MaxHealth : setup.MaxHealth);
            EditorGUILayout.IntField("Opening Hand Size", setup.OpeningHandSize);
            EditorGUILayout.IntField("Max Hand Size", setup.MaxHandSize);
            EditorGUILayout.IntField("Manual Draw Cost", setup.ManualDrawCost);
        }
    }
}
