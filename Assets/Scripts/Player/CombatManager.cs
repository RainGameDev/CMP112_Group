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

    [BoxGroup("Inputs")]
    public bool isAttacking;

    void Start()
    {
        inputs = new();
        inputs.Enable();
    }

    void Update()
    {
        isAttacking = inputs.Combat.Attack.WasPressedThisFrame();

        if (
            isAttacking
            && equipmentManager.mainHandItem != null
            && !containerUI.containerUI.activeInHierarchy
        )
        {
            Debug.Log("Attacking");
            animator.SetBool("Swing", true);
        }
        else
        {
            animator.SetBool("Swing", false);
        }
    }
}
