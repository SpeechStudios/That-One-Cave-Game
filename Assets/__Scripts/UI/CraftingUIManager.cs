using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CraftingUIManager : MonoBehaviour
{
    public GameObject CraftingCanvas;
    public List<CraftingSlot> Grid;
    public CraftingOutcomeSlot Outcome;

    [HideInInspector] public PlayerCraftingModule TargetCrafting;
    private DragGhostUIManager UI_DragGhost;
    private void Start()
    {
        UI_DragGhost = PlayerUIManager.Instance.UI_DragGhost;
    }
    public void Init()
    {

    }
    public void Bind(PlayerCraftingModule targetCrafting)
    {
        TargetCrafting = targetCrafting;
        TargetCrafting.OnCraftingSlotsChanged += HandleCraftingChanged;
        TargetCrafting.OnRecipeReady += ready => Outcome.OnRecipeComplete(ready, TargetCrafting);
    }
    private void HandleCraftingChanged(List<SlotPatch> patches)
    {
        foreach (var patch in patches)
        {
            if (patch.Type == SlotType.Ghost)
            {
               UI_DragGhost.UpdateDragGhost(patch.Data);
            }
            if (patch.Type == SlotType.Crafting)
            {
                Grid[patch.Index].SlotData = patch.Data;
                Grid[patch.Index].UpdateUI();
            }
        }
    }
}
