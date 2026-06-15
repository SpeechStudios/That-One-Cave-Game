using System.Collections.Generic;
using UnityEngine;

public class ForgeUI : MonoBehaviour
{
    public SmeltingSlot Slot1;
    public SmeltingSlot Slot2;
    public SmeltingOutcomeSlot OutcomeSlot;
    public Transform SmeltingFill;

    public void SetupSlots(int forgeIndex)
    {
        Slot1.Setup(forgeIndex, 0);
        Slot2.Setup(forgeIndex, 1);
        OutcomeSlot.Setup(forgeIndex);
    }
    public void UpdateUI(List<SmeltingSlotData> slots, int forgeIndex)
    {
        Slot1.SlotData = slots[forgeIndex * 3 + 0].Data;
        Slot2.SlotData = slots[forgeIndex * 3 + 1].Data;
        OutcomeSlot.SlotData = slots[forgeIndex * 3 + 2].Data;

        Slot1.UpdateUI();
        Slot2.UpdateUI();
        OutcomeSlot.UpdateUI();
    }
    public void UpdateFill(SmeltingForgeData data)
    {
        if (data.CurrentRecipe == null)
        {
            if (SmeltingFill.transform.localScale.x != 0)
                UpdateSmeltingFill(0, 1);

            return;
        }
        UpdateSmeltingFill(data.SmeltingTimer, data.CurrentRecipe.SmeltingTime);
    }
    public void UpdateSmeltingFill(float smeltTimer, float smeltTime)
    {
        float progress = Mathf.Clamp01(smeltTimer / smeltTime);
        SmeltingFill.transform.localScale = new Vector3(progress, 1, 1);
    }
}
