using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoNotDestroyVolumeCanvas : MonoBehaviour
{
    private void Awake()
    {
        // if one already exists we don't need another
        if (GameObject.FindGameObjectsWithTag("VolumeSlider").Length > 1)
        {
            Destroy(this.gameObject);
        }
        else
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }
}
