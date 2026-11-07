using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponGate : MonoBehaviour
{
    void Start()
    {
        if (LevelManager.Instance && LevelManager.Instance.HasWeapon) Open();
        LevelManager.WeaponObtained += Open;
    }

    void OnDestroy()
    {
        LevelManager.WeaponObtained -= Open;
    }

    void Open()
    {
        GetComponent<Collider>().enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (LevelManager.IsPlayerBody(collision.collider) && LevelManager.Instance)
            LevelManager.Instance.ShowMessage("You won't last down there unarmed. Check the chest.", 2.5f);
    }
}
