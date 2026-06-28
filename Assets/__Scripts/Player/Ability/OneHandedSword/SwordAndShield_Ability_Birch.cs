using UnityEngine;

public class SwordAndShield_Ability_Birch : Ability
{
    public override float Cooldown => 1.5f;

    protected override void OnActivate(GameObject user)
    {
        // just the slash logic, cooldown handled by base
    }

    public override void PlayEffect(GameObject user)
    {
        // slash VFX
    }
}
