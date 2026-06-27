using UnityEngine;
using UnityEditor;

/*
[CustomEditor(typeof(MeleeHitDetection))]
public class MeleeHitDetectionEditor : Editor
{
    private MeleeHitDetection HitDetection;

    private void OnEnable()
    {
        HitDetection = (MeleeHitDetection)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        HitDetection.ColliderType = (HitDetectionColliderType)EditorGUILayout.EnumPopup("Collider Type", HitDetection.ColliderType);
        EditorGUILayout.Space();

        if (HitDetection.ColliderType == HitDetectionColliderType.Sphere)
            DrawSphereColliderFields();
        if (HitDetection.ColliderType == HitDetectionColliderType.Capsule)
            DrawCapsuleColliderFields();

        EditorGUILayout.Space();
        HitDetection.ShowGizmos = EditorGUILayout.Toggle("Show Gizmos", HitDetection.ShowGizmos);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(HitDetection);
        }
    }
    private void DrawSphereColliderFields()
    {
        EditorGUILayout.LabelField("Sphere Collider Settings", EditorStyles.boldLabel);
        HitDetection.Point1 = (Transform)EditorGUILayout.ObjectField("Centre Point", HitDetection.Point1, typeof(Transform), true);
        HitDetection.Radius = EditorGUILayout.FloatField("Radius", HitDetection.Radius);
        HitDetection.HitLayer = EditorGUILayout.MaskField("Detection Layer", HitDetection.HitLayer, UnityEditorInternal.InternalEditorUtility.layers);
    }
    private void DrawCapsuleColliderFields()
    {
        EditorGUILayout.LabelField("Capsule Collider Settings", EditorStyles.boldLabel);
        HitDetection.Point1 = (Transform)EditorGUILayout.ObjectField("Top Point", HitDetection.Point1, typeof(Transform), true);
        HitDetection.Point2 = (Transform)EditorGUILayout.ObjectField("Bottom Point", HitDetection.Point2, typeof(Transform), true);
        HitDetection.Radius = EditorGUILayout.FloatField("Capusle Width", HitDetection.Radius);
        HitDetection.HitLayer = EditorGUILayout.MaskField("Detection Layer", HitDetection.HitLayer, UnityEditorInternal.InternalEditorUtility.layers);
    }
}
*/