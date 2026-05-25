using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{

    public Sprite mute;
    public Sprite unmute;
    public Image soundicon;
    public AudioSource[] bgms;
    public bool soundon;

    Button buttonmute;
    public void Awake()
    {

        mute = Resources.Load("mut", typeof(Sprite)) as Sprite;
        unmute = Resources.Load("unmut", typeof(Sprite)) as Sprite;
        soundicon = GameObject.Find("vol").GetComponent<Image>();
        buttonmute = GameObject.Find("vol").GetComponent<Button>();
        bgms = GameObject.Find("holder").GetComponents<AudioSource>();
        soundon = true;
        buttonmute.onClick.AddListener(mutepressed);
    }

    public void mutepressed()
    {
        if (soundon == true)
        {
            soundon = false;
            soundicon.sprite = mute;
            foreach (AudioSource bgm in bgms)
                bgm.Pause();
        }
        else
        {
            soundon = true;
            soundicon.sprite = unmute;
            foreach (AudioSource bgm in bgms)
                bgm.Play();
        }
    }
}