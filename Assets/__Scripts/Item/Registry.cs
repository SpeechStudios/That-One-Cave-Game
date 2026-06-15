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
            if (!ItemLookUp.TryAdd(item.ID, item))
                Debug.LogWarning($"[ItemRegistry] Duplicate ID {item.ID} — '{item.Name}' skipped.");
        }

        foreach (CraftingRecipe recipe in CraftingRecipeList)
        {
            if (recipe == null) continue;
            recipe.ID = CraftingRecipeID++;
            if (!CraftingRecipeLookUp.TryAdd(recipe.ID, recipe))
                Debug.LogWarning($"[ItemRegistry] Duplicate ID {recipe.ID} — '{recipe.CraftedOutcome.Name}' skipped.");
        }
        foreach (SmeltingRecipe recipe in SmeltingRecipeList)
        {
            if (recipe == null) continue;
            recipe.ID = SmeltingRecipeID++;
            if (!SmeltingRecipeLookUp.TryAdd(recipe.ID, recipe))
                Debug.LogWarning($"[ItemRegistry] Duplicate ID {recipe.ID} — '{recipe.SmeltingOutcome.Name}' skipped.");
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

        Instance.CraftingRecipeLookUp.TryGetValue(id, out CraftingRecipe recipe);
        return recipe;
    }

    public static bool TryGetCraftingRecipe(int id, out CraftingRecipe recipe)
    {
        recipe = GetCraftingRecipe(id);
        return recipe != null;
    }

    public static SmeltingRecipe GetSmeltingRecipe(int id)
    {
        if (Instance == null)
        {
            Debug.LogError("[ItemRegistry] No instance in scene.");
            return null;
        }

        Instance.SmeltingRecipeLookUp.TryGetValue(id, out SmeltingRecipe recipe);
        return recipe;
    }

    public static bool TryGetSmeltingRecipe(int id, out SmeltingRecipe recipe)
    {
        recipe = GetSmeltingRecipe(id);
        return recipe != null;
    }
}