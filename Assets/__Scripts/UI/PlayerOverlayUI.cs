using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class PlayerOverlayUI : MonoBehaviour
{
    public RectTransform HealthBar;
    public List<RectTransform> Items;
    public Image WeaponIcon, PickaxeIcon, AxeIcon;
    public GameObject WeaponAbilities;
    public AbilityUI Ability1;
    public AbilityUI Ability2;

    public float Radius = 400f;
    public float ArcRotationOffset = 90f;

    public float ItemAngularSize = 20f;
    public float SpacingAngle = 10f;

    public float SelectedScale = 1.5f;
    public float DefaultScale = 1f;

    private readonly float LerpSpeed = 10f;
    private int SelectedIndex = -1;
    private bool Lerping = false;

    private void OnEnable()
    {
        LayoutImmediate();
    }

    private void Update()
    {
        if (!Lerping) return;

        ComputeTargets(out var positions, out var scales);
        float t = 1f - Mathf.Exp(-LerpSpeed * Time.deltaTime);

        bool stillMoving = false;
        for (int i = 0; i < Items.Count; i++)
        {
            var rt = Items[i];
            if (rt == null) continue;

            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, positions[i], t);
            rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one * scales[i], t);

            if (Vector2.Distance(rt.anchoredPosition, positions[i]) > 0.1f ||
                Mathf.Abs(rt.localScale.x - scales[i]) > 0.001f)
            {
                stillMoving = true;
            }
        }

        if (!stillMoving) Lerping = false;
    }

    public void SelectItem(int index)
    {
        SelectedIndex = Items.Count - 1 - index;
        if (index == 0)
            ShowWeapon(true);
        else
            ShowWeapon(false);
        Lerping = true;
    }
    public void LayoutImmediate()
    {
        ComputeTargets(out var positions, out var scales);
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i] == null) continue;
            Items[i].anchoredPosition = positions[i];
            Items[i].localScale = Vector3.one * scales[i];
        }
    }
    private void ComputeTargets(out Vector2[] positions, out float[] scales)
    {
        int count = Items.Count;
        positions = new Vector2[count];
        scales = new float[count];
        if (count == 0) return;

        float[] scaleFactors = new float[count];
        for (int i = 0; i < count; i++)
            scaleFactors[i] = (i == SelectedIndex) ? SelectedScale : DefaultScale;

        float[] slotWidths = new float[count];
        float totalWidth = 0f;
        for (int i = 0; i < count; i++)
        {
            slotWidths[i] = ItemAngularSize * scaleFactors[i];
            totalWidth += slotWidths[i];
        }
        totalWidth += SpacingAngle * (count - 1);

        float cursor = -totalWidth * 0.5f;
        float startAngle = ArcRotationOffset;

        for (int i = 0; i < count; i++)
        {
            float centerOffset = cursor + slotWidths[i] * 0.5f;
            cursor += slotWidths[i] + SpacingAngle;

            float angleRad = (startAngle + centerOffset) * Mathf.Deg2Rad;
            positions[i] = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * Radius;
            scales[i] = scaleFactors[i];
        }
    }
    public void UpdateHealth(float health, float maxHealth)
    {
        float ratio = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;
        Vector3 scale = HealthBar.localScale;
        scale.x = ratio;
        HealthBar.localScale = scale;
    }
    public void ShowWeapon(bool show)
    {
        if (show)
            WeaponAbilities.SetActive(true);
        else
            WeaponAbilities.SetActive(false);
    }
    public void TriggerCooldown(bool isPrimary, CooldownTimer cooldown, float fullDuration)
    {
        var target = isPrimary ? Ability1 : Ability2;
        target.BeginCooldown(cooldown, fullDuration);
    }

}