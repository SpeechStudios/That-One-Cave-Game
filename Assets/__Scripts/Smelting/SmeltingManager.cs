using System.Collections.Generic;
using UnityEngine;

public class SmeltingManager : MonoBehaviour
{
    public static SmeltingManager Instance { get; private set; }

    public GameObject SmeltingCanvas;
    public List<ForgeUI> Forges;

    [HideInInspector] public Smelter TargetSmelter;

    public void Init()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        for (int i = 0; i < Forges.Count; i++)
        {
            Forges[i].SetupSlots(i);
        }
    } 
    public void Open(Smelter smelter)
    {
        TargetSmelter = smelter;
        TargetSmelter.OnSmeltingSlotsChanged += HandleSmeltingSlotsChanged;
        TargetSmelter.OnSmeltingValidated += CheckResetTimer;
        TargetSmelter.OnSmeltingComplete += SyncForgeTimers;
        SyncForgeItems();
        SmeltingCanvas.SetActive(true);
    }
    public void Close()
    {
        TargetSmelter.OnSmeltingSlotsChanged -= HandleSmeltingSlotsChanged;
        TargetSmelter.OnSmeltingValidated -= CheckResetTimer;
        TargetSmelter.OnSmeltingComplete -= SyncForgeTimers;
        TargetSmelter = null;
        SmeltingCanvas.SetActive(false);
    }
    private void Update()
    {
        if (SmeltingCanvas.activeInHierarchy)
            SyncForgeTimers();
    }
    public void SyncForgeTimers()
    {
        var ClientForges = TargetSmelter.ClientForges;
        for (int i = 0; i < ClientForges.Count; i++)
        {
            SmeltingForgeData data = ClientForges[i];
            ForgeUI ui = Forges[i];
            ui.UpdateFill(data);
        }
    }
    public void SyncForgeItems()
    {
        var ClientForges = TargetSmelter.ClientForges;
        for (int i = 0; i < ClientForges.Count; i++)
        {
            ForgeUI ui = Forges[i];
            ui.UpdateUI(TargetSmelter.ClientSlots, i);
        }
    }
    public void CheckResetTimer(bool isValid)
    {
        if (isValid) return;

        var ClientForges = TargetSmelter.ClientForges;
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
                DragGhostManager.Instance.UpdateDragGhost(patch.Data);
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
