using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pelota : MonoBehaviour
{
    Rigidbody rb;
    GameObject redo;
    Collider col;
    void Start()
    {
      
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        col = GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
       
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.name == "Red")
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
