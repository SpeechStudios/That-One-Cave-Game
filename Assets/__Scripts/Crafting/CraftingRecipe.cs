using System.Collections.Generic;
using UnityEngine;

public enum ResourceType
{
    None,
    Ore,
    Wood,
    Metal,
    String,
    Crystal
}
public enum MaterialType
{
    None,

    //Wood
    Birch,
    Oak,
    Ash,
    Phatnom,
    Mantium,
    Swift,

    //Ore
    CopperOre,
    TinOre,
    IronOre,
    Coal,
    MithrilOre,
    SolsteelOre,
    BrimsteelOre,
    SwiftsteelOre,
    Sulphur,

    //Metal
    Bronze,
    Steel,
    Mithril,
    Solsteel,
    Brimsteel,
    Swiftsteel,

    //Misc
    String,
    FireCrystal,
}
[System.Serializable]
public struct CraftingComponent
{
    public ResourceType ResourceType;
    public int RequiredQuantity;
}

[CreateAssetMenu(fileName = "CraftingRecipe", menuName = "New Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [HideInInspector] public int ID;
    public List<CraftingComponent> Components;
    public Item CraftedOutcome;
    public int CraftedOutcomeQuantity = 1;

    private const int MaxComponents = 4;

    private void OnValidate()
    {
        if (Components == null) return;

        if (Components.Count > MaxComponents)
        {
            Components.RemoveRange(MaxComponents, Components.Count - MaxComponents);
        }
    }
}
