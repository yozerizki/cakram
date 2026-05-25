using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class materihandler : MonoBehaviour
{
    public RectTransform content;
    public ScrollRect sr;



    public string[] judulnya;
    [TextArea]
    public string[] teksnya;
    public Sprite[] gambarnya;
    public AudioClip[] suaranya;

    public AudioSource tts;
    public Text uijudul;
    public Image uigambar;
    public TMP_Text uibody;
    public Image uiimage;

    AudioSource audioSource1;


    int ind;
    int startingind;
    int splitlimit;
    public GameObject lanjutbutton;
    dontDestroy dd;



    private void Start()
    {
        lanjutbutton.SetActive(false);
        dd = GameObject.Find("holder").GetComponent<dontDestroy>();
        tts = this.gameObject.GetComponent<AudioSource>();
        audioSource1 = dd.gameObject.GetComponent<AudioSource>();
        ind = dd.startidmateri;
        startingind = dd.startidmateri;
        splitlimit = judulnya.Length;
        Setmateri(ind);

    }
    public void Setmateri(int index)
    {
        tts.Stop();
        uigambar.sprite = gambarnya[index];
        uijudul.text = judulnya[index];
        uibody.text = teksnya[index];
        StartCoroutine(DuckAudioWhileSFXPlays(suaranya[index]));
        StartCoroutine(paksanaik());
    }
    public void Nextmateri()
    {

        if (ind < splitlimit - 1)
        {
            StopAllCoroutines();
            ind++;
            tts.Stop();
            uigambar.sprite = gambarnya[ind];
            uijudul.text = judulnya[ind];
            uibody.text = teksnya[ind];
            StartCoroutine(DuckAudioWhileSFXPlays(suaranya[ind]));
            StartCoroutine(paksanaik());
            if (ind == splitlimit - 1)
                lanjutbutton.SetActive(true);
        }


    }
    public void Prevmateri()
    {
        if (ind > startingind)
        {
            StopAllCoroutines();
            ind--;
            tts.Stop();
            
            uigambar.sprite = gambarnya[ind];
            uijudul.text = judulnya[ind];
            uibody.text = teksnya[ind];
            StartCoroutine(DuckAudioWhileSFXPlays(suaranya[ind]));
            StartCoroutine(paksanaik());
        }

    }


    IEnumerator paksanaik()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        yield return null;
        sr.verticalNormalizedPosition = 1f;
    }

    private IEnumerator DuckAudioWhileSFXPlays(AudioClip clip)
    {
      

        // Turunkan volume ke 40%
        audioSource1.volume =  0.18f;

        tts.PlayOneShot(clip);

        // Tunggu durasi clip (fallback untuk PlayOneShot)
        yield return new WaitForSeconds(clip.length);

        // Kembalikan volume
        audioSource1.volume = 1;
    }
    public void stopaudio() {
        tts.Stop();
        audioSource1.volume = 1;
    }

}