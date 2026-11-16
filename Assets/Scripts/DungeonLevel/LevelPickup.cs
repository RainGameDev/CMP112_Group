using UnityEngine;

public class LevelPickup : MonoBehaviour
{
    public enum Kind { Relic, Exit }

    public Kind kind;
    public GameObject removeOnPickup;

    private void OnTriggerEnter(Collider other)
    {
        LevelManager lm = LevelManager.Instance;
        if (!lm || !LevelManager.IsPlayerBody(other)) return;

        if (kind == Kind.Relic)
        {
            lm.AddMaxHealth();
            Debug.Log("Picked up relic");
            if (removeOnPickup) Destroy(removeOnPickup);
            Destroy(gameObject);
        }
        else if (lm.IsBossDefeated)
        {
            lm.CompleteLevel();
        }
    }
}
