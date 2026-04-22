using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EnemySpawnerDatabase.EnemyEntry))]
public class EnemyEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. Get the tier property to decide the color
        SerializedProperty tierProp = property.FindPropertyRelative("tier");
        EnemySpawnerDatabase.EnemyTier tier = (EnemySpawnerDatabase.EnemyTier)tierProp.enumValueIndex;

        // 2. Define colors based on the Enum
        Color rowColor = Color.white;
        switch (tier)
        {
            case EnemySpawnerDatabase.EnemyTier.Fodder:   rowColor = new Color(0.7f, 1f, 0.7f, 0.15f); break; // Soft Green
            case EnemySpawnerDatabase.EnemyTier.Elite:    rowColor = new Color(0.7f, 0.8f, 1f, 0.15f); break; // Soft Blue
            case EnemySpawnerDatabase.EnemyTier.MiniBoss: rowColor = new Color(1f, 0.9f, 0.5f, 0.15f); break; // Soft Orange
            case EnemySpawnerDatabase.EnemyTier.Boss:     rowColor = new Color(1f, 0.5f, 0.5f, 0.25f); break; // Soft Red
        }

        // 3. Draw a background rectangle for the whole row
        EditorGUI.DrawRect(position, rowColor);

        // 4. Draw the default properties on top of the color
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Ensure the background color covers the entire height of the expanded entry
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}