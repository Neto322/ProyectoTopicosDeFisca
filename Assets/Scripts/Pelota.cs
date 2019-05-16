using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pelota : MonoBehaviour
{
    [SerializeField]
    Material mat;
    Rigidbody rb;
    GameObject redo;
    Collider col;
    float conteo;
    float R;
    float G;
    float B;
    [SerializeField]
    ParticleSystem chispas;
    [SerializeField]
    AudioSource choque;
    [SerializeField]
    ParticleSystem Boom;
    Renderer render;
    bool muerto;
    void Start()
    {
        choque = GetComponent<AudioSource>();
          rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        col = GetComponent<Collider>();
        mat = GetComponent<Material>();
        mat = (Material)Resources.Load("Estado_Neon", typeof(Material));
        render = GetComponent<Renderer>();
        
    }

    // Update is called once per frame
    void Update()
    {

        mat.SetFloat("_RimColor", R);
        mat.SetFloat("_RimColor2", G);
        mat.SetFloat("_RimColor2", B);
    
    }
    private void OnCollisionEnter(Collision collision)
    {
        choque.Play();
        chispas.Play();
        R = Random.Range(0, 255);
        G = Random.Range(0, 255);
        B = Random.Range(0, 255);
        if (collision.gameObject.name == "Red")
        {
            redo = collision.gameObject;
            Physics.IgnoreCollision(col, redo.GetComponent<Collider>());
            Debug.Log("Oh yeah yaeh");
        }
        if (collision.gameObject.name == "PorteriaRojaCollider")
        {
            
            Destroy(gameObject);
        }
        if (collision.gameObject.name == "PorteriaAzulCollider")
        {
            
            Destroy(gameObject);

        }
    }
}
