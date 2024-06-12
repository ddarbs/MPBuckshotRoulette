using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    [SerializeField] private AudioMixer i_AudioMixer;
    [SerializeField] private Slider i_VolumeSlider;
    [SerializeField] private Image i_VolumeIcon;
    [SerializeField] private Sprite i_VolumeUnmutedSprite, i_VolumeMutedSprite;

    private bool i_VolumeMuted = false;
    private float i_LastVolumeValue = 1f;

    public void Slider_UpdateAudioMixer(float _value)
    {
        float _mixerValue = Mathf.Log10(_value) * 20;
        i_AudioMixer.SetFloat("Volume", _mixerValue);
        
        if (_value > 0.01f)
        {
            if (i_VolumeMuted)
            {
                i_VolumeIcon.sprite = i_VolumeUnmutedSprite;
                i_VolumeMuted = false;
            }
            
            i_LastVolumeValue = _value;
        }
    }

    public void Button_ToggleAudio()
    {
        i_VolumeMuted = !i_VolumeMuted;
        switch (i_VolumeMuted)
        {
            case true:
                i_VolumeSlider.value = i_VolumeSlider.minValue;
                i_VolumeIcon.sprite = i_VolumeMutedSprite;
                break;
            case false:
                i_VolumeSlider.value = i_LastVolumeValue;
                i_VolumeIcon.sprite = i_VolumeUnmutedSprite;
                break;
        }
    }
}
