using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sliders : MonoBehaviour
{
    Renderer render;
    [SerializeField]
    Slider sldColorPower;
    [SerializeField]
    Slider sldDesintegrate;
    [SerializeField]
    Slider sldRimPower;
    [SerializeField]
    Slider sldR;
    [SerializeField]
    Slider sldG;
    [SerializeField]
    Slider sldB;
    [SerializeField]
    Shader Shader2;
    [SerializeField]
    Shader Shader1;
    // Start is called before the first frame update
    void Start()
    {
        render = GetComponent<Renderer>();
        Shader1 = Shader.Find("Shader Graphs/Shader");
        Shader2 = Shader.Find("Shader Graphs/Shader2");
        render.material.shader = Shader1;

    }

    // Update is called once per frame
    void Update()
    {
        render.material.SetFloat("_ColorPower", sldColorPower.value);
        render.material.SetFloat("_Desintegrate", sldDesintegrate.value);
        render.material.SetFloat("_RimPower", sldRimPower.value);
        render.material.SetFloat("_R", sldR.value);
        render.material.SetFloat("_G", sldG.value);
        render.material.SetFloat("_B", sldB.value);

        if (Input.GetKeyDown(KeyCode.Space)) 
            {
            Debug.Log("Shader Cambiado");
            render.material.shader = Shader2;
            } 
    }
}
