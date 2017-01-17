using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanCamera : MonoBehaviour
{
    void Start()
    {
    }

    void Update()
    {
        var x = Input.GetAxis("Mouse X") * Time.unscaledDeltaTime * -15f;
        var z = Input.GetAxis("Mouse Y") * Time.unscaledDeltaTime * -15f;
        var y = Input.GetAxis("Mouse ScrollWheel") * Time.unscaledDeltaTime * -500f;
        if (Input.GetButton("Mouse1"))
        {
            transform.position += new Vector3(x, 0, z);
        }
        transform.position += new Vector3(0, y, 0);
    }
}