using System;
using Sirenix.OdinInspector;
using UnityEngine;

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Unique,
}

[CreateAssetMenu(fileName = "Data", menuName = "Items/BaseItem", order = 1)]
public class Item : ScriptableObject
{
    [HorizontalGroup("Info", 100)]
    [HideLabel]
    [PreviewField(100)]
    public Sprite itemIcon;

    [VerticalGroup("Info/UI")]
    [LabelWidth(75)]
    public String itemName;

    [VerticalGroup("Info/UI")]
    [LabelWidth(75)]
    public ItemRarity itemRarity;

    [VerticalGroup("Info/UI")]
    [TextArea]
    [LabelWidth(75)]
    public String itemDescription;

    [BoxGroup("Game")]
    [PreviewField(75)]
    public GameObject droppedPrefab;

    [BoxGroup("Game")]
    public bool stacks;
}
