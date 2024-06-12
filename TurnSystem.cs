using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using TTVadumb.Lobby;
using UnityEngine;
using Random = UnityEngine.Random;

public class TurnSystem : NetworkBehaviour
{
    [SerializeField] private ServerSideHealth i_PlayerOneHealth, i_PlayerTwoHealth;
    [SerializeField] private PlayerSpawnSystem i_PlayerSpawnSystem;
    [SerializeField] private PlayerUiController i_PlayerUIController;
    [SerializeField] private PlayerItemSystem i_PlayerItemSystem;
    [SerializeField] private GameTurnController i_GameTurnController;
    [SerializeField] private GameObject i_ShellUI, i_TurnUI;
    [SerializeField] private TextMeshProUGUI i_ShellText;
    [SerializeField] private Transform i_Shotgun, i_PlayerOneSpawn, i_PlayerTwoSpawn; // temp
    
    private const float c_TurnTime = 30f;
    private float i_TurnTime = 0f;

    private bool i_PlayerOneTurn = false;

    private bool i_RoundStarted = false;
    private bool i_TurnOverEarly = false;

    private const int c_MinShellCount = 4;
    private const int c_MaxShellCount = 10;
    private const int c_RandomShellCount = 2; // the last x shells will be rng'd live or blank
    private List<bool> i_ShellList = new List<bool>();
    private bool i_BlankSelf = false;

    private int i_Damage = 1;
    
    
    // Lobby Config Options
    private bool cfg_ItemsEnabled;
    private bool cfg_SetShellsEnabled;
    private bool cfg_TurnTimerEnabled;
    private int cfg_ItemCount;
    private int cfg_ShellCount;
    private int cfg_TurnTime;
    
    public override void OnStartServer()
    {
        base.OnStartServer();

        if (!GameObject.FindWithTag("LobbyTransfer")) // DEBUG: can tweak cfg options here if testing directly from game scene
        {
            cfg_ItemsEnabled = true;
            cfg_ItemCount = 3;
            cfg_SetShellsEnabled = false;
            cfg_ShellCount = 0;
            cfg_TurnTimerEnabled = true;
            cfg_TurnTime = 30;
            return;
        }
        
        Lobby l_Lobby = GameObject.FindWithTag("LobbyTransfer").GetComponent<LobbyConfigTransfer>().p_Lobby;

        cfg_ItemsEnabled = l_Lobby.cfg_ItemsEnabled;
        cfg_ItemCount = l_Lobby.cfg_ItemCount;
        cfg_SetShellsEnabled = l_Lobby.cfg_StaticShellCountEnabled;
        cfg_ShellCount = l_Lobby.cfg_ShellCount;
        cfg_TurnTimerEnabled = l_Lobby.cfg_TurnTimerEnabled;
        switch (l_Lobby.cfg_TurnTimeoutIndex)
        {
            case 1:
                cfg_TurnTime = 10;
                break;
            case 2:
                cfg_TurnTime = 15;
                break;
            case 3:
                cfg_TurnTime = 20;
                break;
            case 4:
                cfg_TurnTime = 25;
                break;
            case 5:
                cfg_TurnTime = 30;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void StartTurnSystem(bool _playerOneStart)
    {
        Observer_NewRound();
        
        i_PlayerOneTurn = _playerOneStart;
        i_RoundStarted = false;
        i_TurnOverEarly = false;
        i_ShellList.Clear();
        
        // alternate blank/live
        bool l_Live = false;
        int l_LiveShells = 0;
        int l_ShellCount = 0;
        
        if (cfg_SetShellsEnabled)
        {
            l_ShellCount = cfg_ShellCount;
        }
        else
        {
            l_ShellCount = Random.Range(c_MinShellCount, c_MaxShellCount);
        }
        for (int i = 0; i < l_ShellCount - c_RandomShellCount; i++)
        {
            i_ShellList.Add(l_Live);
            if (l_Live)
            {
                l_LiveShells++;
            }
            l_Live = !l_Live;
        }
        
        // rng the last x shells
        for (int i = 0; i < c_RandomShellCount; i++)
        {
            l_Live = Random.value > 0.5f;
            i_ShellList.Add(l_Live);
            if (l_Live)
            {
                l_LiveShells++;
            }
        }
        
        // randomize order of the shells
        var count = i_ShellList.Count; // rip this shit straight from the forums fuck LINQ
        var last = count - 1;
        for (var i = 0; i < last; i++) 
        {
            var r = UnityEngine.Random.Range(i, count);
            (i_ShellList[i], i_ShellList[r]) = (i_ShellList[r], i_ShellList[i]);
        }
        //List<bool> l_RandomShellList = i_ShellList.OrderBy(x => Random.value).ToList(); // LINQ is garbage and I'm garbage for using it
        //i_ShellList = l_RandomShellList;
        
        // show the shell count
        Observer_ShowShells(l_LiveShells, l_ShellCount - l_LiveShells);
        
        StartCoroutine(TurnThread());
    }
    
    public bool GetPlayerOneTurn()
    {
        return i_PlayerOneTurn;
    }

    private IEnumerator TurnThread()
    {
        // on round start or reload show the shell count for 4 seconds, hide, then wait for 1 second
        if (!i_RoundStarted)
        {
            yield return new WaitForSeconds(0.5f);
            
            //Pan to P1 Health, Activate it
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerOne,i_PlayerUIController.i_p1CameraPositions[1].transform.position, i_PlayerUIController.i_p1CameraLookAt[1].transform.position);
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerTwo,i_PlayerUIController.i_p2CameraPositions[1].transform.position, i_PlayerUIController.i_p2CameraLookAt[1].transform.position );
            yield return new WaitForSeconds(2.5f);
            //i_PlayerUIController.activateGameObject(i_PlayerUIController.i_p1Health);
            i_PlayerUIController.Observer_ActivatePlayerOneHealth();
            yield return new WaitForSeconds(1f);
            //Pan to p2 health, Activate it
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerOne,i_PlayerUIController.i_p1CameraPositions[2].transform.position, i_PlayerUIController.i_p1CameraLookAt[2].transform.position);
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerTwo,i_PlayerUIController.i_p2CameraPositions[2].transform.position, i_PlayerUIController.i_p2CameraLookAt[2].transform.position );
            yield return new WaitForSeconds(2.5f);
            //i_PlayerUIController.activateGameObject(i_PlayerUIController.i_p2Health);
            i_PlayerUIController.Observer_ActivatePlayerTwoHealth();
            yield return new WaitForSeconds(1f);
            
            // Pan to Ammo show grey / blue, then remove them
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerOne,i_PlayerUIController.i_p1CameraPositions[3].transform.position, i_PlayerUIController.i_p1CameraLookAt[3].transform.position);
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerTwo,i_PlayerUIController.i_p2CameraPositions[3].transform.position, i_PlayerUIController.i_p2CameraLookAt[3].transform.position );
            yield return new WaitForSeconds(2.5f);
            i_PlayerUIController.activateGameObject(i_PlayerUIController.i_Ammo);
            yield return new WaitForSeconds(1f);
            for (int i = 0; i < i_ShellList.Count; i++)
            {
                Observer_AudioLoadShell();
                yield return new WaitForSeconds(0.2f);
            }
            yield return new WaitForSeconds(1f);
            Observer_AudioRackShotgun();
            yield return new WaitForSeconds(2f);
            Observer_HideShells();
            yield return new WaitForSeconds(1f);
            if (cfg_ItemsEnabled)
            {
                // Pan to Items 
                i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerOne,i_PlayerUIController.i_p1CameraPositions[4].transform.position, i_PlayerUIController.i_p1CameraLookAt[4].transform.position);
                i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerTwo,i_PlayerUIController.i_p2CameraPositions[4].transform.position, i_PlayerUIController.i_p2CameraLookAt[4].transform.position );
                yield return new WaitForSeconds(2.5f);
                // Spawn items
                i_PlayerItemSystem.SpawnItems(cfg_ItemCount);
                yield return new WaitForSeconds(2.5f);
            }
            // Default Look 
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerOne,i_PlayerUIController.i_p1CameraPositions[0].transform.position, i_PlayerUIController.i_p1CameraLookAt[0].transform.position);
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerTwo,i_PlayerUIController.i_p2CameraPositions[0].transform.position, i_PlayerUIController.i_p2CameraLookAt[0].transform.position );
            yield return new WaitForSeconds(2.5f);
            // Spawn Shotgun
            i_PlayerUIController.activateGameObject(i_PlayerUIController.i_Shotgun);
            // Activate Timer
            
            i_RoundStarted = true;
        }
        
        // reset turn vars
        i_Damage = 1;
        i_TurnTime = cfg_TurnTime;
        i_TurnOverEarly = false;
        i_PlayerItemSystem.ResetSpecialItems();
        i_Shotgun.LookAt(i_Shotgun.up);
        
        switch (i_PlayerOneTurn)
        {
            case true:
                Target_StartTurn(i_PlayerSpawnSystem.i_PlayerOne);
                break;
            case false:
                Target_StartTurn(i_PlayerSpawnSystem.i_PlayerTwo);
                break;
        }

        if (cfg_TurnTimerEnabled)
        {
            while (i_TurnTime > 0f && !i_TurnOverEarly)
            {
                yield return new WaitForSeconds(0.5f);
                i_TurnTime -= 0.5f;
            }
        }
        else
        {
            while (!i_TurnOverEarly)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
        

        if (i_TurnOverEarly)
        {
            // michael // TODO: observer shotgun blast
            yield return new WaitForSeconds(1f);
        }
        
        // stop the round if one of them is dead
        if (i_PlayerOneHealth.GetHealth() <= 0 || i_PlayerTwoHealth.GetHealth() <= 0)
        {
            Debug.Log("Round over someone is on the floor");
            i_PlayerItemSystem.EndGame();
            switch (i_PlayerOneTurn)
            {
                case true:
                    Target_EndTurn(i_PlayerSpawnSystem.i_PlayerOne);
                    break;
                case false:
                    Target_EndTurn(i_PlayerSpawnSystem.i_PlayerTwo);
                    break;
            }
            Observer_HideShells();
            yield return new WaitForSeconds(2f);
            i_PlayerOneHealth.EndGame();
            i_PlayerTwoHealth.EndGame();
            i_GameTurnController.Server_EndGame();
            i_PlayerUIController.Server_EndGame();
            yield break;
        }
        
        // reload shells at 0
        if (i_ShellList.Count == 0)
        {
            Debug.Log("Out of shells reloading shotgun");

            switch (i_PlayerOneTurn)
            {
                case true:
                    Target_EndTurn(i_PlayerSpawnSystem.i_PlayerOne);
                    break;
                case false:
                    Target_EndTurn(i_PlayerSpawnSystem.i_PlayerTwo);
                    break;
            }
            
            if (!i_BlankSelf)
            {
                i_PlayerOneTurn = !i_PlayerOneTurn;
            }
            
            StartTurnSystem(i_PlayerOneTurn);
            yield break;
        }
        
        // if they get timed out by the turn timer
        switch (i_PlayerOneTurn)
        {
            case true:
                Target_EndTurn(i_PlayerSpawnSystem.i_PlayerOne);
                break;
            case false:
                Target_EndTurn(i_PlayerSpawnSystem.i_PlayerTwo);
                break;
        }
        
        // if a player shot a blank at themselves don't change the turn
        if (!i_BlankSelf)
        {
            i_PlayerOneTurn = !i_PlayerOneTurn;
        }
        
        StartCoroutine(TurnThread());
    }

    #region Items
    private IEnumerator DelayHideShellText()
    {
        yield return new WaitForSeconds(2f);
        Observer_HideShells();
    }
    
    public void Server_EjectShell()
    {
        bool l_Live = i_ShellList[0];
        
        if (l_Live)
        {
            Debug.Log("(server) ejected a Live shell");
        }
        else
        {
            Debug.Log("(server) ejected a Blank shell");
        }
        
        i_ShellList.RemoveAt(0);
        
        Observer_ShowEjectShell(l_Live);
        StartCoroutine(DelayHideShellText());
        // TODO: replace text stuff with server spawning and ejecting shell
    }

    public void Server_InvertShell()
    {
        bool l_Live = i_ShellList[0];
        i_ShellList[0] = !l_Live;
        
        // TODO: observer inverter
    }

    public void Server_MagnifyShell()
    {
        bool l_Live = i_ShellList[0];
        
        if (i_PlayerOneTurn)
        {
            Target_MagnifyShell(i_PlayerSpawnSystem.i_PlayerOne, l_Live);
        }
        else
        {
            Target_MagnifyShell(i_PlayerSpawnSystem.i_PlayerTwo, l_Live);
        }
        
        StartCoroutine(DelayHideShellText());
        // TODO: replace text stuff
    }

    public void Server_DoubleDamage()
    {
        i_Damage++;
        
        // TODO: server cut shotgun off
    }

    public void Server_PhoneCall()
    {
        int l_Index = -1;
        if (i_ShellList.Count > 3)
        {
            l_Index = Random.Range(2, i_ShellList.Count);
        }
        else
        {
            l_Index = Random.Range(0, i_ShellList.Count);
        }

        bool l_Live = i_ShellList[l_Index];
        
        Debug.Log(l_Live ? $"(server) {l_Index} from now is a Live shell" : $"(server) {l_Index} from now is a Blank shell");
        
        if (i_PlayerOneTurn)
        {
            Target_PhoneCall(i_PlayerSpawnSystem.i_PlayerOne, l_Index, l_Live);
        }
        else
        {
            Target_PhoneCall(i_PlayerSpawnSystem.i_PlayerTwo, l_Index, l_Live);
        }
        
        StartCoroutine(DelayHideShellText());
        // TODO: replace text stuff with phone call
    }
    #endregion Items
    
    [ServerRpc(RequireOwnership = false)]
    private void Server_Shoot(NetworkConnection _conn, bool _self)
    {
        // stop players trying to shoot when it's not their turn 
        if ((i_PlayerOneTurn && _conn != i_PlayerSpawnSystem.i_PlayerOne) || (!i_PlayerOneTurn && _conn != i_PlayerSpawnSystem.i_PlayerTwo))
        {
            return;
        }
        
        i_BlankSelf = false;
        bool l_Live = i_ShellList[0]; // first in first out
        switch (i_PlayerOneTurn)
        {
            case true:
                if (l_Live)
                {
                    if (!_self)
                    {
                        i_PlayerTwoHealth.Damage(i_Damage);
                        Debug.Log("Player One splattered Player Two");
                    }
                    else
                    {
                        i_PlayerOneHealth.Damage(i_Damage);
                        Debug.Log("Player One splattered themselves");
                    }
                }
                else
                {
                    if (!_self)
                    {
                        Debug.Log("Player One shot a blank at Player Two");
                    }
                    else
                    {
                        i_BlankSelf = true;
                        Debug.Log("Player One shot a blank at themselves");
                    }
                }

                i_Shotgun.LookAt(_self ? i_PlayerTwoSpawn : i_PlayerOneSpawn);
                break;
            case false:
                if (l_Live)
                {
                    if (!_self)
                    {
                        i_PlayerOneHealth.Damage(i_Damage);
                        Debug.Log("Player Two splattered Player One");
                    }
                    else
                    {
                        i_PlayerTwoHealth.Damage(i_Damage);
                        Debug.Log("Player Two splattered themselves");
                    }
                }
                else
                {
                    if (!_self)
                    {
                        Debug.Log("Player Two shot a blank at Player One");
                    }
                    else
                    {
                        i_BlankSelf = true;
                        Debug.Log("Player Two shot a blank at themselves");
                    }
                }
                
                i_Shotgun.LookAt(_self ? i_PlayerOneSpawn : i_PlayerTwoSpawn);
                break;
        }
        i_ShellList.RemoveAt(0);
        i_TurnOverEarly = true;

        if (l_Live)
        {
            Observer_AudioFireLive();
        }
        else
        {
            Observer_AudioFireBlank();
        }
    }

    [TargetRpc]
    private void Target_StartTurn(NetworkConnection _conn)
    {
        i_TurnUI.SetActive(true);
    }
    
    [TargetRpc]
    private void Target_EndTurn(NetworkConnection _conn)
    {
        i_TurnUI.SetActive(false);
    }

    [TargetRpc]
    private void Target_MagnifyShell(NetworkConnection _conn, bool _live)
    {
        i_ShellUI.SetActive(true);
        if (_live)
        {
            i_ShellText.text = "<color=red>Live</color>";
            Debug.Log("(client) next is a Live shell");
        }
        else
        {
            i_ShellText.text = "<color=blue>Blank</color>";
            Debug.Log("(client) next is a Blank shell");
        }
    }
    
    [TargetRpc]
    private void Target_PhoneCall(NetworkConnection _conn, int _index, bool _live)
    {
        i_ShellUI.SetActive(true);
        if (_live)
        {
            i_ShellText.text = $"{_index} from now is <color=red>Live</color>";
            Debug.Log($"(client) {_index} from now is a Live shell");
        }
        else
        {
            i_ShellText.text = $"{_index} from now is <color=blue>Blank</color>";
            Debug.Log($"(client) {_index} from now is a Blank shell");
        }
    }
    
    [ObserversRpc]
    private void Observer_ShowShells(int _live, int _blank)
    {
        i_ShellUI.SetActive(true);
        i_ShellText.text = $"<color=red>{_live}</color> / <color=blue>{_blank}</color>";
    }

    [ObserversRpc]
    private void Observer_HideShells()
    {
        i_ShellText.text = "";
        i_ShellUI.SetActive(false);
    }

    [ObserversRpc]
    private void Observer_ShowEjectShell(bool _live)
    {
        i_ShellUI.SetActive(true);
        if (_live)
        {
            i_ShellText.text = "ejected <color=red>live</color> shell";
            Debug.Log("(client) ejected a Live shell");
        }
        else
        {
            i_ShellText.text = "ejected <color=blue>blank</color> shell";
            Debug.Log("(client) ejected a Blank shell");
        }
    }

#region Audio
    [ObserversRpc]
    private void Observer_NewRound()
    {
        AudioSystem.Game_NewRound();
    }
    [ObserversRpc]
    private void Observer_AudioFireBlank()
    {
        AudioSystem.Game_Shotgun_FireBlank();
    }
    [ObserversRpc]
    private void Observer_AudioFireLive()
    {
        AudioSystem.Game_Shotgun_FireLive();
    }
    [ObserversRpc]
    private void Observer_AudioLoadShell()
    {
        AudioSystem.Game_Shotgun_LoadShell();
    }
    [ObserversRpc]
    private void Observer_AudioRackShotgun()
    {
        AudioSystem.Game_Shotgun_Rack();
    }
#endregion Audio
    

    public void Button_Shoot(bool _self)
    {
        i_TurnUI.SetActive(false);
        Server_Shoot(base.LocalConnection, _self);
    }
}
