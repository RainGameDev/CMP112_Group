using UnityEngine;

public class Hitbox : MonoBehaviour
{
    public EquipmentManager equipmentManager;

    public bool isStatic;

    private void OnTriggerEnter(Collider other)
    {
        if (isStatic)
        {
            return;
        }

        if (other.gameObject.GetComponent<Hitbox>() != null)
        {
            if (equipmentManager)
            {
                if (other.gameObject.GetComponentInParent<HealthManager>() is HealthManager hm)
                {
                    Debug.Log("Hit an enemy");
                    hm.ChangeHealth(-equipmentManager.mainHandItem.damage);
                }
            }
        }
    }
}
