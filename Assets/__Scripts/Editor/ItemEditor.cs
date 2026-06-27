using FishNet.Object;
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Item))]
public class ItemEditor : Editor
{
    private Item itemIdentity;

    private void OnEnable()
    {
        itemIdentity = (Item)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawIdentityFields();
        DrawTypeSpecificSection(itemIdentity.ItemType);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(itemIdentity);
        }
    }
    private void DrawIdentityFields()
    {
        itemIdentity.Name = EditorGUILayout.TextField("Name", itemIdentity.Name);
        itemIdentity.MaxStackSize = EditorGUILayout.IntField("Max Stack Size", itemIdentity.MaxStackSize);
        itemIdentity.Icon = (Sprite)EditorGUILayout.ObjectField("Icon", itemIdentity.Icon, typeof(Sprite), false);
        itemIdentity.WorldItemPrefab = (WorldItemGameObject)EditorGUILayout.ObjectField("World Item", itemIdentity.WorldItemPrefab, typeof(WorldItemGameObject), false);
        itemIdentity.ItemSlotType = (ItemSlotType)EditorGUILayout.EnumPopup("Item Slot", itemIdentity.ItemSlotType);
        itemIdentity.ItemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", itemIdentity.ItemType);
    }

    private void DrawTypeSpecificSection(ItemType itemType)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Type Specific Data", EditorStyles.boldLabel);

        switch (itemType)
        {
            case ItemType.Material:
                DrawMaterialFields();
                break;

            case ItemType.MeleeWeapon:
                DrawMeleeWeaponFields();
                break;

            case ItemType.RangedWeapon:
                DrawRangedWeaponFields();
                break;

            case ItemType.Ammo:
                DrawAmmoFields();
                break;

            case ItemType.Pick:
                DrawPickFields();
                break;

            case ItemType.Axe:
                DrawAxeFields();
                break;
        }
    }
    private void DrawMaterialFields()
    {
        itemIdentity.ResourceType =
            (ResourceType)EditorGUILayout.EnumPopup("Resource Type", itemIdentity.ResourceType);

        MaterialType[] validMaterials = GetValidMaterials(itemIdentity.ResourceType);

        string[] options = new string[validMaterials.Length];
        for (int i = 0; i < validMaterials.Length; i++)
        {
            options[i] = validMaterials[i].ToString();
        }

        int currentIndex = Array.IndexOf(validMaterials, itemIdentity.MaterialType);
        if (currentIndex < 0) currentIndex = 0;

        int selectedIndex = EditorGUILayout.Popup("Material Type", currentIndex, options);

        itemIdentity.MaterialType = validMaterials[selectedIndex];
    }

    private void DrawMeleeWeaponFields()
    {
        itemIdentity.Damage = EditorGUILayout.IntField("Damage", itemIdentity.Damage);
        itemIdentity.Accuracy = EditorGUILayout.IntField("Accuracy", itemIdentity.Accuracy);
        itemIdentity.SwingSpeed = EditorGUILayout.IntField("Swing Speed", itemIdentity.SwingSpeed);
        itemIdentity.EquipPrefab = (NetworkObject)EditorGUILayout.ObjectField("Equip Prefab", itemIdentity.EquipPrefab, typeof(NetworkObject), false);
    }

    private void DrawRangedWeaponFields()
    {
        EditorGUILayout.LabelField("Ranged Weapon Data", EditorStyles.boldLabel);

        itemIdentity.Damage = EditorGUILayout.IntField("Damage", itemIdentity.Damage);
        itemIdentity.Accuracy = EditorGUILayout.IntField("Accuracy", itemIdentity.Accuracy);
        itemIdentity.ProjectileSpeed = EditorGUILayout.IntField("Projectile Speed", itemIdentity.ProjectileSpeed);
        itemIdentity.RecoveryTime = EditorGUILayout.IntField("Recovery Time", itemIdentity.RecoveryTime);
        itemIdentity.ChargeTime = EditorGUILayout.IntField("Charge Time", itemIdentity.ChargeTime);
        itemIdentity.RequiresAmmo = EditorGUILayout.Toggle("Requires Ammo", itemIdentity.RequiresAmmo);
        itemIdentity.EquipPrefab = (NetworkObject)EditorGUILayout.ObjectField("Equip Prefab", itemIdentity.EquipPrefab, typeof(NetworkObject), false);
    }

    private void DrawAmmoFields()
    {
        itemIdentity.Damage = EditorGUILayout.IntField("Damage", itemIdentity.Damage);
        itemIdentity.Accuracy = EditorGUILayout.IntField("Accuracy", itemIdentity.Accuracy);
        itemIdentity.ProjectileSpeed = EditorGUILayout.IntField("Projectile Speed", itemIdentity.ProjectileSpeed);
        itemIdentity.EquipPrefab = (NetworkObject)EditorGUILayout.ObjectField("Equip Prefab", itemIdentity.EquipPrefab, typeof(NetworkObject), false);
    }

    private void DrawPickFields()
    {
        itemIdentity.Damage = EditorGUILayout.IntField("Damage", itemIdentity.Damage);
        itemIdentity.SwingSpeed = EditorGUILayout.IntField("Swing Speed", itemIdentity.SwingSpeed);
        itemIdentity.EquipPrefab = (NetworkObject)EditorGUILayout.ObjectField("Equip Prefab", itemIdentity.EquipPrefab, typeof(NetworkObject), false);
    }

    private void DrawAxeFields()
    {
        itemIdentity.Damage = EditorGUILayout.IntField("Damage", itemIdentity.Damage);
        itemIdentity.SwingSpeed = EditorGUILayout.IntField("Swing Speed", itemIdentity.SwingSpeed);
        itemIdentity.EquipPrefab = (NetworkObject)EditorGUILayout.ObjectField("Equip Prefab", itemIdentity.EquipPrefab, typeof(NetworkObject), false);
    }
    public static MaterialType[] GetValidMaterials(ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.Wood:
                return new[]
                {
                    MaterialType.Birch,
                    MaterialType.Oak,
                    MaterialType.Ash,
                    MaterialType.Phantom,
                    MaterialType.Mantium,
                    MaterialType.Swift
                };

            case ResourceType.Ore:
                return new[]
                {
                    MaterialType.CopperOre,
                    MaterialType.TinOre,
                    MaterialType.IronOre,
                    MaterialType.Coal,
                    MaterialType.MithrilOre,
                    MaterialType.SolsteelOre,
                    MaterialType.BrimsteelOre,
                    MaterialType.SwiftsteelOre,
                    MaterialType.Sulphur
                };

            case ResourceType.Metal:
                return new[]
                {
                    MaterialType.Bronze,
                    MaterialType.Steel,
                    MaterialType.Mithril,
                    MaterialType.Solsteel,
                    MaterialType.Brimsteel,
                    MaterialType.Swiftsteel
                };
            case ResourceType.String:
                return new[]
                {
                    MaterialType.String,
                };
            case ResourceType.Crystal:
                return new[]
                {
                    MaterialType.FireCrystal,
                };
            case ResourceType.None:
            default:
                return new[] { MaterialType.None };
        }
    }
}
