using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    public GameObject CraftingCanvas;
    public Button RecipePrefab;
    public Transform RecipePrefabParent;
    public List<CraftingSlot> Slots;
    public CraftingOutcomeSlot Outcome;

    [HideInInspector] public PlayerCraftingModule TargetCrafting;

    public void Init()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }
    public void Bind(PlayerCraftingModule targetCrafting)
    {
        TargetCrafting = targetCrafting;
        TargetCrafting.OnCraftingSlotsChanged += HandleCraftingChanged;
        TargetCrafting.OnRecipeReady += ready => Outcome.OnRecipeComplete(ready, new ItemSlotData 
        {
            ID = TargetCrafting.ClientRecipe.CraftedOutcome.ID,
            Quantity = TargetCrafting.ClientRecipe.CraftedOutcomeQuantity,
        });
        foreach (var recipe in Registry.Instance.CraftingRecipeList)
        {
            Button button = Instantiate(RecipePrefab, RecipePrefabParent);
            button.onClick.AddListener(() => SetupSlots(recipe.ID));
            button.onClick.AddListener(() => TargetCrafting.SelectRecipe(recipe.ID));
        }
    }

    public void Open()
    {
        CraftingCanvas.SetActive(true);
    }
    public void Close()
    {
        CraftingCanvas.SetActive(false);
        TargetCrafting.CloseCrafting();
    }

    private void SetupSlots(int recipeID)
    {
        var recipe = Registry.GetCraftingRecipe(recipeID);
        if (recipe == null) return;

        for (int i = 0; i < recipe.Components.Count; i++)
        {
            Slots[i].gameObject.SetActive(true);
            Slots[i].Setup(recipe.Components[i], i);
        }
    }

    private void HandleCraftingChanged(List<SlotPatch> patches)
    {
        foreach (var patch in patches)
        {
            if (patch.Type == SlotType.Ghost)
            {
                DragGhostManager.Instance.UpdateDragGhost(patch.Data);
            }
            if (patch.Type == SlotType.Crafting)
            {
                Slots[patch.Index].SlotData = patch.Data;
                Slots[patch.Index].UpdateUI();
            }
        }
    }
}
