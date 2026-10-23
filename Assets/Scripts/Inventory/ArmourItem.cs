using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Items/Armour", order = 1)]
public class ArmourItem : EquipmentItem
{
    [BoxGroup("Armour Info")]
    public int defence;
}
