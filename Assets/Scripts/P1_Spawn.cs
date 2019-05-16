using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class P1_Spawn : MonoBehaviour
{
    [SerializeField]
    GameObject P1;
    bool gamestart;
    Transform trans;
    void Start()
    {
        gamestart = true;
        trans = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (gamestart == true)
        {
            Instantiate(P1, trans.position, trans.rotation);
            gamestart = false;
        }
    }
}
