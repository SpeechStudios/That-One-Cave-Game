using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingUIManager : MonoBehaviour
{
    public GameObject CraftingCanvas;
    public RecipeButton RecipePrefab;
    public Transform RecipePrefabParent;
    public List<CraftingSlot> Slots;
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
        TargetCrafting.OnRecipeReady += ready => Outcome.OnRecipeComplete(ready, new ItemSlotData 
        {
            ID = TargetCrafting.ClientRecipe.CraftedOutcome.ID,
            Quantity = TargetCrafting.ClientRecipe.CraftedOutcomeQuantity,
        });
        foreach (var recipe in Registry.Instance.CraftingRecipeList)
        {
            RecipeButton rb = Instantiate(RecipePrefab, RecipePrefabParent);
            rb.Text.text = Registry.GetCraftingRecipe(recipe.ID).ItemName;
            rb.Button.onClick.AddListener(() => SetupSlots(recipe.ID));
            rb.Button.onClick.AddListener(() => TargetCrafting.SelectRecipe(recipe.ID));
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
               UI_DragGhost.UpdateDragGhost(patch.Data);
            }
            if (patch.Type == SlotType.Crafting)
            {
                Slots[patch.Index].SlotData = patch.Data;
                Slots[patch.Index].UpdateUI();
            }
        }
    }
}
