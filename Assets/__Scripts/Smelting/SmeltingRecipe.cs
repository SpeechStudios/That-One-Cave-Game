using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SmeltingRecipe", menuName = "New Smelting Recipe")]
public class SmeltingRecipe : ScriptableObject
{
    [HideInInspector] public int ID;
    public MaterialType Resource1;
    public MaterialType Resource2;
    public Item SmeltingOutcome;
    public int OutcomeQuantity = 1;
    public int SmeltingTime;
}
