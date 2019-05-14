using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CambioDeShader : MonoBehaviour
{
    Renderer render;
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Shader Cambiado");
            render.material.shader = Shader2;
        }
    }
}
