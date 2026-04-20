using Enemies;
using RunFlow;
using UnityEditor;
using UnityEngine;

namespace RunFlow.Editor
{
    [CustomEditor(typeof(EncounterDef))]
    public class EncounterDefEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawProperty("id");
            DrawProperty("displayName");
            DrawProperty("encounterKind");
            DrawProperty("pathPrefab");
            DrawProperty("spawnBatches", includeChildren: true);
            DrawWaveSummary((EncounterDef)target);
            DrawProperty("rewardPool");
            DrawProperty("goldReward");
            DrawProperty("metaCurrencyReward");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProperty(string propertyName, bool includeChildren = false)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property, includeChildren);
        }

        private static void DrawWaveSummary(EncounterDef encounter)
        {
            if (encounter == null)
                return;

            if (encounter.spawnBatches == null || encounter.spawnBatches.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "This encounter has no spawn batches. Configure batches here to author the wave directly in the editor.",
                    MessageType.Info
                );
                return;
            }

            EditorGUILayout.HelpBox(
                $"Wave summary: {encounter.spawnBatches.Count} batches, {encounter.TotalSpawnCount} total enemies, ~{encounter.EstimatedDurationSeconds:0.##} seconds until all batches have spawned.",
                MessageType.None
            );

            for (int i = 0; i < encounter.spawnBatches.Count; i++)
            {
                SpawnBatch batch = encounter.spawnBatches[i];
                if (batch == null)
                {
                    EditorGUILayout.HelpBox($"Batch {i + 1} is null.", MessageType.Warning);
                    continue;
                }

                if (batch.enemyDef == null)
                {
                    EditorGUILayout.HelpBox($"Batch {i + 1} has no enemy definition assigned.", MessageType.Warning);
                }

                if (batch.spawnCount <= 0)
                {
                    EditorGUILayout.HelpBox($"Batch {i + 1} has a non-positive spawn count and will be clamped to 1.", MessageType.Warning);
                }

                if (batch.spawnInterval < 0f || batch.waitTime < 0f)
                {
                    EditorGUILayout.HelpBox($"Batch {i + 1} has a negative timing value and will be clamped to 0.", MessageType.Warning);
                }
            }
        }
    }
}
