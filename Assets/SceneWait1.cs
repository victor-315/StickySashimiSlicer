using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneWait1 : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(transition());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator transition()
    {
        
        yield return new WaitForSeconds(1.2f);
        SceneManager.LoadScene("Transition1");
    }
}