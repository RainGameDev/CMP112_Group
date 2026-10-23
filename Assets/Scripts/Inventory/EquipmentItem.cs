using Sirenix.OdinInspector;
using UnityEngine;

public enum EquipmentSlot
{
    Head,
    Chest,
    Legs,
    Feet,
    MainHand,
    OffHand,
}

public class EquipmentItem : Item
{
    [BoxGroup("Equipment Info")]
    public EquipmentSlot slot;
}
