using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Component.Observing;
using FishNet.Connection;
using FishNet.Demo.AdditiveScenes;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using TMPro;
using TTVadumb.Lobby;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class LobbySystem : NetworkBehaviour
{
#region Inspector Refs
    [SerializeField] private AudioListener i_SceneAudioListener;
    [SerializeField] private EventSystem i_SceneEventSystem;
    [SerializeField] private AudioSource i_MusicSource;
    [SerializeField] private AudioSource i_AmbienceSource;

    [Header("Browser UI")]
    [SerializeField] private NetworkObject LobbyParent;
    [SerializeField] private NetworkObject LobbyPrefab;
    [SerializeField] private GameObject BrowserUI, LobbyUI;
    [SerializeField] private GridLayoutGroup i_LobbyGrid;

    [Header("Lobby UI")] 
    [SerializeField] private TextMeshProUGUI i_PlayerOneText;
    [SerializeField] private TextMeshProUGUI i_PlayerTwoText, i_LobbyIDText;
    [SerializeField] private Toggle i_ItemToggle, i_ItemCapToggle, i_ShellToggle, i_TurnToggle;
    [SerializeField] private Slider i_ItemSlider, i_ItemCapSlider, i_ShellSlider, i_TurnSlider;
    [SerializeField] private Button i_KickPlayerTwoButton, i_StartGameButton;
#endregion Inspector Refs

#region Variables
    /// <summary>
    /// Holds a list of all active Clients 
    /// </summary>
    private Dictionary<NetworkConnection, LobbyPlayer> PlayerList = new Dictionary<NetworkConnection, LobbyPlayer>();
    
    /// <summary>
    /// Holds a list of all active Lobbies
    /// </summary>
    private Dictionary<int, Lobby> LobbyList = new Dictionary<int, Lobby>();

    private int NextLobbyID = 1;

    private bool Client_IsLobbyOwner = false;
#endregion Variables
    
#region Server
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        NetworkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
        NetworkManager.SceneManager.OnClientPresenceChangeEnd += OnClientPresenceChangeEnd;
        
        i_SceneAudioListener.enabled = false;
        i_SceneEventSystem.enabled = false;
        i_MusicSource.Stop();
        i_MusicSource.enabled = false;
        i_AmbienceSource.Stop();
        i_AmbienceSource.enabled = false;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        
        ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        NetworkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
        NetworkManager.SceneManager.OnClientPresenceChangeEnd -= OnClientPresenceChangeEnd;
    }

    private void OnRemoteConnectionState(NetworkConnection _conn, RemoteConnectionStateArgs _args)
    {
        switch (_args.ConnectionState)
        {
            case RemoteConnectionState.Started:
                break;
            case RemoteConnectionState.Stopped:
                switch (PlayerList[_conn].status)
                {
                    case PlayerState.Browsing:
                        // nothing special I think
                        break;
                    case PlayerState.Lobby:
                        if (LobbyList.ContainsKey(PlayerList[_conn].lobbyID)) // if the quitter was in a lobby
                        {
                            int l_id = PlayerList[_conn].lobbyID;
                            if (LobbyList[l_id].owner == _conn) // if the quitter is the lobby owner
                            {
                                ServerManager.Despawn(LobbyList[l_id].handler.gameObject);
                                
                                if (LobbyList[l_id].playerCount > 1)
                                {
                                    PlayerList[LobbyList[l_id].playertwo].status = PlayerState.Browsing;
                                    PlayerList[LobbyList[l_id].playertwo].lobbyID = -1;
                                    Target_LeaveLobby(LobbyList[l_id].playertwo);
                                }

                                LobbyList.Remove(l_id);
                            }
                            else // if not just update back to 1/2 
                            {
                                LobbyList[l_id].playertwo = null;
                                LobbyList[l_id].playerCount--;   
                                Target_PlayerTwoLeftLobby(LobbyList[l_id].owner);
                                LobbyList[l_id].handler.GetComponent<LobbyHandler>().OnLobbyLeave();
                            }
                        }
                        break;
                    case PlayerState.Game:
                        // don't think this script will see anyone in a game
                        break;
                }
                
                PlayerList.Remove(_conn);
                break;
        }
    }

    private void OnClientLoadedStartScenes(NetworkConnection _conn, bool _asServer)
    {
        LobbyPlayer _newPlayer = new LobbyPlayer("player_" + Random.Range(0, 421));
        PlayerList.Add(_conn, _newPlayer);
        // TODO: target disable the connection ui and enable the menu/browser ui 
    }
    
    private void OnClientPresenceChangeEnd(ClientPresenceChangeEventArgs _args) // TODO: scenemanager stuff
    {
        if (PlayerList.ContainsKey(_args.Connection))
        {
            return;
        }
        
        // TODO: put players back into their lobby, or the browser if the host quit
    }
#endregion Server

#region ServerRPCs
    [ServerRpc(RequireOwnership = false)]
    private void Server_CreateLobby(NetworkConnection _conn)
    {
        // double check not trying to create a lobby while in a lobby
        if (PlayerList[_conn].lobbyID != -1 || PlayerList[_conn].status != PlayerState.Browsing)
        {
            return;
        }
        
        // update the creator's info
        PlayerList[_conn].status = PlayerState.Lobby;
        PlayerList[_conn].lobbyID = NextLobbyID;
        
        // create the UI
        NetworkObject _newLobbyUI = Instantiate(LobbyPrefab);
        _newLobbyUI.SetParent(LobbyParent);
        LobbyHandler _lobbyHandler = _newLobbyUI.GetComponent<LobbyHandler>();
        _lobbyHandler.Setup(PlayerList[_conn].name, this, NextLobbyID);
        
        // create the backend
        Lobby _newLobby = new Lobby(_lobbyHandler.gameObject, NextLobbyID, _conn);
        LobbyList.Add(NextLobbyID, _newLobby);
        NextLobbyID++;
        
        // BUG: for some reason PlayFlow is spawning it at 3x scale?
        _newLobbyUI.transform.localScale = new Vector3(1f, 1f, 1f);
        // BUG: playflow loves to instantiate at weird spots too
        Observer_NudgeLobbyGrid();
        
        // spawn it
        ServerManager.Spawn(_newLobbyUI, _conn);
        
        // update the ui for everyone
        _lobbyHandler.OnLobbyStart();
        

        // target creator into the lobby
        Target_CreateLobby(_conn, _newLobby.id, PlayerList[_conn].name, _newLobby.cfg_ItemsEnabled, _newLobby.cfg_ItemCount, _newLobby.cfg_ItemTurnCapEnabled, _newLobby.cfg_ItemTurnCap, _newLobby.cfg_StaticShellCountEnabled, _newLobby.cfg_ShellCount, _newLobby.cfg_TurnTimerEnabled, _newLobby.cfg_TurnTimeoutIndex);
    }

    public void Server_JoinLobby(NetworkConnection _conn, int _lobbyID)
    {
        if (LobbyList[_lobbyID].playerCount >= 2)
        {
            Debug.LogWarning("this shouldn't happen uh oh oopsies", this);
            return;
        }
        
        LobbyList[_lobbyID].playerCount++;
        LobbyList[_lobbyID].playertwo = _conn;
        
        PlayerList[_conn].lobbyID = _lobbyID;
        PlayerList[_conn].status = PlayerState.Lobby;
        
        LobbyList[_lobbyID].handler.GetComponent<LobbyHandler>().OnLobbyJoin(PlayerList[_conn].name);
        
        Target_JoinLobby(_conn, _lobbyID, PlayerList[LobbyList[_lobbyID].owner].name, PlayerList[_conn].name, LobbyList[_lobbyID].cfg_ItemsEnabled, LobbyList[_lobbyID].cfg_ItemCount, LobbyList[_lobbyID].cfg_ItemTurnCapEnabled, LobbyList[_lobbyID].cfg_ItemTurnCap, LobbyList[_lobbyID].cfg_StaticShellCountEnabled, LobbyList[_lobbyID].cfg_ShellCount, LobbyList[_lobbyID].cfg_TurnTimerEnabled, LobbyList[_lobbyID].cfg_TurnTimeoutIndex);
        Target_PlayerTwoJoinedLobby(LobbyList[_lobbyID].owner, PlayerList[_conn].name);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void Server_LeaveLobby(NetworkConnection _conn)
    {
        int l_LobbyID = PlayerList[_conn].lobbyID;
        if (LobbyList[l_LobbyID].owner == _conn) // if the quitter is the lobby owner
        {
            ServerManager.Despawn(LobbyList[l_LobbyID].handler.gameObject);
                                
            if (LobbyList[l_LobbyID].playerCount > 1)
            {
                PlayerList[LobbyList[l_LobbyID].playertwo].status = PlayerState.Browsing;
                PlayerList[LobbyList[l_LobbyID].playertwo].lobbyID = -1;
                Target_LeaveLobby(LobbyList[l_LobbyID].playertwo);
            }

            LobbyList.Remove(l_LobbyID);
        }
        else // if not just update back to 1/2 
        {
            LobbyList[l_LobbyID].playertwo = null;
            LobbyList[l_LobbyID].playerCount--;
            Target_PlayerTwoLeftLobby(LobbyList[l_LobbyID].owner);
            LobbyList[l_LobbyID].handler.GetComponent<LobbyHandler>().OnLobbyLeave();
        }

        PlayerList[_conn].status = PlayerState.Browsing;
        PlayerList[_conn].lobbyID = -1;
        Target_LeaveLobby(_conn);
    }

    [ServerRpc(RequireOwnership = false)]
    private void Server_KickPlayerTwo(NetworkConnection _conn)
    {
        PlayerList[LobbyList[PlayerList[_conn].lobbyID].playertwo].status = PlayerState.Browsing;
        PlayerList[LobbyList[PlayerList[_conn].lobbyID].playertwo].lobbyID = -1;
        Target_LeaveLobby(LobbyList[PlayerList[_conn].lobbyID].playertwo);
        
        LobbyList[PlayerList[_conn].lobbyID].playertwo = null;
        LobbyList[PlayerList[_conn].lobbyID].playerCount--;
        Target_PlayerTwoLeftLobby(_conn);
        LobbyList[PlayerList[_conn].lobbyID].handler.GetComponent<LobbyHandler>().OnLobbyLeave();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void Server_SliderItemChange(NetworkConnection _conn, int _value)
    {
        LobbyList[PlayerList[_conn].lobbyID].cfg_ItemCount = _value;
        if (LobbyList[PlayerList[_conn].lobbyID].playerCount > 1)
        {
            Target_SliderItemChange(LobbyList[PlayerList[_conn].lobbyID].playertwo, _value);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void Server_SliderItemCapChange(NetworkConnection _conn, int _value)
    {
        LobbyList[PlayerList[_conn].lobbyID].cfg_ItemTurnCap = _value;
        if (LobbyList[PlayerList[_conn].lobbyID].playerCount > 1)
        {
            Target_SliderItemCapChange(LobbyList[PlayerList[_conn].lobbyID].playertwo, _value);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void Server_SliderShellChange(NetworkConnection _conn, int _value)
    {
        LobbyList[PlayerList[_conn].lobbyID].cfg_ShellCount = _value;
        if (LobbyList[PlayerList[_conn].lobbyID].playerCount > 1)
        {
            Target_SliderShellChange(LobbyList[PlayerList[_conn].lobbyID].playertwo, _value);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void Server_SliderTurnChange(NetworkConnection _conn, int _value)
    {
        LobbyList[PlayerList[_conn].lobbyID].cfg_TurnTimeoutIndex = _value;
        if (LobbyList[PlayerList[_conn].lobbyID].playerCount > 1)
        {
            Target_SliderTurnChange(LobbyList[PlayerList[_conn].lobbyID].playertwo, _value);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void Server_ToggleItemChange(NetworkConnection _conn, bool _enabled)
    {
        LobbyList[PlayerList[_conn].lobbyID].cfg_ItemsEnabled = _enabled;
        if (LobbyList[PlayerList[_conn].lobbyID].playerCount > 1)
        {
            Target_ToggleItemChange(LobbyList[PlayerList[_conn].lobbyID].playertwo, _enabled);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void Server_ToggleItemCapChange(NetworkConnection _conn, bool _enabled)
    {
        LobbyList[PlayerList[_conn].lobbyID].cfg_ItemTurnCapEnabled = _enabled;
        if (LobbyList[PlayerList[_conn].lobbyID].playerCount > 1)
        {
            Target_ToggleItemCapChange(LobbyList[PlayerList[_conn].lobbyID].playertwo, _enabled);
        }
    }
        
    [ServerRpc(RequireOwnership = false)]
    private void Server_ToggleShellChange(NetworkConnection _conn, bool _enabled)
    {
        LobbyList[PlayerList[_conn].lobbyID].cfg_StaticShellCountEnabled = _enabled;
        if (LobbyList[PlayerList[_conn].lobbyID].playerCount > 1)
        {
            Target_ToggleShellChange(LobbyList[PlayerList[_conn].lobbyID].playertwo, _enabled);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void Server_ToggleTurnChange(NetworkConnection _conn, bool _enabled)
    {
        LobbyList[PlayerList[_conn].lobbyID].cfg_TurnTimerEnabled = _enabled;
        if (LobbyList[PlayerList[_conn].lobbyID].playerCount > 1)
        {
            Target_ToggleTurnChange(LobbyList[PlayerList[_conn].lobbyID].playertwo, _enabled);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void Server_StartGame(NetworkConnection _conn)
    {
        Debug.Log("loading into new scene");
        NetworkConnection[] _players = new NetworkConnection[] { _conn, LobbyList[PlayerList[_conn].lobbyID].playertwo };
        
        // add to match condition for observers
        //MatchCondition.AddToMatch(0, _players);
        
        /*SceneLoadData servertest_sld = new SceneLoadData("SampleScene");
        servertest_sld.Options.AllowStacking = true;
        servertest_sld.Options.LocalPhysics = LocalPhysicsMode.Physics3D;
        servertest_sld.Params = new LoadParams() { ServerParams = new object[] { LobbyList[PlayerList[_conn].lobbyID] } };
        base.SceneManager.LoadConnectionScenes(servertest_sld);*/

        SceneLoadData _sld = new SceneLoadData("SampleScene");
        _sld.SceneLookupDatas[0].Handle = PlayerList[_conn].lobbyID;
        _sld.Options.AllowStacking = true;
        _sld.Options.LocalPhysics = LocalPhysicsMode.Physics3D;
        _sld.Params = new LoadParams() { ServerParams = new object[] { LobbyList[PlayerList[_conn].lobbyID] } };
        base.SceneManager.LoadConnectionScenes(_players, _sld);
        
        SceneUnloadData _sud = new SceneUnloadData("LobbyScene");
        _sud.Options.Mode = UnloadOptions.ServerUnloadMode.KeepUnused;
        base.SceneManager.UnloadConnectionScenes(_players, _sud);

        //StartCoroutine(Test_DelayClientLoad(_players));
    }
#endregion ServerRPCs

private IEnumerator Test_DelayClientLoad(NetworkConnection[] _players)
{
    yield return new WaitForSeconds(1f);
    Debug.Log("loading clients into new scene");
    SceneLoadData _sld = new SceneLoadData("SampleScene");
    //_sld.Options.AllowStacking = true;
    _sld.Options.LocalPhysics = LocalPhysicsMode.Physics3D;
    _sld.Params = new LoadParams() { ServerParams = new object[] { LobbyList[PlayerList[_players[0]].lobbyID] } };
    base.SceneManager.LoadConnectionScenes(_players, _sld);
        
    SceneUnloadData _sud = new SceneUnloadData("LobbyScene");
    _sud.Options.Mode = UnloadOptions.ServerUnloadMode.KeepUnused;
    base.SceneManager.UnloadConnectionScenes(_players, _sud);
}

#region ObserverRPCs
    [ObserversRpc]
    private void Observer_NudgeLobbyGrid()
    {
        StartCoroutine(DelayedNudgeLobbyGrid());
    }

    private IEnumerator DelayedNudgeLobbyGrid()
    {
        i_LobbyGrid.enabled = false;
        yield return new WaitForSeconds(0.5f);
        i_LobbyGrid.enabled = true;
    }
#endregion

#region TargetRPCs
    [TargetRpc]
    private void Target_CreateLobby(NetworkConnection _conn, int _lobbyID, string _playerOne, bool _cfgItemsEnabled, int _cfgItemCount, bool _cfgItemCapEnabled, int _cfgItemCap, bool _cfgStaticShells, int _cfgShells, bool _cfgTurnTimer, int _cfgTurnTimeIndex)
    {
        i_LobbyIDText.text = $"lobby id {_lobbyID}";
        
        i_PlayerOneText.text = _playerOne;
        i_PlayerTwoText.text = "";
            
        i_ItemToggle.interactable = true;
        i_ItemSlider.interactable = true;
        i_ItemCapToggle.interactable = true;
        i_ItemCapSlider.interactable = true;
        i_ShellToggle.interactable = true;
        i_ShellSlider.interactable = true;
        i_TurnToggle.interactable = true;
        i_TurnSlider.interactable = true;
        i_KickPlayerTwoButton.interactable = false;
        i_StartGameButton.interactable = false;
        
        i_ItemToggle.isOn = _cfgItemsEnabled;
        i_ItemSlider.value = _cfgItemCount;
        i_ItemCapToggle.isOn = _cfgItemCapEnabled;
        i_ItemCapSlider.value = _cfgItemCap;
        i_ShellToggle.isOn = _cfgStaticShells;
        i_ShellSlider.value = _cfgShells;
        i_TurnToggle.isOn = _cfgTurnTimer;
        i_TurnSlider.value = _cfgTurnTimeIndex;
        
        Client_IsLobbyOwner = true;
        LobbyUI.SetActive(true);
        
        AudioSystem.Lobby_Join();
    }

    [TargetRpc]
    private void Target_JoinLobby(NetworkConnection _conn, int _lobbyID, string _playerOne, string _playerTwo, bool _cfgItemsEnabled, int _cfgItemCount, bool _cfgItemCapEnabled, int _cfgItemCap, bool _cfgStaticShells, int _cfgShells, bool _cfgTurnTimer, int _cfgTurnTimeIndex)
    {
        i_LobbyIDText.text = $"lobby id {_lobbyID}";
        
        i_PlayerOneText.text = _playerOne;
        i_PlayerTwoText.text = _playerTwo;
        
        i_ItemToggle.interactable = false;
        i_ItemSlider.interactable = false;
        i_ItemCapToggle.interactable = false;
        i_ItemCapSlider.interactable = false;
        i_ShellToggle.interactable = false;
        i_ShellSlider.interactable = false;
        i_TurnToggle.interactable = false;
        i_TurnSlider.interactable = false;
        i_KickPlayerTwoButton.interactable = false;
        i_StartGameButton.interactable = false;
        
        i_ItemToggle.isOn = _cfgItemsEnabled;
        i_ItemSlider.value = _cfgItemCount;
        i_ItemCapToggle.isOn = _cfgItemCapEnabled;
        i_ItemCapSlider.value = _cfgItemCap;
        i_ShellToggle.isOn = _cfgStaticShells;
        i_ShellSlider.value = _cfgShells;
        i_TurnToggle.isOn = _cfgTurnTimer;
        i_TurnSlider.value = _cfgTurnTimeIndex;
        
        LobbyUI.SetActive(true);
        
        AudioSystem.Lobby_Join();
    }

    [TargetRpc]
    private void Target_PlayerTwoJoinedLobby(NetworkConnection _conn, string _playerTwo)
    {
        i_PlayerTwoText.text = _playerTwo;
        i_KickPlayerTwoButton.interactable = true;
        i_StartGameButton.interactable = true;
        
        AudioSystem.Lobby_Join();
    }

    [TargetRpc]
    private void Target_SliderItemChange(NetworkConnection _conn, int _value)
    {
        i_ItemSlider.value = _value;
        
        AudioSystem.Slider_Change();
    }
    
    [TargetRpc]
    private void Target_SliderItemCapChange(NetworkConnection _conn, int _value)
    {
        i_ItemCapSlider.value = _value;
        
        AudioSystem.Slider_Change();
    }
    
    [TargetRpc]
    private void Target_SliderShellChange(NetworkConnection _conn, int _value)
    {
        i_ShellSlider.value = _value;
        
        AudioSystem.Slider_Change();
    }
    
    [TargetRpc]
    private void Target_SliderTurnChange(NetworkConnection _conn, int _value)
    {
        i_TurnSlider.value = _value;
        
        AudioSystem.Slider_Change();
    }
    
    [TargetRpc]
    private void Target_ToggleItemChange(NetworkConnection _conn, bool _enabled)
    {
        i_ItemToggle.isOn = _enabled;
        
        AudioSystem.Slider_Change();
    }
    
    [TargetRpc]
    private void Target_ToggleItemCapChange(NetworkConnection _conn, bool _enabled)
    {
        i_ItemCapToggle.isOn = _enabled;
        
        AudioSystem.Slider_Change();
    }
    
    [TargetRpc]
    private void Target_ToggleShellChange(NetworkConnection _conn, bool _enabled)
    {
        i_ShellToggle.isOn = _enabled;
        
        AudioSystem.Slider_Change();
    }
    
    [TargetRpc]
    private void Target_ToggleTurnChange(NetworkConnection _conn, bool _enabled)
    {
        i_TurnToggle.isOn = _enabled;
        
        AudioSystem.Slider_Change();
    }
    
    [TargetRpc]
    private void Target_PlayerTwoLeftLobby(NetworkConnection _conn)
    {
        i_KickPlayerTwoButton.interactable = false;
        i_StartGameButton.interactable = false;
        i_PlayerTwoText.text = "";
        
        AudioSystem.Lobby_Leave();
    }
    
    [TargetRpc]
    private void Target_LeaveLobby(NetworkConnection _conn)
    {
        Client_IsLobbyOwner = false;
        LobbyUI.SetActive(false);
        
        AudioSystem.Lobby_Leave();
    }
#endregion TargetRPCs

#region Buttons
    public void Button_CreateLobby()
    {
        Server_CreateLobby(base.LocalConnection);
    }
    
    public void Button_LeaveLobby()
    {
        Server_LeaveLobby(base.LocalConnection);
    }

    public void Button_KickPlayerTwo()
    {
        Server_KickPlayerTwo(base.LocalConnection);
    }

    public void Button_StartGame()
    {
        Server_StartGame(base.LocalConnection);
    }
#endregion Buttons

#region Sliders
    public void Slider_Items_OnValueChanged(float _value)
    {
        if (!Client_IsLobbyOwner)
        {
            return;
        }
        
        Server_SliderItemChange(base.LocalConnection, Mathf.RoundToInt(_value));
        
        AudioSystem.Slider_Change();
    }
    
    public void Slider_ItemCap_OnValueChanged(float _value)
    {
        if (!Client_IsLobbyOwner)
        {
            return;
        }
        
        Server_SliderItemCapChange(base.LocalConnection, Mathf.RoundToInt(_value));
        
        AudioSystem.Slider_Change();
    }
    
    public void Slider_Shell_OnValueChanged(float _value)
    {
        if (!Client_IsLobbyOwner)
        {
            return;
        }
        
        Server_SliderShellChange(base.LocalConnection, Mathf.RoundToInt(_value));
        
        AudioSystem.Slider_Change();
    }
    
    public void Slider_Turn_OnValueChanged(float _value)
    {
        if (!Client_IsLobbyOwner)
        {
            return;
        }
        
        Server_SliderTurnChange(base.LocalConnection, Mathf.RoundToInt(_value));
        
        AudioSystem.Slider_Change();
    }
#endregion Sliders

#region Toggles
public void Toggle_Items_OnValueChanged(bool _enabled)
{
    if (!Client_IsLobbyOwner)
    {
        return;
    }
        
    Server_ToggleItemChange(base.LocalConnection, _enabled);
    
    AudioSystem.Slider_Change();
}

public void Toggle_ItemCap_OnValueChanged(bool _enabled)
{
    if (!Client_IsLobbyOwner)
    {
        return;
    }
        
    Server_ToggleItemCapChange(base.LocalConnection, _enabled);
    
    AudioSystem.Slider_Change();
}
    
public void Toggle_Shell_OnValueChanged(bool _enabled)
{
    if (!Client_IsLobbyOwner)
    {
        return;
    }
        
    Server_ToggleShellChange(base.LocalConnection, _enabled);
    
    AudioSystem.Slider_Change();
}
    
public void Toggle_Turn_OnValueChanged(bool _enabled)
{
    if (!Client_IsLobbyOwner)
    {
        return;
    }
        
    Server_ToggleTurnChange(base.LocalConnection, _enabled);
    
    AudioSystem.Slider_Change();
}
#endregion Toggles
}
