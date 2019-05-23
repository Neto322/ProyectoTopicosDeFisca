using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control_Manager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        // requires you to set up axes "Joy0X" - "Joy3X" and "Joy0Y" - "Joy3Y" in the Input Manger
        for (int i = 0; i < 4; i++)
        {
            if (Mathf.Abs(Input.GetAxis("Joy" + i + "X")) > 0.2 ||
                Mathf.Abs(Input.GetAxis("Joy" + i + "Y")) > 0.2)
            {
                Debug.Log(Input.GetJoystickNames()[i] + " is moved");
            }
        }
    }
}
