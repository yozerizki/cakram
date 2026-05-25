using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class materitexthandler : MonoBehaviour
{
    public RectTransform content;
    public ScrollRect sr;
   

    
    public string[] judulnya;

    [TextArea]
    public string[] teksnya;

    public Text uijudul;
    public TMP_Text uibody;
    public Image uiimage;

    int ind;
    int startingind;
    int splitlimit;
    public GameObject lanjutbutton;
    dontDestroy dd;



    private void Start()
    {
        dd = GameObject.Find("holder").GetComponent<dontDestroy>();
        ind = dd.startidmateri;
        startingind = dd.startidmateri;
        splitlimit = judulnya.Length;
        Setmateri(ind);

    }
    public void Setmateri(int index) {

        uijudul.text = judulnya[index];
        uibody.text = teksnya[index];
        StartCoroutine(paksanaik());
    }
    public void Nextmateri()
    {

        if (ind < splitlimit-1)
        {
            ind++;
            uijudul.text = judulnya[ind];
            uibody.text = teksnya[ind];
            StartCoroutine(paksanaik());
            if (ind == splitlimit - 1)
                lanjutbutton.SetActive(true);
        }
        

    }
    public void Prevmateri()
    {
        if (ind > startingind)
        {
            ind--;
            uijudul.text = judulnya[ind];
            uibody.text = teksnya[ind];
            StartCoroutine(paksanaik());
        }

    }


    IEnumerator paksanaik()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        yield return null;
        sr.verticalNormalizedPosition = 1f;
    }
}