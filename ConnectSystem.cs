using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using TTVadumb.Lobby;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ConnectSystem : NetworkBehaviour
{
    [SerializeField] private AudioListener i_SceneAudioListener;
    [SerializeField] private EventSystem i_SceneEventSystem;
    [SerializeField] private AudioSource i_MusicSource;
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        NetworkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;

        i_SceneAudioListener.enabled = false;
        i_SceneEventSystem.enabled = false;
        i_MusicSource.Stop();
        i_MusicSource.enabled = false;
        //GameObject.FindGameObjectWithTag("VolumeSlider").transform.GetChild(0).gameObject.SetActive(false); // doesn't matter server cant click it anyways
        
        SceneLoadData _sld = new SceneLoadData("LobbyScene");
        _sld.Options.AutomaticallyUnload = false;
        base.SceneManager.LoadGlobalScenes(_sld);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        
        NetworkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
    }

    private void OnClientLoadedStartScenes(NetworkConnection _conn, bool _asServer)
    {
        SceneUnloadData _sud = new SceneUnloadData("ConnectScene");
        _sud.Options.Mode = UnloadOptions.ServerUnloadMode.KeepUnused;
        
        base.SceneManager.UnloadConnectionScenes(_conn, _sud);
        // TODO: look at the fishnet demo script for scenemanager loading, see if they have a cleaner way to load/unload
    }

    /*public override void OnStartClient()
    {
        base.OnStartClient();
        AudioSystem.ExitScene();
    }*/
}
