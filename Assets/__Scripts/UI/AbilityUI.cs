using UnityEngine;

public class AbilityUI : MonoBehaviour
{
    public RectTransform CooldownFill;
    public GameObject IconDarkMask;

    private CooldownTimer cooldown;
    private float fullDuration;
    private bool active;

    public void BeginCooldown(CooldownTimer cooldown, float fullDuration)
    {
        this.cooldown = cooldown;
        this.fullDuration = fullDuration;
        active = true;
        CooldownFill.gameObject.SetActive(true);
        Refresh();
    }

    private void Update()
    {
        if (!active) return;
        Refresh();
        if (cooldown.IsReady)
        {
            active = false;
            CooldownFill.gameObject.SetActive(false);
        }
    }

    private void Refresh()
    {
        bool isReady = cooldown.IsReady;
        float ratio = isReady || fullDuration <= 0f ? 1f : 1f - Mathf.Clamp01(cooldown.SecondsRemaining / fullDuration);

        Vector3 scale = CooldownFill.localScale;
        scale.y = ratio;
        CooldownFill.localScale = scale;

        IconDarkMask.SetActive(!isReady);
    }
}