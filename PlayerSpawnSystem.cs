using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Component.Observing;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using TTVadumb.Lobby;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlayerSpawnSystem : NetworkBehaviour
{

#region Inspector Refs
    [SerializeField] private EventSystem i_SceneEventSystem;
    [SerializeField] private AudioSource i_MusicSource;

    [SerializeField] private NetworkObject i_PlayerPrefab;
    [SerializeField] private Transform i_PlayerOneSpawnPos, i_PlayerTwoSpawnPos;
    [SerializeField] private ServerSideHealth i_PlayerOneHealth, i_PlayerTwoHealth;
    [SerializeField] private PlayerItemSystem i_PlayerItemSystem;

    [SerializeField] private NetworkObject i_TestP1HP, i_TestP2HP;
    [SerializeField] private GameObject i_PlayerOneObject, i_PlayerTwoObject;
#endregion Inspector Refs

#region Variables
    private int i_PlayerCount = 0;
    public NetworkConnection i_PlayerOne, i_PlayerTwo;
#endregion Variables
    
#region Server
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        NetworkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
        NetworkManager.SceneManager.OnClientPresenceChangeStart += OnClientPresenceChangeStart;
        NetworkManager.SceneManager.OnClientPresenceChangeEnd += OnClientPresenceChangeEnd;
        
        Debug.Log("starting to add nobs to match");
        var _nobs = GameObject.FindObjectsOfType<NetworkObject>();
        //MatchCondition.AddToMatch(0, _nobs, NetworkManager);
        Debug.Log("all nobs added to match 0");
        
        i_SceneEventSystem.enabled = false;
        i_MusicSource.Stop();
        i_MusicSource.enabled = false;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        
        ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        NetworkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
        NetworkManager.SceneManager.OnClientPresenceChangeStart -= OnClientPresenceChangeStart;
        NetworkManager.SceneManager.OnClientPresenceChangeEnd -= OnClientPresenceChangeEnd;
    }
    
    private void OnRemoteConnectionState(NetworkConnection _conn, RemoteConnectionStateArgs _args) // this does not fire when changing scenes
    {
        switch (_args.ConnectionState)
        {
            case RemoteConnectionState.Started:
                Debug.Log("remote connection started");
                break;
            case RemoteConnectionState.Stopped:
                Debug.Log("remote connection stopped");
                break;
        }
    }

    private void OnClientLoadedStartScenes(NetworkConnection _conn, bool _asServer) // this does not fire when changing scenes
    {
        Debug.Log("on client loaded start scenes");
        // remove scripting symbol LOBBY_SYSTEM in build settings
    #if !LOBBY_SYSTEM
        i_PlayerCount++;
        Vector3 _spawnPosition = Vector3.zero;
        Quaternion _spawnQuaternion = Quaternion.identity;
        switch (i_PlayerCount)
        {
            case 1:
                i_PlayerOneHealth.Setup(_conn);
                i_PlayerOne = _conn;
                _spawnPosition = i_PlayerOneSpawnPos.position;
                _spawnQuaternion = i_PlayerOneSpawnPos.rotation;
                break;
            case 2:
                i_PlayerTwoHealth.Setup(_conn);
                i_PlayerTwo = _conn;
                _spawnPosition = i_PlayerTwoSpawnPos.position;
                _spawnQuaternion = i_PlayerTwoSpawnPos.rotation;
                break;
            default:
                Debug.LogError("something's fucked up here", this);
                break;
        }

        NetworkObject _player = Instantiate(i_PlayerPrefab, _spawnPosition, _spawnQuaternion);
       
        ServerManager.Spawn(_player, _conn);
        SceneManager.AddOwnerToDefaultScene(_player);
    #endif
    }

    private void OnClientPresenceChangeStart(ClientPresenceChangeEventArgs _args)
    {
        if (!_args.Added)
        {
            return;
        }
    }
    
    private void OnClientPresenceChangeEnd(ClientPresenceChangeEventArgs _args) // this will be used when we connect the lobby system
    {
        Debug.Log("OnClientPresenceChangeEnd - start");
        // add scripting symbol LOBBY_SYSTEM in build settings
    #if LOBBY_SYSTEM
        if (!_args.Added)
        {
            return;
        }
        if (_args.Scene.handle != gameObject.scene.handle)
        {
            return;
        }
        Debug.Log("OnClientPresenceChangeEnd - past returns");
        
        i_PlayerCount++;
        Vector3 _spawnPosition = Vector3.zero;
        Quaternion _spawnQuaternion = Quaternion.identity;
        switch (i_PlayerCount)
        {
            case 1:
                i_PlayerOneHealth.Setup(_args.Connection);
                i_PlayerOne = _args.Connection;
                _spawnPosition = i_PlayerOneSpawnPos.position;
                _spawnQuaternion = i_PlayerOneSpawnPos.rotation;
                break;
            case 2:
                i_PlayerTwoHealth.Setup(_args.Connection);
                i_PlayerTwo = _args.Connection;
                _spawnPosition = i_PlayerTwoSpawnPos.position;
                _spawnQuaternion = i_PlayerTwoSpawnPos.rotation;
                break;
            default:
                Debug.LogError("something's fucked up here", this);
                break;
        }
        Debug.Log("OnClientPresenceChangeEnd - past player setup");

        // BUG: this isn't running correctly on playflow 
        NetworkObject _player = Instantiate(i_PlayerPrefab, _spawnPosition, _spawnQuaternion);
        ServerManager.Spawn(_player, _args.Connection, gameObject.scene);
        //MatchCondition.AddToMatch(0, _player, NetworkManager);
        Debug.Log("OnClientPresenceChangeEnd - past player spawn");
        if (i_PlayerCount == 1)
        {
            i_PlayerOneObject = _player.gameObject;
            Target_MovePlayers(_args.Connection, new GameObject[] {i_PlayerOneObject});
        }
        else if (i_PlayerCount == 2)
        {
            i_PlayerTwoObject = _player.gameObject;
            Target_MovePlayers(_args.Connection, new GameObject[] {i_PlayerOneObject, i_PlayerTwoObject});
        }

        Debug.Log("OnClientPresenceChangeEnd - past target moveplayers");
        /*if (i_PlayerCount == 2)
        {
            StartCoroutine(Test_DelayClientLoad());
        }*/
    #endif
    }
    
    /*private IEnumerator Test_DelayClientLoad()
    {
        yield return new WaitForSeconds(2f);
        //MatchCondition.AddToMatch(0, i_TestP1HP, NetworkManager);
        //MatchCondition.AddToMatch(0, i_TestP2HP, NetworkManager);
        Debug.Log("starting to add nobs to match");
        var _nobs = GameObject.FindObjectsOfType<NetworkObject>();
        //MatchCondition.AddToMatch(0, _nobs, NetworkManager);
        Debug.Log("all nobs added to match 0");
    }*/
#endregion Server

[TargetRpc]
private void Target_MovePlayers(NetworkConnection _conn, GameObject[] _players)
{
    //GameObject[] l_Players = GameObject.FindGameObjectsWithTag("Player");
    foreach (var _player in _players)
    {
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_player, gameObject.scene);
    }
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(gameObject.scene);
}
}
