using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanCamera : MonoBehaviour
{
    public float MinY = 17f;
    Vector3 pos;
    float x;
    float z;
    float y;

    void Update()
    {
        x = Input.GetAxis("Mouse X") * Time.unscaledDeltaTime * -15f;
        z = Input.GetAxis("Mouse Y") * Time.unscaledDeltaTime * -15f;
        y = Input.GetAxis("Mouse ScrollWheel") * Time.unscaledDeltaTime * -500f;
        pos = transform.position;
        if (Input.GetButton("Mouse1"))
        {
            transform.position += new Vector3(
                Mathf.Abs(pos.x + x) > 25 ? 0 : x,
                0,
                Mathf.Abs(pos.z + z) > 25 ? 0 : z);
        }
        if (transform.position.y + y > MinY)
        {
            transform.position += new Vector3(0, y, 0);
        }
    }
}