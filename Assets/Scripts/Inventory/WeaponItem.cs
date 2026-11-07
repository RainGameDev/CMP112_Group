using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Items/Weapon", order = 1)]
public class WeaponItem : EquipmentItem
{
    [BoxGroup("Weapon Info")]
    public int damage;
}
