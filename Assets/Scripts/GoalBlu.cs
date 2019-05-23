using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalBlu : MonoBehaviour
{
    [SerializeField]
    GameObject zero;
    [SerializeField]
    GameObject one;

    float scoreblu;
        void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
      
        Debug.Log(scoreblu);
        if (scoreblu == 0)
        {
            zero.SetActive(true);
        }
        if (scoreblu == 1)
        {
            zero.SetActive(false);
            one.SetActive(true);
        }
    }
}
