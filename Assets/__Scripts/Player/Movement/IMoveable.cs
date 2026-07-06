using UnityEngine;

public interface IMoveable
{
    void ApplyKnockback(Vector3 velocity);
    void ApplySlow(float multiplier, float duration);
    void ApplyImmobilize(float duration);
}