using System.Collections.Generic;
using UnityEngine;

public class SmeltingUIManager : MonoBehaviour
{
    public GameObject SmeltingCanvas;
    public ForgeUI Forge;

    [HideInInspector] public PlayerSmeltingModule TargetSmelting;
    private DragGhostUIManager UI_DragGhost;
    private void Start()
    {
        UI_DragGhost = PlayerUIManager.Instance.UI_DragGhost;
    }
    public void Init()
    {
        Forge.SetupSlots();
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
        SmeltingForgeData data = TargetSmelting.ClientForge;
        Forge.UpdateFill(data);
    }
    public void SyncForgeItems()
    {
        SmeltingForgeData data = TargetSmelting.ClientForge;
        Forge.UpdateFill(data);
    }
    public void CheckResetTimer(bool isValid)
    {
        if (isValid) return;

        SmeltingForgeData data = TargetSmelting.ClientForge;
        Forge.UpdateFill(data);
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
        if (slotIndex == 0)
            return Forge.Slot1;
        if (slotIndex == 1)
            return Forge.Slot2;
        if (slotIndex == 2)
            return Forge.OutcomeSlot;

        throw null;
    }
}
