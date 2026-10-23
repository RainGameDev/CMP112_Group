using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public ArmourItem helmetItem;
    public ArmourItem chestItem;
    public ArmourItem legsItem;
    public ArmourItem feetItem;
    public WeaponItem mainHandItem;

    public void EquipItem(EquipmentItem item)
    {
        switch (item.slot)
        {
            case EquipmentSlot.Head:
                helmetItem = item as ArmourItem;
                break;
            case EquipmentSlot.Chest:
                chestItem = item as ArmourItem;
                break;
            case EquipmentSlot.Legs:
                legsItem = item as ArmourItem;
                break;
            case EquipmentSlot.Feet:
                feetItem = item as ArmourItem;
                break;
            case EquipmentSlot.MainHand:
                mainHandItem = item as WeaponItem;
                break;
        }
    }

    public void UnequipItem(EquipmentItem item)
    {
        switch (item.slot)
        {
            case EquipmentSlot.Head:
                helmetItem = null;
                break;
            case EquipmentSlot.Chest:
                chestItem = null;
                break;
            case EquipmentSlot.Legs:
                legsItem = null;
                break;
            case EquipmentSlot.Feet:
                feetItem = null;
                break;
            case EquipmentSlot.MainHand:
                mainHandItem = null;
                break;
        }
    }
}
