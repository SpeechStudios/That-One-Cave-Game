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
    Phantom,
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
    public int MaterialGroup;
}


[CreateAssetMenu(menuName = "Crafting/Recipe", fileName = "New Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public int ID;
    public CraftingComponent[] Pattern = new CraftingComponent[9];
    public Item CraftedOutcome;
    public int CraftedOutcomeQuantity;
}
