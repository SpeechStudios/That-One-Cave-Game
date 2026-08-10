using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CraftingOutcomeSlot : ItemSlot, IPointerDownHandler
{
    private bool Incrementing;
    private float CurrentSpeed = 0f;
    private float Accumulator;

    private readonly float Acceleration = 10f;
    private readonly float MaxSpeed = 10f;
    public void OnRecipeComplete(bool isReady, PlayerCraftingModule targetCrafting)
    {
        if (isReady)
        {
            SlotData.ID = targetCrafting.ClientRecipe.CraftedOutcome.ID;
            SlotData.Quantity = targetCrafting.ClientRecipe.CraftedOutcomeQuantity;
            SlotData.Materials = targetCrafting.Materials.OrderBy(kvp => kvp.Key).Select(kvp => (int)kvp.Value).ToArray();
            UpdateUI();
        }
        else
        {
            SlotData.Clear();
            UpdateUI();
        }
    }
    public override void UpdateUI(int quantity = -1)
    {
        base.UpdateUI();
        //Show Crafted Item Stats
    }
    public override void OnPointerDown(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
        {
            if (SlotData.HasItem())
            {
                if (PlayerUI.UI_Crafting.TargetCrafting.CraftItem())
                {
                    UpdateUI();
                    PlayerUI.UI_DragGhost.UpdateUI();
                }
            }
        }
        if (e.button == PointerEventData.InputButton.Right)
        {
            if (Keyboard.current.shiftKey.isPressed)
            {
                ShiftRightMouseClicked();
            }
            else
            {
                if (SlotData.Quantity == 0) return;
                Increment();
                Incrementing = true;
            }
        }
    }
    public void OnPointerUp(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right)
        {
            Incrementing = false;
            CurrentSpeed = 0f;
            Accumulator = 0f;
            Quantity = 0;
        }
    }
    public override void ShiftRightMouseClicked()
    {
        if (PlayerUI.UI_Crafting.TargetCrafting.InstantCraft())
        {
            UpdateUI();
        }
    }
    public void Increment()
    {
        if (PlayerUI.UI_Crafting.TargetCrafting.CraftItem())
        {
            UpdateUI();
            PlayerUI.UI_DragGhost.UpdateUI();
        }
    }
    public virtual void Update()
    {
        if (!Incrementing) return;

        float dt = Time.deltaTime;
        CurrentSpeed = Mathf.Min(CurrentSpeed + Acceleration * dt, MaxSpeed);
        Accumulator += CurrentSpeed * dt;

        while (Accumulator >= 1f)
        {
            Increment();
            Accumulator -= 1f;
        }
    }
    public override void OnPointerExit(PointerEventData eventData)
    {
        PointerIsOver = false;
        CurrentSpeed = 0f;
        Accumulator = 0f;
    }
}
