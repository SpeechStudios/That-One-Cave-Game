using UnityEngine;

public enum BowEffectType { Fire, Poison, Wind }
public class BowEffect : MonoBehaviour
{
    public GameObject FireEffect;
    public GameObject PoisonEffect;
    public GameObject WindEffect;
    public void Toggle(BowEffectType type, bool toggle)
    {
        switch (type)
        {
            case BowEffectType.Fire:
                FireEffect.SetActive(toggle);
                break;
            case BowEffectType.Poison:
                PoisonEffect.SetActive(toggle);
                break;
            case BowEffectType.Wind:
                WindEffect.SetActive(toggle);
                break;
            default:
                break;
        }
    }
    public void Clear()
    {
        if (FireEffect != null) FireEffect.SetActive(false);
        if (PoisonEffect != null) PoisonEffect.SetActive(false);
        if (WindEffect != null) WindEffect.SetActive(false);
    }
}
