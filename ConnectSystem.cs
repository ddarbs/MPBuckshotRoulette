using System.Collections;
using System.Collections.Generic;
using FishNet.Component.Observing;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Managing.Server;
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
    [SerializeField] private GameObject i_VersionFailedText, i_ClientLoginButton;
    
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
        Target_CheckVersion(_conn);
        
        /*SceneUnloadData _sud = new SceneUnloadData("ConnectScene");
        _sud.Options.Mode = UnloadOptions.ServerUnloadMode.KeepUnused;
        
        base.SceneManager.UnloadConnectionScenes(_conn, _sud);
        // TODO: look at the fishnet demo script for scenemanager loading, see if they have a cleaner way to load/unload*/
    }

    /*public override void OnStartClient()
    {
        base.OnStartClient();
        CheckVersion(Application.version);
    }*/

    [TargetRpc]
    private void Target_CheckVersion(NetworkConnection _conn)
    {
        Server_CheckVersion(base.LocalConnection, Application.version);
    }

    [ServerRpc(RequireOwnership = false)]
    private void Server_CheckVersion(NetworkConnection _conn, string _version)
    {
        if (_version != Application.version)
        {
            SceneUnloadData _sud = new SceneUnloadData("LobbyScene");
            _sud.Options.Mode = UnloadOptions.ServerUnloadMode.KeepUnused;
        
            Target_VersionFailed(_conn);
            
            base.SceneManager.UnloadConnectionScenes(_conn, _sud);

            StartCoroutine(Server_DelayKick(_conn));
        }
        else
        {
            SceneUnloadData _sud = new SceneUnloadData("ConnectScene");
            _sud.Options.Mode = UnloadOptions.ServerUnloadMode.KeepUnused;
        
            base.SceneManager.UnloadConnectionScenes(_conn, _sud);
            // TODO: look at the fishnet demo script for scenemanager loading, see if they have a cleaner way to load/unload
        }
    }

    [TargetRpc]
    private void Target_VersionFailed(NetworkConnection _conn)
    {
        i_ClientLoginButton.SetActive(false);
        i_VersionFailedText.SetActive(true);
    }

    private IEnumerator Server_DelayKick(NetworkConnection _conn)
    {
        yield return new WaitForSeconds(1f);
        ServerManager.Kick(_conn, KickReason.UnexpectedProblem);
    }
}
