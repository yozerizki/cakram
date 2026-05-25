using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class scenemanager : MonoBehaviour
{
    AudioSource sfx;

    GameObject panelexit;
    private void Start()
    {

        sfx = this.gameObject.GetComponent<AudioSource>();
        panelexit = GameObject.FindGameObjectWithTag("panelexit");
        
        if (panelexit != null)
            panelexit.SetActive(false);
    }
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape)) {
            exitpressed();
        }
    }
    public void gotomateri()
    {
        StartCoroutine(waitandchangescene("scenemateri"));

    }

    public void restartscene() {
        StartCoroutine(waitandchangescene(SceneManager.GetActiveScene().name));
    }
    public void exitpressed() {
        panelexit.SetActive(true);
    }
    public void cancelexitgame()
    {
        panelexit.SetActive(false);
    }
    public void exitgame()
    {
        //sfx.PlayOneShot(kliksound);
        Application.Quit();
    }
    public string getThissceneName() {
        string a = SceneManager.GetActiveScene().name;
        return a;
    }

    IEnumerator waitandchangescene(string namascene)
    {
        //Wait Until Sound has finished playing
        while (sfx.isPlaying)
        {
            yield return null;
        }
        //Audio has finished playing, disable GameObject
        SceneManager.LoadScene(namascene);
    }

}
