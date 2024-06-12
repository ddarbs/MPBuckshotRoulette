using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyHandler : NetworkBehaviour // INFO: this handles the Browser UI, LobbySystem handles interactions with the Lobby UI 
{
    [SerializeField] private TextMeshProUGUI i_CountText, i_NamesText, i_StatusText;
    [SerializeField] private Button i_LobbyButton;
    [SerializeField] private Image i_LobbyBackground;
    [SerializeField] private Color i_Red, i_Green;
    
    private string i_OwnerName;
    private LobbySystem i_LobbySystem;
    private int i_LobbyID;

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        Server_RefreshLobbyInfo(base.LocalConnection);
    }
    [ServerRpc(RequireOwnership = false)]
    private void Server_RefreshLobbyInfo(NetworkConnection _conn)
    {
        Target_RefreshLobbyInfo(_conn, i_CountText.text, i_NamesText.text, i_StatusText.text);
    }
    [TargetRpc]
    private void Target_RefreshLobbyInfo(NetworkConnection _conn, string _count, string _names, string _status)
    {
        i_CountText.text = _count;
        i_NamesText.text = _names;
        i_StatusText.text = _status;
        if (_count == "2/2")
        {
            i_LobbyButton.interactable = false;
            i_LobbyBackground.color = i_Red;
        }
    }

    public void Setup(string _owner, LobbySystem _lobbySystem, int _lobbyID)
    {
        i_OwnerName = _owner;
        i_LobbySystem = _lobbySystem;
        i_LobbyID = _lobbyID;
    }
    
    public void OnLobbyStart()
    {
        Observer_OnLobbyStart(i_OwnerName);
    }
    [ObserversRpc(RunLocally = true)]
    private void Observer_OnLobbyStart(string _ownerName)
    {
        i_NamesText.text = $"{_ownerName}\n";
    }
    
    public void OnLobbyJoin(string _playerTwoName)
    {
        Observer_OnLobbyJoin(_playerTwoName);
    }
    [ObserversRpc(RunLocally = true)]
    private void Observer_OnLobbyJoin(string _playerTwoName)
    {
        i_LobbyButton.interactable = false;
        i_LobbyBackground.color = i_Red;
        i_CountText.text = "2/2";
        i_NamesText.text += _playerTwoName;
    }
    
    public void OnLobbyLeave()
    {
        Observer_OnLobbyLeave(i_OwnerName);
    }
    [ObserversRpc(RunLocally = true)]
    private void Observer_OnLobbyLeave(string _ownerName)
    {
        i_LobbyButton.interactable = true;
        i_LobbyBackground.color = i_Green;
        i_CountText.text = "1/2";
        i_NamesText.text = $"{_ownerName}\n";
    }

    [ServerRpc(RequireOwnership = false)]
    private void Server_ClickLobby(NetworkConnection _conn)
    {
        i_LobbySystem.Server_JoinLobby(_conn, i_LobbyID);
    }

    public void Button_ClickLobby()
    {
        Server_ClickLobby(base.LocalConnection);
    }

    public void AudioSystem_Hover()
    {
        AudioSystem.Button_Hover();
    }
    
    public void AudioSystem_ClickDown()
    {
        AudioSystem.Button_ClickDown();
    }
    
    public void AudioSystem_ClickUp()
    {
        AudioSystem.Button_ClickUp();
    }
}
