using Sirenix.OdinInspector;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [BoxGroup("References")]
    public EquipmentManager equipmentManager;

    [BoxGroup("References")]
    public Animator animator;

    [BoxGroup("References")]
    public ContainerUI containerUI;

    [BoxGroup("References")]
    public PlayerInputs inputs;

    [BoxGroup("Combat")]
    public float attackLockDuration = 0.5f;

    [BoxGroup("Combat")]
    public bool isAttackLocked;

    float attackLockTimeLeft;

    [BoxGroup("Inputs")]
    public bool isAttacking;

    void Start()
    {
        inputs = new();
        inputs.Enable();
    }

    void Update()
    {
        bool attackPressed = inputs.Combat.Attack.WasPressedThisFrame();

        if (attackLockTimeLeft > 0f)
        {
            attackLockTimeLeft -= Time.deltaTime;
            if (attackLockTimeLeft <= 0f)
                isAttackLocked = false;
        }

        if (
            attackPressed
            && !isAttackLocked
            && equipmentManager.mainHandItem != null
            && !containerUI.containerUI.activeInHierarchy
        )
        {
            Debug.Log("Attacking");
            animator.SetBool("Swing", true);
            isAttackLocked = true;
            attackLockTimeLeft = attackLockDuration;
        }
        else
        {
            animator.SetBool("Swing", false);
        }

        isAttacking = attackPressed;
    }
}
