using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GamepadInput;

public class ControlNeon : MonoBehaviour
{
    [SerializeField]
    Material mat;
    float conteo;
    bool gamestart;
    void Start()
    {
        conteo = 3;
        gamestart = false;
        mat = GetComponent<Material>();
        mat = (Material)Resources.Load("Estado_Neon", typeof(Material));

    }

    // Update is called once per frame
    void Update()
    {
        if (GamePad.GetButtonDown(GamePad.Button.A, GamePad.Index.Any))
        {
            gamestart = true;

        }
        if ( gamestart == true)
        {
           if (conteo >= 0.7)
            {
                conteo -= Time.deltaTime;
            }
            mat.SetFloat("_RimPower", conteo);
        }
    }
}
