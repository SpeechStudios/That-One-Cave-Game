using System.Collections.Generic;
using UnityEngine;


public class AbilityDatabase : MonoBehaviour
{
    public static AbilityDatabase Instance { get; private set; }
    public AbilityData[] Entries;
    private Dictionary<System.Type, AbilityData> LookUp;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[AbilityDatabase] Duplicate instance found — destroying the new one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildLookup();
    }

    private void BuildLookup()
    {
        LookUp = new Dictionary<System.Type, AbilityData>(Entries.Length);
        foreach (var data in Entries)
        {
            if (!LookUp.TryAdd(data.GetType(), data))
            {
                Debug.LogWarning($"[AbilityDatabase] Duplicate AbilityData of type '{data.GetType().Name}' — first one wins.");
            }
        }
    }

    public AbilityData Get(System.Type dataType)
    {

        if (LookUp.TryGetValue(dataType, out var data))
            return data;

        Debug.LogError($"[AbilityDatabase] No AbilityData of type '{dataType.Name}' registered. ");
        return null;
    }
}

public static class AbilityFactory
{
    public static T Create<T>(Weapon weapon) where T : Ability, new()
    {
        var ability = new T();
        var data = AbilityDatabase.Instance.Get(ability.DataType);
        ability.Initialize(weapon, data);
        return ability;
    }
}