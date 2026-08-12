using System.Collections.Generic;
using UnityEngine;

public class Registry : MonoBehaviour
{
    public static Registry Instance { get; private set; }

    public static List<Item> GetStartingItems() => Instance?.StartingItems ?? new List<Item>();
    [SerializeField] private List<Item> StartingItems;

    [SerializeField] private List<Item> ItemList;
    private readonly Dictionary<int, Item> ItemLookUp = new();
    private int ItemID = 1;

    public List<CraftingRecipe> CraftingRecipeList;
    private readonly Dictionary<int, CraftingRecipe> CraftingRecipeLookUp = new();
    private int CraftingRecipeID = 1;

    public List<SmeltingRecipe> SmeltingRecipeList;
    private readonly Dictionary<int, SmeltingRecipe> SmeltingRecipeLookUp = new();
    private int SmeltingRecipeID = 1;

    public List<AbilityData> AbilityDataList;
    private readonly Dictionary<int, AbilityData> AbilityDataLookUp = new();
    private int AbilityDataID = 1;


    public void Init()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Build();
    }
    private void Build()
    {
        ItemLookUp.Clear();

        foreach (Item item in ItemList)
        {
            if (item == null) continue;
            item.ID = ItemID++;
            ItemLookUp.Add(item.ID, item);
        }
        foreach (CraftingRecipe recipe in CraftingRecipeList)
        {
            if (recipe == null) continue;
            recipe.ID = CraftingRecipeID++;
            CraftingRecipeLookUp.Add(recipe.ID, recipe);
        }
        foreach (SmeltingRecipe recipe in SmeltingRecipeList)
        {
            if (recipe == null) continue;
            recipe.ID = SmeltingRecipeID++;
            SmeltingRecipeLookUp.Add(recipe.ID, recipe);
        }
        foreach (AbilityData data in AbilityDataList)
        {
            if (data == null) continue;
            data.ID = AbilityDataID++;
            AbilityDataLookUp.Add(data.ID, data);
        }
    }
    public static Item GetItem(int id)
    {
        if (Instance == null)
        {
            Debug.LogError("[ItemRegistry] No instance in scene.");
            return null;
        }

        Instance.ItemLookUp.TryGetValue(id, out Item item);
        return item;
    }
    public static bool TryGetItem(int id, out Item item)
    {
        item = GetItem(id);
        return item != null;
    }

    public static CraftingRecipe GetCraftingRecipe(int id)
    {
        if (Instance == null)
        {
            Debug.LogError("[ItemRegistry] No instance in scene.");
            return null;
        }

        Instance.CraftingRecipeLookUp.TryGetValue(id, out var recipe);
        return recipe;
    }

    public static SmeltingRecipe GetSmeltingRecipe(int id)
    {
        if (Instance == null)
        {
            Debug.LogError("[ItemRegistry] No instance in scene.");
            return null;
        }

        Instance.SmeltingRecipeLookUp.TryGetValue(id, out var recipe);
        return recipe;
    }
    public static AbilityData GetAbilityData(int id)
    {
        if (Instance == null)
        {
            Debug.LogError("[ItemRegistry] No instance in scene.");
            return null;
        }

        Instance.AbilityDataLookUp.TryGetValue(id, out var data);
        return data;
    }
}