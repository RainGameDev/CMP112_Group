using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraOverview : MonoBehaviour
{
    public Transform focus;
    public float distance = 42f;
    public float linger = 1f;
    public float contactMargin = 0.15f;

    Collider barrier;
    float lastTouchTime = -100f;
    bool showing;

    void Awake()
    {
        barrier = GetComponent<Collider>();
    }

    void Update()
    {
        LevelManager lm = LevelManager.Instance;
        if (!lm || !focus) return;

        Bounds box = barrier.bounds;
        box.Expand(contactMargin * 2f);
        if (lm.PlayerTouching(box)) lastTouchTime = Time.time;

        bool touching = Time.time - lastTouchTime < linger;

        if (touching && !showing) lm.SetOverview(focus.position, distance);
        if (!touching && showing) lm.ClearOverview();
        showing = touching;
    }
}
