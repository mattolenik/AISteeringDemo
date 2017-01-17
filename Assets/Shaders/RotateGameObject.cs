using UnityEngine;
using System.Collections;

public class RotateGameObject : MonoBehaviour
{
    public float rot_speed_x = 0;
    public float rot_speed_y = 0;
    public float rot_speed_z = 0;
    public bool local = false;

    void FixedUpdate()
    {
        if (local)
        {
            transform.Rotate(transform.up, Time.fixedDeltaTime * rot_speed_x, Space.Self);
        }
        else
        {
            transform.Rotate(Time.fixedDeltaTime * new Vector3(rot_speed_x, rot_speed_y, rot_speed_z), Space.World);
        }
    }
}
