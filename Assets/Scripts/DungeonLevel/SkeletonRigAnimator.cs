using UnityEngine;

// Animates the PSX skeleton model using code, because the model we downloaded
// (vinrax's PSX Skeleton) has a rig (bones) but no animations.
//
// Every frame it puts all the bones back to their starting pose, then bends
// them to make this frame's pose:
//  - walking: legs swing and knees bend, the hips bob, the arms swing,
//    and it all goes faster the faster the skeleton is moving
//  - idle: arms hang down (the model is in a T-pose by default) and the chest
//    "breathes" a tiny bit
//  - attacking: the wind-up raises the sword arm back and the jaw chatters,
//    then the lunge thrusts the arm forward
//  - being hit, startled or in the air: the arms flail
//
// Bones are found by their names in the model ("hips", "thigh.L" and so on).
// Arm poses are given as directions relative to the skeleton ("upper arm points
// down and a bit out"), so it doesn't matter which way each bone's own axes face.
public class SkeletonRigAnimator : MonoBehaviour
{
    public Transform model;          // the skinned model under the enemy
    public float strideLength = 0.9f;   // metres per step, so the legs match the walking speed
    public float legSwing = 32f;        // how far the legs swing forward/back (degrees)
    public float kneeBend = 45f;        // how much the knees bend (degrees)
    public float armSwing = 22f;        // how far the arms swing (degrees)

    Rigidbody rb;    // used to read how fast the skeleton is moving
    Enemy enemy;     // used to know if it's winding up, lunging or stunned (empty on the boss)

    // the bones we move
    Transform hips, chest, jaw;
    Transform thighL, shinL, thighR, shinR;
    Transform weaponUpper, weaponLower, weaponHand;   // the arm on the character's right
    Transform offUpper, offLower, offHand;            // the other arm
    float weaponSide, offSide;                        // +1 right, -1 left in character space

    Transform[] posed;      // every bone we change
    Quaternion[] rest;      // their starting rotations, so we can reset them every frame
    Vector3 hipsRest;       // the hips' starting position (they bob up and down)

    float phase, speed;     // phase = how far through the walk cycle we are, speed = smoothed walking speed

    // how much of each pose to blend in, each goes smoothly between 0 and 1
    float windupW, lungeW, stunW, airW;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        enemy = GetComponent<Enemy>();
        if (!model) model = transform;

        // find the bones by name
        hips = Find("hips");
        chest = Find("chest");
        jaw = Find("jaw");
        thighL = Find("thigh.L"); shinL = Find("shin.L");
        thighR = Find("thigh.R"); shinR = Find("shin.R");

        // Blender's .L/.R can end up mirrored after import, so work out which arm is on the right
        // (we check which side of the skeleton the "L" arm is actually on)
        Transform upperL = Find("upper_arm.L"), upperR = Find("upper_arm.R");
        bool lIsRight = upperL && transform.InverseTransformPoint(upperL.position).x > 0f;
        string w = lIsRight ? ".L" : ".R", o = lIsRight ? ".R" : ".L";
        weaponUpper = Find("upper_arm" + w); weaponLower = Find("forearm" + w); weaponHand = Find("hand" + w);
        offUpper = Find("upper_arm" + o); offLower = Find("forearm" + o); offHand = Find("hand" + o);
        weaponSide = 1f;
        offSide = -1f;

        // remember the starting rotation of every bone we're going to move
        posed = new[] { hips, chest, jaw, thighL, shinL, thighR, shinR, weaponUpper, weaponLower, offUpper, offLower };
        rest = new Quaternion[posed.Length];
        for (int i = 0; i < posed.Length; i++)
            if (posed[i]) rest[i] = posed[i].localRotation;
        if (hips) hipsRest = hips.localPosition;
    }

    // LateUpdate runs after the skeleton has moved this frame, so the pose matches where it ended up
    void LateUpdate()
    {
        float dt = Time.deltaTime;

        // back to the bind pose, then build this frame's pose on top
        for (int i = 0; i < posed.Length; i++)
            if (posed[i]) posed[i].localRotation = rest[i];
        if (hips) hips.localPosition = hipsRest;

        // how fast it's moving along the floor (smoothed so the walk doesn't twitch)
        Vector3 v = rb ? rb.linearVelocity : Vector3.zero;
        float flat = new Vector3(v.x, 0f, v.z).magnitude;
        speed = Mathf.Lerp(speed, flat, 10f * dt);
        float walk = Mathf.Clamp01(speed / 2.5f);       // 0 = standing still, 1 = full walk
        phase += speed * dt * Mathf.PI / strideLength;  // moves faster through the step cycle when moving faster
        float s = Mathf.Sin(phase);                     // goes -1 -> 1 -> -1 once per pair of steps

        // fade each pose in or out depending on what the Enemy AI is doing
        windupW = Mathf.MoveTowards(windupW, enemy && enemy.IsWindingUp ? 1f : 0f, 8f * dt);
        lungeW = Mathf.MoveTowards(lungeW, enemy && enemy.IsLunging ? 1f : 0f, 12f * dt);
        stunW = Mathf.MoveTowards(stunW, enemy && enemy.IsStunned ? 1f : 0f, 10f * dt);
        airW = Mathf.MoveTowards(airW, Mathf.Abs(v.y) > 1.5f ? 1f : 0f, 6f * dt);   // moving fast up or down = in the air

        // the skeleton's right side. Rotating around this tips things forward/back
        Vector3 right = transform.right;

        // hips bob with the step
        if (hips) hips.position += Vector3.up * (Mathf.Abs(s) * 0.03f * walk);

        // chest: breathe, lean back on the wind-up, throw forward on the lunge
        float lean = Mathf.Sin(Time.time * 2f) * 2f - 18f * windupW + 24f * lungeW - 12f * stunW;
        if (chest) chest.rotation = Quaternion.AngleAxis(lean, right) * chest.rotation;

        // legs: swing from the hip, bend the knee on the way through
        // (the two legs use opposite values of s, so one goes forward while the other goes back)
        Leg(thighL, shinL, -s, walk, right);
        Leg(thighR, shinR, s, walk, right);

        // arms: hang down instead of the T-pose, swing opposite the legs
        // These are the directions each part of the arm points in for each pose
        // (x = out to the side, y = up, z = forward)
        Vector3 upDown = new Vector3(0.22f, -1f, 0.05f), lowDown = new Vector3(0.08f, -1f, 0.3f);           // hanging down
        Vector3 upRaise = new Vector3(0.35f, 0.75f, -0.45f), lowRaise = new Vector3(0.1f, 0.9f, 0.3f);      // raised back for the wind-up
        Vector3 upThrust = new Vector3(0.15f, 0.05f, 1f), lowThrust = new Vector3(0.05f, -0.1f, 1f);        // thrust forward for the lunge
        Vector3 upFlail = new Vector3(0.7f, 0.3f, -0.2f), lowFlail = new Vector3(0.4f, 0.8f, 0f);           // flailing when hit or in the air

        // sword arm: mix the poses together, and only swing it while walking (not while attacking)
        Vector3 wUp = Blend(upDown, upRaise, upThrust, upFlail);
        Vector3 wLow = Blend(lowDown, lowRaise, lowThrust, lowFlail);
        Arm(weaponUpper, weaponLower, weaponHand, wUp, wLow, weaponSide, s * armSwing * walk * (1f - windupW - lungeW), right);

        // other arm: hangs or flails, and reaches forward a bit during the wind-up for balance
        Vector3 oUp = Vector3.Lerp(Vector3.Lerp(upDown, upFlail, Mathf.Max(stunW, airW)), new Vector3(0.4f, -0.6f, 0.4f), windupW);
        Vector3 oLow = Vector3.Lerp(lowDown, lowFlail, Mathf.Max(stunW, airW));
        Arm(offUpper, offLower, offHand, oUp, oLow, offSide, -s * armSwing * walk, right);

        // jaw chatters while it winds up
        if (jaw)
        {
            float open = 6f + windupW * 18f * Mathf.Abs(Mathf.Sin(Time.time * 25f)) + stunW * 20f;
            jaw.rotation = Quaternion.AngleAxis(open, right) * jaw.rotation;
        }
    }

    // arm pose for the weapon arm: hanging / raised back / thrust forward / flailing
    // (Lerp mixes two directions, so poses blend smoothly instead of snapping)
    Vector3 Blend(Vector3 down, Vector3 raise, Vector3 thrust, Vector3 flail)
    {
        Vector3 d = Vector3.Lerp(down, flail, Mathf.Max(stunW, airW));
        d = Vector3.Lerp(d, raise, windupW);
        return Vector3.Lerp(d, thrust, lungeW);
    }

    // swings one leg: rotate the thigh forward/back, then bend the knee
    void Leg(Transform thigh, Transform shin, float swing, float walk, Vector3 right)
    {
        if (thigh) thigh.rotation = Quaternion.AngleAxis(swing * legSwing * walk, right) * thigh.rotation;
        // the knee bends most while the leg is passing underneath the body
        float bend = Mathf.Max(0f, Mathf.Sin(phase + (swing > 0f ? 0f : Mathf.PI) + Mathf.PI * 0.5f)) * kneeBend * walk;
        // always a slight bend (+4 degrees) so the legs never look locked straight
        if (shin) shin.rotation = Quaternion.AngleAxis(bend + 4f, right) * shin.rotation;
    }

    // poses one arm: point the upper arm, add the walking swing, then point the forearm
    void Arm(Transform upper, Transform lower, Transform hand, Vector3 upDir, Vector3 lowDir, float side, float swing, Vector3 right)
    {
        if (!upper || !lower) return;
        Aim(upper, lower.position, Local(upDir, side));
        upper.rotation = Quaternion.AngleAxis(swing, right) * upper.rotation;
        if (hand) Aim(lower, hand.position, Local(lowDir, side));
    }

    // character-space direction (x mirrored per side) to world space
    // (so "out to the side" means right for the right arm and left for the left arm)
    Vector3 Local(Vector3 dir, float side) => transform.TransformDirection(new Vector3(dir.x * side, dir.y, dir.z)).normalized;

    // turn a bone so the way it points (towards its child) matches a world direction
    // FromToRotation gives the turn needed to get from the current direction to the new one
    static void Aim(Transform bone, Vector3 childPos, Vector3 worldDir)
    {
        Vector3 current = childPos - bone.position;
        if (current.sqrMagnitude < 1e-6f) return;
        bone.rotation = Quaternion.FromToRotation(current.normalized, worldDir) * bone.rotation;
    }

    // looks through every object in the model for one with this exact name
    Transform Find(string boneName)
    {
        foreach (var t in model.GetComponentsInChildren<Transform>(true))
            if (t.name == boneName) return t;
        return null;
    }
}
