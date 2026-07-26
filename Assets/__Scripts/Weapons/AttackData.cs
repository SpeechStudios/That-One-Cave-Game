using UnityEngine;

[System.Serializable]
public class AttackData 
{
    public Vector3 Start;
    public Vector3 Mid;
    public Vector3 End;
    public AnimationCurve EaseType;
}
[System.Serializable]
public class SwingData
{
    public AttackData AttackData;
    public AnimationClip Clip;
}
