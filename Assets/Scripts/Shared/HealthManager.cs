using UnityEngine;

public class HealthManager : MonoBehaviour
{
    public GameObject deathObject;
    public float maxHealth;
    public float currentHealth;

    public void ChangeHealth(float amount)
    {
        currentHealth += amount;

        if (currentHealth <= 0)
        {
            Destroy(deathObject);
        }
    }
}
