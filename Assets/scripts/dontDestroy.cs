using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dontDestroy : MonoBehaviour
{
    public static dontDestroy Instance { get; private set; }

    public bool soundon;
    public int nyawa = 3;
    public int skor;
    public bool perisaion = false;
    public int soalke = 0;
    public Vector2 playerstartpos;
    public int soalpilihan;
    public bool[] isdone = new bool[5] { false, false, false, false, false };
    
    public string namafolderpuzzle;
    public int nomorsoal;
    public int banyaksoal;
    public int startidmateri;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject); // prevent duplicates!
    }
}
