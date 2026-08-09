using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CraftingRecipe))]
public class CraftingRecipeEditor : Editor
{
    private SerializedProperty idProp;
    private SerializedProperty patternProp;
    private SerializedProperty craftedOutcomeProp;
    private SerializedProperty craftedOutcomeQuantityProp;

    private bool isFirst;

    private void OnEnable()
    {
        idProp = serializedObject.FindProperty("ID");
        patternProp = serializedObject.FindProperty("Pattern");
        craftedOutcomeProp = serializedObject.FindProperty("CraftedOutcome");
        craftedOutcomeQuantityProp = serializedObject.FindProperty("CraftedOutcomeQuantity");

        isFirst = true;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (isFirst)
        {
            InitializeDefaults();
            isFirst = false;
        }

        EditorGUILayout.PropertyField(idProp);
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Crafting Pattern", EditorStyles.boldLabel);
        DrawPatternGrid();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Outcome", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(craftedOutcomeProp);
        EditorGUILayout.PropertyField(craftedOutcomeQuantityProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void InitializeDefaults()
    {
        CraftingRecipe recipe = (CraftingRecipe)target;
        bool untouched = true;

        for (int i = 0; i < recipe.Pattern.Length; i++)
        {
            if (recipe.Pattern[i].ResourceType != ResourceType.None || recipe.Pattern[i].MaterialGroup != 0)
            {
                untouched = false;
                break;
            }
        }

        if (!untouched) return;

        for (int i = 0; i < patternProp.arraySize; i++)
            patternProp.GetArrayElementAtIndex(i).FindPropertyRelative("MaterialGroup").intValue = -1;

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPatternGrid()
    {
        float cellWidth = 60f;

        for (int row = 0; row < 3; row++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            for (int col = 0; col < 3; col++)
            {
                int index = row * 3 + col;
                DrawCell(patternProp.GetArrayElementAtIndex(index), cellWidth);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }
    }

    private void DrawCell(SerializedProperty componentProp, float width)
    {
        SerializedProperty resourceTypeProp = componentProp.FindPropertyRelative("ResourceType");
        SerializedProperty materialGroupProp = componentProp.FindPropertyRelative("MaterialGroup");

        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(width));

        EditorGUILayout.LabelField("Type", EditorStyles.miniLabel, GUILayout.Width(width - 8));
        EditorGUILayout.PropertyField(resourceTypeProp, GUIContent.none, GUILayout.Width(width - 8));

        EditorGUILayout.LabelField("Group", EditorStyles.miniLabel, GUILayout.Width(width - 8));
        EditorGUILayout.PropertyField(materialGroupProp, GUIContent.none, GUILayout.Width(width - 8));

        EditorGUILayout.EndVertical();
        GUILayout.Space(4);
    }
}