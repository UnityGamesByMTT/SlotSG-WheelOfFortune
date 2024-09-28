using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AudioController : MonoBehaviour
{
    [SerializeField] private AudioSource bg_adudio;
    [SerializeField] private AudioSource audioPlayer_wl;
    [SerializeField] private AudioSource audioPlayer_button;
    [SerializeField] private AudioSource audioPlayer_Spin;

    [Header("clips")]
    [SerializeField] private AudioClip SpinButtonClip;
    [SerializeField] private AudioClip SpinClip;
    [SerializeField] private AudioClip BonusSpinClip;
    [SerializeField] private AudioClip Button;
    [SerializeField] private AudioClip Win_Audio;
    [SerializeField] private AudioClip Bonus_Win_Audio;
    [SerializeField] private AudioClip NormalBg_Audio;
    [SerializeField] private AudioClip BonusBg_Audio;

    private void Awake()
    {
        playBgAudio();
        //if (bg_adudio) bg_adudio.Play();
        //audioPlayer_button.clip = clips[clips.Length - 1];
    }

    internal void PlayWLAudio(string type)
    {
        StopWLAaudio();
        // audioPlayer_wl.loop=loop;

        switch (type)
        {

            case "win":
                audioPlayer_wl.clip = Win_Audio;
                break;
            case "bonuswin":
                audioPlayer_wl.clip = Bonus_Win_Audio;
                break;

                //index = 3;

        }
        //audioPlayer_wl.clip = clips[index];
        //audioPlayer_wl.loop = true;
        audioPlayer_wl.Play();

    }

    internal void PlaySpinAudio(string type = "default")
    {

        if (audioPlayer_Spin)
        {
            if (type == "bonus")
                audioPlayer_Spin.clip = BonusSpinClip;
            else
                audioPlayer_Spin.clip = SpinClip;

            audioPlayer_Spin.Play();
        }

    }

    internal void StopSpinAudio()
    {

        if (audioPlayer_Spin) audioPlayer_Spin.Stop();

    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {

            bg_adudio.Pause();
            audioPlayer_wl.Pause();
            audioPlayer_button.Pause();
        }
        else
        {
            if (!bg_adudio.mute) bg_adudio.UnPause();
            if (!audioPlayer_wl.mute) audioPlayer_wl.UnPause();
            if (!audioPlayer_button.mute) audioPlayer_button.UnPause();

        }
    }



    internal void playBgAudio(string type = "default")
    {


        //int randomIndex = UnityEngine.Random.Range(0, Bg_Audio.Length);
        StopBgAudio();
        if (bg_adudio)
        {
            if (type == "bonus")
                bg_adudio.clip = BonusBg_Audio;
            else
                bg_adudio.clip = NormalBg_Audio;


            bg_adudio.Play();
        }

    }

    internal void PlayButtonAudio(string type = "default")
    {

        if (type == "spin")
            audioPlayer_button.clip = SpinButtonClip;
        else
            audioPlayer_button.clip = Button;

        //StopButtonAudio();
        audioPlayer_button.Play();
        // Invoke("StopButtonAudio", audioPlayer_button.clip.length);

    }

    internal void StopWLAaudio()
    {
        audioPlayer_wl.Stop();
        audioPlayer_wl.loop = false;
    }

    internal void StopButtonAudio()
    {

        audioPlayer_button.Stop();

    }


    internal void StopBgAudio()
    {
        bg_adudio.Stop();

    }


    internal void ToggleMute(float value, string type = "all")
    {

        switch (type)
        {
            case "bg":
                    bg_adudio.mute = value <0.1;
                    bg_adudio.volume = value;
                break;
            case "button":
                audioPlayer_button.mute = value<0.1;
                audioPlayer_Spin.mute=value<0.1;

                audioPlayer_button.volume = value;
                audioPlayer_Spin.volume = value;
                break;
            case "wl":
                audioPlayer_wl.mute = value<0.1;
                audioPlayer_wl.volume = value;
                break;
            case "all":
                audioPlayer_wl.mute = value<0.1;
                bg_adudio.mute = value<0.1;
                audioPlayer_button.mute = value<0.1;
                
                audioPlayer_wl.volume = value;
                bg_adudio.volume = value;
                audioPlayer_button.volume = value;
                break;
        }
    }

}
