using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class materiImagehandler : MonoBehaviour
{
    public RectTransform content;
    public ScrollRect sr;
    
    public string[] judulnya;
    public Sprite[] gambarnya;

    public Text uijudul;
    public Image uiimage;

    int ind;
    int splitlimit;
    public GameObject lanjutbutton;



    private void Start()
    {
        
        
        ind = 0;
        splitlimit = gambarnya.Length-1;
        Setmateri(ind);

    }
    public void Setmateri(int index) {

        uijudul.text = judulnya[index];
        uiimage.sprite = gambarnya[index];
        StartCoroutine(paksanaik());
    }
    public void Nextmateri()
    {

        if (ind < splitlimit)
        {
            ind++;
            uijudul.text = judulnya[ind];
            uiimage.sprite = gambarnya[ind];
            StartCoroutine(paksanaik());
            if (ind == splitlimit)
                lanjutbutton.SetActive(true);
        }
        

    }
    public void Prevmateri()
    {
        ind--;
        uijudul.text = judulnya[ind];
        uiimage.sprite = gambarnya[ind];
        StartCoroutine(paksanaik());

    }

    IEnumerator paksanaik()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        yield return null;
        sr.verticalNormalizedPosition = 1f;
    }
}