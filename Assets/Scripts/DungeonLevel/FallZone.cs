using UnityEngine;

public class FallZone : MonoBehaviour
{
    public float damage;
    public Transform respawnAt;

    private void OnTriggerEnter(Collider other)
    {
        if (LevelManager.IsPlayerBody(other) && LevelManager.Instance)
            LevelManager.Instance.Fall(damage, respawnAt);
    }
}
