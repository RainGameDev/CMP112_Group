using Sirenix.OdinInspector;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [BoxGroup("Items")]
    public ArmourItem helmetItem;

    [BoxGroup("Items")]
    public ArmourItem chestItem;

    [BoxGroup("Items")]
    public ArmourItem legsItem;

    [BoxGroup("Items")]
    public ArmourItem feetItem;

    [BoxGroup("Items")]
    public WeaponItem mainHandItem;

    [BoxGroup("Equipped Objects")]
    public Transform mainHandHolder;

    [BoxGroup("Equipped Objects")]
    public GameObject mainHandObject;

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
                if (item as WeaponItem != mainHandItem)
                {
                    mainHandItem = item as WeaponItem;
                    mainHandObject = Instantiate(item.equippedPrefab, mainHandHolder);
                }
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
                if (mainHandObject != null)
                    Destroy(mainHandObject);

                mainHandItem = null;
                break;
        }
    }
}
