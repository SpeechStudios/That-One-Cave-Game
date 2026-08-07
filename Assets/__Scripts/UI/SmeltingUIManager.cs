using System.Collections.Generic;
using UnityEngine;

public class SmeltingUIManager : MonoBehaviour
{

    public GameObject SmeltingCanvas;
    public List<ForgeUI> Forges;

    [HideInInspector] public PlayerSmeltingModule TargetSmelting;
    private DragGhostUIManager UI_DragGhost;
    private void Start()
    {
        UI_DragGhost = PlayerUIManager.Instance.UI_DragGhost;
    }
    public void Init()
    {
        for (int i = 0; i < Forges.Count; i++)
        {
            Forges[i].SetupSlots(i);
        }
    }
    public void Bind(PlayerSmeltingModule targetSmelting)
    {
        TargetSmelting = targetSmelting;
        TargetSmelting.OnSmeltingSlotsChanged += HandleSmeltingSlotsChanged;
        TargetSmelting.OnSmeltingValidated += CheckResetTimer;
        TargetSmelting.OnSmeltingComplete += SyncForgeTimers;
    }

    private void Update()
    {
        if (SmeltingCanvas.activeInHierarchy)
            SyncForgeTimers();
    }
    public void SyncForgeTimers()
    {
        var ClientForges = TargetSmelting.ClientForges;
        for (int i = 0; i < ClientForges.Count; i++)
        {
            SmeltingForgeData data = ClientForges[i];
            ForgeUI ui = Forges[i];
            ui.UpdateFill(data);
        }
    }
    public void SyncForgeItems()
    {
        var ClientForges = TargetSmelting.ClientForges;
        for (int i = 0; i < ClientForges.Count; i++)
        {
            ForgeUI ui = Forges[i];
            ui.UpdateUI(TargetSmelting.ClientSlots, i);
        }
    }
    public void CheckResetTimer(bool isValid)
    {
        if (isValid) return;

        var ClientForges = TargetSmelting.ClientForges;
        for (int i = 0; i < ClientForges.Count; i++)
        {
            SmeltingForgeData data = ClientForges[i];
            ForgeUI ui = Forges[i];
            ui.UpdateFill(data);
        }
    }

    private void HandleSmeltingSlotsChanged(List<SlotPatch> patches)
    {
        foreach (var patch in patches)
        {
            if (patch.Type == SlotType.Ghost)
            {
                UI_DragGhost.UpdateDragGhost(patch.Data);
            }
            if (patch.Type == SlotType.Smelting)
            {
                var slot = GetForgeSlot(patch.Index);
                slot.SlotData = patch.Data;
                slot.UpdateUI();
            }
        }
    }
    private ItemSlot GetForgeSlot(int slotIndex)
    {
        int forgeIndex = 0;
        while (slotIndex >= 3)
        {
            forgeIndex++;
            slotIndex -= 3;
        }

        if (slotIndex == 0)
            return Forges[forgeIndex].Slot1;
        if (slotIndex == 1)
            return Forges[forgeIndex].Slot2;
        if (slotIndex == 2)
            return Forges[forgeIndex].OutcomeSlot;

        throw null;
    }
}
