using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GamepadInput;

public class Disc_Spawn : MonoBehaviour
{
    [SerializeField]
    GameObject disco;
    [SerializeField]
    GameObject imagen;
    [SerializeField]
    GameObject texto;
    Transform trans;
    public float scoreblue;
    public float scorered;
    float contar;
    float player;
    bool gamestart;
    void Start()
    {
        gamestart = false;
        contar = 3;
        trans = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GamePad.GetButtonDown(GamePad.Button.A, GamePad.Index.Any))
        {
            gamestart = true;
            texto.SetActive(false);
            imagen.SetActive(false);
            contar -= Time.deltaTime;
            if (contar <= 0)
            {
                player = Random.Range(1, 3);
                if (player == 2)
                {
                    Instantiate(disco, new Vector3(0, -6.33f, -18.42f), trans.rotation);
                }
                if (player == 1)
                {
                    Instantiate(disco, new Vector3(0, -6.33f, -30.23f), trans.rotation);

                }
                contar = 3;
            }
        }
        
        if(gamestart == true)
        {
            if (GameObject.FindGameObjectsWithTag("disco").Length == 0)
            {
                contar -= Time.deltaTime;
                if (contar <= 0)
                {
                    player = Random.Range(1, 3);
                    if (player == 2)
                    {
                        Instantiate(disco, new Vector3(0, -6.33f, -18.42f), trans.rotation);
                    }
                    if (player == 1)
                    {
                        Instantiate(disco, new Vector3(0, -6.33f, -30.23f), trans.rotation);

                    }
                    contar = 3;
                }
            }
        }
        

    }
}
