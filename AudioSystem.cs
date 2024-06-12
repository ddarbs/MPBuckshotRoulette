using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class AudioSystem : MonoBehaviour // this is all client side
{
#region Inspector Refs
    [SerializeField] private AudioSource i_Music;
    [SerializeField] private AudioSource i_Audio;

    [Header("UI")] 
    [SerializeField] private AudioClip i_ButtonHover;
    [SerializeField] private AudioClip i_ButtonClickDown;
    [SerializeField] private AudioClip i_ButtonClickUp;
    [SerializeField] private AudioClip i_SliderChange;

    [Header("Lobby")] 
    [SerializeField] private AudioClip i_LobbyJoin;
    [SerializeField] private AudioClip i_LobbyLeave;

    [Header("Game")] 
    [SerializeField] private AudioClip i_ShotgunLoadShell;
    [SerializeField] private AudioClip i_ShotgunRack;
    [SerializeField] private AudioClip i_ShotgunFireLive;
    [SerializeField] private AudioClip i_ShotgunFireBlank;
    [SerializeField] private AudioClip i_HealthDecrease;
    [SerializeField] private AudioClip i_HealthIncrease;
    [SerializeField] private AudioClip i_NewRound;
    [SerializeField] private AudioClip i_Heartbeat;
    [SerializeField] private AudioClip i_Item_Beer;
    [SerializeField] private AudioClip i_Item_Smokes;
    [SerializeField] private AudioClip i_Item_Magnifying;
    [SerializeField] private AudioClip i_Item_Inverter;
    [SerializeField] private AudioClip i_Item_Phone;
    [SerializeField] private AudioClip i_Item_Saw;
    [SerializeField] private AudioClip i_Item_Pills;
    [SerializeField] private AudioClip i_HealthSpawnNoise;
    [SerializeField] private AudioClip i_DiceRoll;
    [SerializeField] private AudioClip i_DiceRollVictory;
#endregion Inspector Refs

#region Variables
    private const float c_BaseMusicVolume = 0.2f;
    private static AudioSystem i_Instance;

    [SerializeField] private float i_PitchDelay;
#endregion

    private void Awake()
    {
        // overwrite the static ref on every new scene
        i_Instance = this;
    }

#region Scene Stuff
    private void Start() // this will cause a bunch of "there are no audio listeners in the scene" while not a Client, but it won't happen when we connect Lobby->Game 
    {
        StartCoroutine(IncreasePitch());
    }

    private IEnumerator IncreasePitch()
    {
        i_Music.Play();
        if (i_PitchDelay > 0f)
        {
            yield return new WaitForSeconds(i_PitchDelay);
            while (i_Music.pitch < 1f)
            {
                yield return new WaitForSeconds(0.2f);
                i_Music.pitch += 0.01f;
            }
            if (i_Music.pitch > 1f)
            {
                i_Music.pitch = 1f;
            }
        }
    }

    public static void ExitScene()
    {
        i_Instance.StartCoroutine(i_Instance.ExitSceneThread());
    }

    private IEnumerator ExitSceneThread()
    {
        while (i_Music.volume > 0f)
        {
            i_Music.volume -= 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
    }
#endregion Scene Stuff

#region Lobby Stuff
    public static void Lobby_Join()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_LobbyJoin, 0.8f);
    }
    
    public static void Lobby_Leave()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_LobbyLeave, 1f);
    }
#endregion Lobby Stuff

#region Buttons
    public static void Button_Hover()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_ButtonHover, 0.25f);
    }
    
    public static void Button_ClickDown()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_ButtonClickDown, 0.4f);
    }
    
    public static void Button_ClickUp()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_ButtonClickUp, 0.4f);
    }
#endregion Buttons

#region Sliders
    public static void Slider_Change()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_SliderChange, 0.8f);
    }
#endregion Sliders

#region Game Stuff
    public static void Game_OnDamage()
    {
        i_Instance.i_Music.volume = 0f;
        Game_Heartbeat();
        i_Instance.StartCoroutine(i_Instance.IncreaseVolume());
    }
    private IEnumerator IncreaseVolume()
    {
        yield return new WaitForSeconds(2f);
        float l_AddVolume = c_BaseMusicVolume / 10f;
        while (i_Music.volume < c_BaseMusicVolume)
        {
            yield return new WaitForSeconds(0.1f);
            i_Music.volume += l_AddVolume;
        }

        if (i_Music.volume > c_BaseMusicVolume)
        {
            i_Music.volume = c_BaseMusicVolume;
        }
    }
    
    public static void Game_Shotgun_LoadShell()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_ShotgunLoadShell, 1f);
    }
    
    public static void Game_Shotgun_Rack()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_ShotgunRack, 1f);
    }
    
    public static void Game_Shotgun_FireLive()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_ShotgunFireLive, 1f);
    }
    
    public static void Game_Shotgun_FireBlank()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_ShotgunFireBlank, 1f);
    }
    
    public static void Game_HealthIndicator_Decrease()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_HealthDecrease, 1f);
    }
    
    public static void Game_HealthIndicator_Increase()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_HealthIncrease, 1f);
    }

    public static void Game_Heartbeat()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_Heartbeat, 1f);
    }

    public static void Game_NewRound()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_NewRound, 1f);
    }
    
    public static void Game_Item_Beer()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_Item_Beer, 1f);
    }
    
    public static void Game_Item_Smoke()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_Item_Smokes, 1f);
    }
    public static void Game_Item_Magnify()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_Item_Magnifying, 1f);
    }
    public static void Game_Item_Inverter()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_Item_Inverter, 1f);
    }
    public static void Game_Item_Phone()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_Item_Phone, 1f);
    }
    public static void Game_Item_Saw()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_Item_Saw, 1f);
    }
    public static void Game_Item_Pills()
    {
        i_Instance.i_Audio.PlayOneShot(i_Instance.i_Item_Pills, 1f);
    }
    
#endregion Game Stuff
}
