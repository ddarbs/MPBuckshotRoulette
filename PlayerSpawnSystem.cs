using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using TTVadumb.Lobby;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerSpawnSystem : NetworkBehaviour
{

#region Inspector Refs
    [SerializeField] private EventSystem i_SceneEventSystem;
    [SerializeField] private AudioSource i_MusicSource;

    [SerializeField] private NetworkObject i_PlayerPrefab;
    [SerializeField] private Transform i_PlayerOneSpawnPos, i_PlayerTwoSpawnPos;
    [SerializeField] private ServerSideHealth i_PlayerOneHealth, i_PlayerTwoHealth;
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
        NetworkManager.SceneManager.OnClientPresenceChangeEnd += OnClientPresenceChangeEnd;

        i_SceneEventSystem.enabled = false;
        i_MusicSource.Stop();
        i_MusicSource.enabled = false;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        
        ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        NetworkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
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
        // comment this to connect lobby scene then uncomment OnClientPresenceChangeEnd
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
    }
    
    private void OnClientPresenceChangeEnd(ClientPresenceChangeEventArgs _args) // this will be used when we connect the lobby system
    {
        Debug.Log("client presence change end");
        // uncomment this to connect lobby scene then comment OnClientLoadedStartScenes
        /*if (!_args.Added)
        {
            return;
        }
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

        NetworkObject _player = Instantiate(i_PlayerPrefab, _spawnPosition, _spawnQuaternion);
        ServerManager.Spawn(_player, _args.Connection, _args.Scene);*/
    }
#endregion Server
}
