using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreRojo : MonoBehaviour
{
    [SerializeField]
    GameObject zero;
    [SerializeField]
    GameObject one;
    [SerializeField]
    GameObject two;
    [SerializeField]
    GameObject three;
    [SerializeField]
    GameObject four;
    [SerializeField]
    float score;
    [SerializeField]
    GameObject victory;
    [SerializeField]
    ParticleSystem boom;
    float conteo;
    void Start()
    {
        conteo = 5;
        score = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (score == 0)
        {
            
            zero.SetActive(true);
        }
        if (score == 1)
        {
            zero.SetActive(false);
            one.SetActive(true);
          

        }
        if (score == 2)
        {
            one.SetActive(false);
            two.SetActive(true);
            

        }
        if (score == 3)
        {
            two.SetActive(false);
            three.SetActive(true);
           

        }
        if (score == 4)
        {
            three.SetActive(false);
            four.SetActive(true);
            

        }
        if (score == 5)
        {
            boom.Play();
            victory.SetActive(true);
            conteo -= Time.deltaTime;
            if (conteo <= 0)
            {
                Scene loadedLevel = SceneManager.GetActiveScene();
                SceneManager.LoadScene(loadedLevel.buildIndex);
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        boom.Play();
        if (collision.gameObject.name == "disco(Clone)")
        {
            score++;
        }

    }
}
