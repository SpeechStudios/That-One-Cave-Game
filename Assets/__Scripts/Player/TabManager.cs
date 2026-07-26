using UnityEngine;
using UnityEngine.UI;

public enum Tab { Player = 0, Crafting = 1, Smelting = 2, Reinforce = 3}
public class TabManager : MonoBehaviour
{
    public static TabManager Instance { get; private set; }

    public GameObject PlayerCanvas;
    public GameObject CraftingCanvas;
    public GameObject SmeltingCanvas;
    public GameObject ReinforceCanvas;

    public TabButton PlayerButton;
    public TabButton CraftingButton;
    public TabButton SmeltingButton;
    public TabButton ReinforceButton;


    internal Tab CurrentTab;
    public void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        CurrentTab = Tab.Player;
        PlayerButton.Select();
    }

    public void SelectButton(int buttonType)
    {
        if (CurrentTab == (Tab)buttonType) return;
        switch (CurrentTab)
        {
            case Tab.Player:
                PlayerCanvas.SetActive(false);
                PlayerButton.Deselect();
                break;
            case Tab.Crafting:
                CraftingCanvas.SetActive(false);
                CraftingButton.Deselect();
                break;
            case Tab.Smelting:
                SmeltingCanvas.SetActive(false);
                SmeltingButton.Deselect();
                break;
            case Tab.Reinforce:
                ReinforceCanvas.SetActive(false);
                ReinforceButton.Deselect();
                break;
            default:
                break;
        }
        CurrentTab = (Tab)buttonType;
        switch (CurrentTab)
        {
            case Tab.Player:
                PlayerCanvas.SetActive(true);
                PlayerButton.Select();
                break;
            case Tab.Crafting:
                CraftingCanvas.SetActive(true);
                CraftingButton.Select();
                break;
            case Tab.Smelting:
                SmeltingCanvas.SetActive(true);
                SmeltingButton.Select();
                break;
            case Tab.Reinforce:
                ReinforceCanvas.SetActive(true);
                ReinforceButton.Select();
                break;
            default:
                break;
        }
    }
}
