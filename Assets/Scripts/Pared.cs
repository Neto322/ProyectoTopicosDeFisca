using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pared : MonoBehaviour
{
    [SerializeField]
    float radio;
    [SerializeField]
    float fuerza;
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Patada");
        Rigidbody RBO = collision.gameObject.GetComponent<Rigidbody>();

        RBO.AddExplosionForce(fuerza, transform.position, radio);
    }
}
