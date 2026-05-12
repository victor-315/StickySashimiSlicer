using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneWait : MonoBehaviour
{
    public Animator _animator;
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
        _animator.Play("Transitionscene1");
        yield return new WaitForSeconds(2.0f);
        SceneManager.LoadScene("Level");
    }
}