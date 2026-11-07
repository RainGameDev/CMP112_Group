using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Camera camera;

    void Start()
    {
        camera = Camera.main;
    }

    void Update()
    {
        transform.LookAt(camera.transform);
    }
}
