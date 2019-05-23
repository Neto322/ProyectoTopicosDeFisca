using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Red : MonoBehaviour
{
    [SerializeField]
    GameObject disco;
 
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Physics.IgnoreCollision(disco.GetComponent<Collider>(), GetComponent<Collider>());

    }

}