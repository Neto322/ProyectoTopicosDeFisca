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
    // Start is called before the first frame update
    void Start()
    {
        render = GetComponent<Renderer>();
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
    }
}
