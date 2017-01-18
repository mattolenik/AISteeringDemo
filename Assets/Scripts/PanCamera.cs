using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanCamera : MonoBehaviour
{
    public float MinY = 17f;

    void Update()
    {
        var y = Input.GetAxis("Mouse ScrollWheel") * Time.unscaledDeltaTime * -500f;
        if (transform.position.y + y > MinY)
        {
            transform.position += new Vector3(0, y, 0);
        }
    }
}