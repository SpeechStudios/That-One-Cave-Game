using UnityEngine;


public abstract class Ability
{
    public abstract float Cooldown { get; }
    private float _lastUsedTime;

    public bool IsOnCooldown() => Time.time - _lastUsedTime < Cooldown;

    public void Activate(GameObject user)
    {
        if (IsOnCooldown()) return;
        _lastUsedTime = Time.time;
        OnActivate(user);
    }

    protected abstract void OnActivate(GameObject user);
    public abstract void PlayEffect(GameObject user);
}
