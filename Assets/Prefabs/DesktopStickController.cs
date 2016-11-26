using UnityEngine;
using System.Collections;

// For debugging without a HMD. Controls a stick that represents a LEAP finger.
public class DesktopStickController : MonoBehaviour {

    static float xSpeed = 2.0f, ySpeed = 2.0f;

    GameObject stick;

    void Start()
    {
        stick = GameObject.Find("Stick");
    }

	void Update()
    {
        Quaternion rx = Quaternion.AngleAxis(xSpeed * Input.GetAxis("Mouse X"), Vector3.up);
        Quaternion ry = Quaternion.AngleAxis(-ySpeed * Input.GetAxis("Mouse Y"), stick.transform.right);
        stick.transform.rotation = rx * ry * stick.transform.rotation;
	}
}
