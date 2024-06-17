using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Component.Observing;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using TMPro;
using TTVadumb.Lobby;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public class TurnSystem : NetworkBehaviour
{
    [SerializeField] private ServerSideHealth i_PlayerOneHealth, i_PlayerTwoHealth;
    [SerializeField] private PlayerSpawnSystem i_PlayerSpawnSystem;
    [SerializeField] private PlayerUiController i_PlayerUIController;
    [SerializeField] private PlayerItemSystem i_PlayerItemSystem;
    [SerializeField] private GameTurnController i_GameTurnController;
    [SerializeField] private GameObject i_ShellUI, i_TurnUI, i_ItemUI;
    [SerializeField] private TextMeshProUGUI i_ShellText;
    [SerializeField] private Transform i_Shotgun, i_ShotgunPump, i_PlayerOneSpawn, i_PlayerTwoSpawn, i_ShotgunEjectPort; // temp
    [SerializeField] private NetworkObject i_ShellLivePrefab, i_ShellBlankPrefab;
    [SerializeField] private Transform[] i_ShellSpawnPositions;
    [SerializeField] private GameObject i_PlayerOneVictoryLightOne, i_PlayerOneVictoryLightTwo, i_PlayerTwoVictoryLightOne, i_PlayerTwoVictoryLightTwo;
    [SerializeField] private TextMeshProUGUI i_ItemTurnUseRemainingText;
    
    private const float c_TurnTime = 30f;
    private float i_TurnTime = 0f;

    private bool i_PlayerOneTurn = false;

    private bool i_RoundStarted = false;
    private bool i_TurnOverEarly = false;
    private bool i_ItemPause = false;

    private const int c_MinShellCount = 4;
    private const int c_MaxShellCount = 10;
    private const int c_RandomShellCount = 2; // the last x shells will be rng'd live or blank
    private List<bool> i_ShellList = new List<bool>();
    private bool i_BlankSelf = false;
    private int i_LiveShells = 0; // not updated in realtime, only used for spawning visual shells
    private bool i_LastShellLive = false;
    
    private int i_Damage = 1;

    private PhysicsRaycaster i_ClientCameraRaycaster;
    private bool i_RaycasterSetup = false;
    
    // Lobby Config Options
    private bool cfg_ItemsEnabled;
    private bool cfg_ItemTurnCapEnabled;
    private bool cfg_SetShellsEnabled;
    private bool cfg_TurnTimerEnabled;
    private int cfg_ItemCount;
    private int cfg_ItemTurnCap;
    private int cfg_ShellCount;
    private int cfg_TurnTime;

    private void GrabLobbyParams(SceneLoadEndEventArgs _args)
    {
        Lobby l_Lobby = (Lobby)_args.QueueData.SceneLoadData.Params.ServerParams[0];
        
        //MatchCondition.AddToMatch(0, new NetworkConnection[]{l_Lobby.owner, l_Lobby.playertwo}, NetworkManager);
        cfg_ItemsEnabled = l_Lobby.cfg_ItemsEnabled;
        cfg_ItemCount = l_Lobby.cfg_ItemCount;
        cfg_ItemTurnCapEnabled = l_Lobby.cfg_ItemTurnCapEnabled;
        cfg_ItemTurnCap = l_Lobby.cfg_ItemTurnCap;
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
        Debug.Log("grabbed lobby cfg parameters");
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        // add/remove scripting symbol LOBBY_SYSTEM in build settings
    #if LOBBY_SYSTEM
        SceneManager.OnLoadEnd += GrabLobbyParams;
    #else
        // DEBUG: can tweak cfg options here if testing directly from game scene
        cfg_ItemsEnabled = true;
        cfg_ItemCount = 8;
        cfg_ItemTurnCapEnabled = true;
        cfg_ItemTurnCap = 3;
        cfg_SetShellsEnabled = false;
        cfg_ShellCount = 0;
        cfg_TurnTimerEnabled = true;
        cfg_TurnTime = 30;
    #endif
    }

    public void StartTurnSystem(bool _playerOneStart)
    {
        if (!i_RaycasterSetup)
        {
            Observer_SetupPhysicsRaycaster();
            i_RaycasterSetup = true;
        }
        Observer_DisablePhysicsRaycaster();
        
        i_PlayerOneTurn = _playerOneStart;
        i_RoundStarted = false;
        i_TurnOverEarly = false;
        i_LiveShells = 0;
        i_ShellList.Clear();

        i_Shotgun.localEulerAngles = new Vector3(0f, 45f, 90f);
        
        // alternate blank/live
        bool l_Live = false;
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
                i_LiveShells++;
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
                i_LiveShells++;
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
        //Observer_ShowShells(i_LiveShells, l_ShellCount - i_LiveShells);
        
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
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerTwo,i_PlayerUIController.i_p2CameraPositions[1].transform.position, i_PlayerUIController.i_p2CameraLookAt[1].transform.position);
            yield return new WaitForSeconds(2.5f);
            //i_PlayerUIController.activateGameObject(i_PlayerUIController.i_p1Health);
            Observer_NewRound();
            i_PlayerUIController.Observer_ActivatePlayerOneHealth(i_PlayerOneHealth.GetHealth());
            yield return new WaitForSeconds(1f);
            
            //Pan to p2 health, Activate it
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerOne,i_PlayerUIController.i_p1CameraPositions[2].transform.position, i_PlayerUIController.i_p1CameraLookAt[2].transform.position);
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerTwo,i_PlayerUIController.i_p2CameraPositions[2].transform.position, i_PlayerUIController.i_p2CameraLookAt[2].transform.position);
            yield return new WaitForSeconds(2.5f);
            //i_PlayerUIController.activateGameObject(i_PlayerUIController.i_p2Health);
            Observer_NewRound();
            i_PlayerUIController.Observer_ActivatePlayerTwoHealth(i_PlayerTwoHealth.GetHealth());
            yield return new WaitForSeconds(1f);
            
            // Pan to Ammo show red / blue, then remove them
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerOne,i_PlayerUIController.i_p1CameraPositions[3].transform.position, i_PlayerUIController.i_p1CameraLookAt[3].transform.position);
            i_PlayerUIController.StartCameraMovement(i_PlayerSpawnSystem.i_PlayerTwo,i_PlayerUIController.i_p2CameraPositions[3].transform.position, i_PlayerUIController.i_p2CameraLookAt[3].transform.position);
            yield return new WaitForSeconds(2.5f);
            StartCoroutine(SpawnShells());
            //i_PlayerUIController.activateGameObject(i_PlayerUIController.i_Ammo);
            yield return new WaitForSeconds(i_ShellList.Count * 0.4f + 1f);
            
            // load shells
            i_Shotgun.localEulerAngles = new Vector3(0f, -45f, -90f);
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < i_ShellList.Count; i++)
            {
                Observer_AudioLoadShell();
                yield return new WaitForSeconds(0.2f);
            }
            yield return new WaitForSeconds(1f);
            Observer_RackShotgun();
            //StartCoroutine(ShotgunPump());
            yield return new WaitForSeconds(2.5f);
            //Observer_HideShells();
            //yield return new WaitForSeconds(1f);
            
            // goto items
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
            //i_PlayerUIController.activateGameObject(i_PlayerUIController.i_Shotgun); // spawning shotgun after it was already loaded
            // Activate Timer
            
            i_RoundStarted = true;
        }
        
        // reset turn vars
        i_Damage = 1;
        i_TurnTime = cfg_TurnTime;
        i_TurnOverEarly = false;
        i_PlayerItemSystem.ResetSpecialItems();
        i_Shotgun.localEulerAngles = i_PlayerOneTurn ? new Vector3(0f, -45f, -90f) : new Vector3(0f, 135f, -90f);
        
        Observer_EnablePhysicsRaycaster();
        
        switch (i_PlayerOneTurn)
        {
            case true:
                Target_ShowTurnUI(i_PlayerSpawnSystem.i_PlayerOne, cfg_ItemTurnCapEnabled, cfg_ItemTurnCap - i_PlayerItemSystem.GetItemsUsedThisTurn());
                break;
            case false:
                Target_ShowTurnUI(i_PlayerSpawnSystem.i_PlayerTwo, cfg_ItemTurnCapEnabled, cfg_ItemTurnCap - i_PlayerItemSystem.GetItemsUsedThisTurn());
                break;
        }

        if (cfg_TurnTimerEnabled)
        {
            while (i_TurnTime > 0f && !i_TurnOverEarly)
            {
                while (i_ItemPause)
                {
                    yield return new WaitForSeconds(0.5f);
                    if (i_TurnOverEarly)
                    {
                        i_ItemPause = false;
                    }
                }

                if (i_PlayerOneHealth.GetHealth() <= 0 || i_PlayerTwoHealth.GetHealth() <= 0)
                {
                    break;
                }
                yield return new WaitForSeconds(0.5f);
                i_TurnTime -= 0.5f;
            }
        }
        else
        {
            while (!i_TurnOverEarly)
            {
                while (i_ItemPause)
                {
                    yield return new WaitForSeconds(0.5f);
                    if (i_TurnOverEarly)
                    {
                        i_ItemPause = false;
                    }
                }
                
                if (i_PlayerOneHealth.GetHealth() <= 0 || i_PlayerTwoHealth.GetHealth() <= 0)
                {
                    break;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        Observer_DisablePhysicsRaycaster();
        if (i_TurnOverEarly)
        {
            yield return new WaitForSeconds(1.5f);
            Observer_RackShotgun();
            //Observer_AudioRackShotgun();
            //Observer_VisualRackShotgun();
            //StartCoroutine(ShotgunPump());
            StartCoroutine(EjectShellVisual(i_LastShellLive));
            yield return new WaitForSeconds(3.5f);
            i_Shotgun.localEulerAngles = new Vector3(0f, 45f, 90f);
        }
        
        // stop the round if one of them is dead
        if (i_PlayerOneHealth.GetHealth() <= 0 || i_PlayerTwoHealth.GetHealth() <= 0)
        {
            Debug.Log("Round over someone is on the floor");
            i_PlayerItemSystem.EndGame();
            switch (i_PlayerOneTurn)
            {
                case true:
                    Target_HideTurnUI(i_PlayerSpawnSystem.i_PlayerOne, cfg_ItemTurnCapEnabled);
                    break;
                case false:
                    Target_HideTurnUI(i_PlayerSpawnSystem.i_PlayerTwo, cfg_ItemTurnCapEnabled);
                    break;
            }
            Observer_HideShells();
            yield return new WaitForSeconds(2.5f);
            StartCoroutine(GameOverVisual());
            Observer_StartClientVictoryVisual(i_PlayerOneHealth.GetHealth() > i_PlayerTwoHealth.GetHealth());
            yield return new WaitForSeconds(5f);
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
                    Target_HideTurnUI(i_PlayerSpawnSystem.i_PlayerOne, cfg_ItemTurnCapEnabled);
                    break;
                case false:
                    Target_HideTurnUI(i_PlayerSpawnSystem.i_PlayerTwo, cfg_ItemTurnCapEnabled);
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
                Target_HideTurnUI(i_PlayerSpawnSystem.i_PlayerOne, cfg_ItemTurnCapEnabled);
                break;
            case false:
                Target_HideTurnUI(i_PlayerSpawnSystem.i_PlayerTwo, cfg_ItemTurnCapEnabled);
                break;
        }
        
        // if a player shot a blank at themselves don't change the turn
        if (!i_BlankSelf)
        {
            i_PlayerOneTurn = !i_PlayerOneTurn;
        }
        
        StartCoroutine(TurnThread());
    }

    private IEnumerator ShotgunPump()
    {
        Vector3 l_ShotgunPumpLocalPosition = i_ShotgunPump.localPosition;
        while (i_ShotgunPump.localPosition.z < -1.25f)
        {
            l_ShotgunPumpLocalPosition.z += 0.01f;
            i_ShotgunPump.localPosition = l_ShotgunPumpLocalPosition;
            yield return new WaitForSeconds(0.005f);
        }
        while (i_ShotgunPump.localPosition.z > -1.91f)
        {
            l_ShotgunPumpLocalPosition.z -= 0.01f;
            i_ShotgunPump.localPosition = l_ShotgunPumpLocalPosition;
            yield return new WaitForSeconds(0.005f);
        }
    }
    
    private IEnumerator SpawnShells()
    {
        List<NetworkObject> l_Shells = new List<NetworkObject>();
        for (int i = 0; i < i_LiveShells; i++)
        {
            NetworkObject l_Shell = Instantiate(i_ShellLivePrefab, i_ShellSpawnPositions[i].position, Quaternion.identity);
            ServerManager.Spawn(l_Shell, null, gameObject.scene);
            l_Shells.Add(l_Shell);
            Observer_AudioSpawnShell();
            yield return new WaitForSeconds(0.2f);
        }
        for (int i = i_LiveShells; i < i_ShellList.Count; i++)
        {
            NetworkObject l_Shell = Instantiate(i_ShellBlankPrefab, i_ShellSpawnPositions[i].position, Quaternion.identity);
            ServerManager.Spawn(l_Shell, null, gameObject.scene);
            l_Shells.Add(l_Shell);
            Observer_AudioSpawnShell();
            yield return new WaitForSeconds(0.2f);
        }
        
        foreach (NetworkObject _shell in l_Shells)
        {
            ServerManager.Despawn(_shell);
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator EjectShellVisual(bool _live)
    {
        NetworkObject l_Shell = default;

        switch (_live)
        {
            case true:
                l_Shell = Instantiate(i_ShellLivePrefab, i_ShotgunEjectPort.position, i_ShotgunEjectPort.rotation);
                break;
            case false:
                l_Shell = Instantiate(i_ShellBlankPrefab, i_ShotgunEjectPort.position, i_ShotgunEjectPort.rotation);
                break;
        }
        ServerManager.Spawn(l_Shell, null, gameObject.scene);
        
        Observer_EjectShell(l_Shell.gameObject);
        
        yield return new WaitForSeconds(2f);
        
        ServerManager.Despawn(l_Shell);
    }

    private IEnumerator GameOverVisual()
    {
        Observer_AudioGameOverStart();
        yield return new WaitForSeconds(2f);
        Observer_AudioGameOverEnd();
    }

    #region Items
    public bool GetItemTurnCapEnabled()
    {
        return cfg_ItemTurnCapEnabled;
    }
    
    public int GetMaxItemUsePerTurn()
    {
        return cfg_ItemTurnCap;
    }
    
    public void Server_UseItem(string _item)
    {
        i_ItemPause = true;
        Observer_DisablePhysicsRaycaster();
        Target_HideTurnUI(i_PlayerOneTurn ? i_PlayerSpawnSystem.i_PlayerOne : i_PlayerSpawnSystem.i_PlayerTwo, cfg_ItemTurnCapEnabled);
        
        StartCoroutine(TimedUseItem(4f));
        
        switch (_item)
        {
            case "Item_Smokes":
                break;
            case "Item_Beer":
                break;
            case "Item_Pills":
                break;
            case "Item_Inverter":
                break;
            case "Item_Magnifying":
                break;
            case "Item_Saw":
                break;
            case "Item_Phone":
                break;
        }
    }

    private IEnumerator TimedUseItem(float _duration)
    {
        yield return new WaitForSeconds(_duration);
        Target_ShowTurnUI(i_PlayerOneTurn ? i_PlayerSpawnSystem.i_PlayerOne : i_PlayerSpawnSystem.i_PlayerTwo, cfg_ItemTurnCapEnabled, cfg_ItemTurnCap - i_PlayerItemSystem.GetItemsUsedThisTurn());
        Observer_EnablePhysicsRaycaster();
        i_ItemPause = false;
    }
    
    private IEnumerator DelayHideShellText()
    {
        yield return new WaitForSeconds(2f);
        Observer_HideShells();
    }
    
    public void Server_EjectShell() // beer
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
        
        //Observer_ShowEjectShell(l_Live);
        //StartCoroutine(DelayHideShellText());
        Observer_RackShotgun();
        //StartCoroutine(ShotgunPump());
        StartCoroutine(EjectShellVisual(l_Live));
        
        i_LastShellLive = l_Live;
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

    public void Server_DoubleDamage() // saw
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

        i_LastShellLive = l_Live;
    }

    [TargetRpc]
    private void Target_ShowTurnUI(NetworkConnection _conn, bool _itemCapEnabled, int _itemCap)
    {
        i_TurnUI.SetActive(true);
        if (_itemCapEnabled)
        {
            i_ItemTurnUseRemainingText.text = $"item uses: {_itemCap}";
            i_ItemUI.SetActive(true);
        }
    }
    
    [TargetRpc]
    private void Target_HideTurnUI(NetworkConnection _conn, bool _itemCapEnabled)
    {
        i_TurnUI.SetActive(false);
        if (_itemCapEnabled)
        {
            i_ItemUI.SetActive(false);
        }
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
    
    [ObserversRpc(ExcludeServer = true)]
    private void Observer_SetupPhysicsRaycaster()
    {
        i_ClientCameraRaycaster = Camera.main.GetComponent<PhysicsRaycaster>(); // fuck me for doing it this way
    }

    [ObserversRpc(ExcludeServer = true)]
    private void Observer_DisablePhysicsRaycaster()
    {
        i_ClientCameraRaycaster.enabled = false;
    }
    
    [ObserversRpc(ExcludeServer = true)]
    private void Observer_EnablePhysicsRaycaster()
    {
        i_ClientCameraRaycaster.enabled = true;
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
    
    [ObserversRpc]
    private void Observer_StartClientVictoryVisual(bool _playerOneVictory)
    {
        StartCoroutine(_playerOneVictory ? ClientPlayerOneVictoryVisual() : ClientPlayerTwoVictoryVisual());
    }

    private IEnumerator ClientPlayerOneVictoryVisual()
    {
        // light 1 on light 2 off
        i_PlayerOneVictoryLightOne.SetActive(true);
        yield return new WaitForSeconds(1f);
        // light 1 off light 2 on
        i_PlayerOneVictoryLightOne.SetActive(false);
        i_PlayerOneVictoryLightTwo.SetActive(true);
        yield return new WaitForSeconds(1f);
        // light 1 on light 2 off
        i_PlayerOneVictoryLightOne.SetActive(true);
        i_PlayerOneVictoryLightTwo.SetActive(false);
        yield return new WaitForSeconds(1f);
        // light 1 off light 2 on
        i_PlayerOneVictoryLightOne.SetActive(false);
        i_PlayerOneVictoryLightTwo.SetActive(true);
        yield return new WaitForSeconds(1f);
        // light 1 off light 2 off
        i_PlayerOneVictoryLightTwo.SetActive(false);
    }
    
    private IEnumerator ClientPlayerTwoVictoryVisual()
    {
        // light 1 on light 2 off
        i_PlayerTwoVictoryLightOne.SetActive(true);
        yield return new WaitForSeconds(1f);
        // light 1 off light 2 on
        i_PlayerTwoVictoryLightOne.SetActive(false);
        i_PlayerTwoVictoryLightTwo.SetActive(true);
        yield return new WaitForSeconds(1f);
        // light 1 on light 2 off
        i_PlayerTwoVictoryLightOne.SetActive(true);
        i_PlayerTwoVictoryLightTwo.SetActive(false);
        yield return new WaitForSeconds(1f);
        // light 1 off light 2 on
        i_PlayerTwoVictoryLightOne.SetActive(false);
        i_PlayerTwoVictoryLightTwo.SetActive(true);
        yield return new WaitForSeconds(1f);
        // light 1 off light 2 off
        i_PlayerTwoVictoryLightTwo.SetActive(false);
    }

    [ObserversRpc]
    private void Observer_RackShotgun()
    {
        AudioSystem.Game_Shotgun_Rack();
        StartCoroutine(VisualRackShotgun());
    }

    private IEnumerator VisualRackShotgun()
    {
        Vector3 l_ShotgunPumpLocalPosition = i_ShotgunPump.localPosition;
        while (i_ShotgunPump.localPosition.z < -1.25f)
        {
            l_ShotgunPumpLocalPosition.z += 0.01f;
            i_ShotgunPump.localPosition = l_ShotgunPumpLocalPosition;
            yield return new WaitForSeconds(0.01f);
        }
        while (i_ShotgunPump.localPosition.z > -1.91f)
        {
            l_ShotgunPumpLocalPosition.z -= 0.01f;
            i_ShotgunPump.localPosition = l_ShotgunPumpLocalPosition;
            yield return new WaitForSeconds(0.01f);
        }
    }
    
    [ObserversRpc]
    private void Observer_EjectShell(GameObject _shell)
    {
        StartCoroutine(VisualEjectShell(_shell));
    }

    private IEnumerator VisualEjectShell(GameObject _shell)
    {
        yield return new WaitForSeconds(0.5f);
        Debug.LogWarning("this may error, not an issue", this);
        Vector3 l_Direction = -i_ShotgunEjectPort.transform.right; 
        float l_Timer = 2.5f;
        while (l_Timer > 0f)
        {
            if (_shell == null)
            {
                break;
            }
            _shell.transform.position += (l_Direction * 0.025f);
            yield return new WaitForSeconds(0.025f);
            l_Timer -= 0.025f;
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
    /*[ObserversRpc] // combined into Observer_RackShotgun()
    private void Observer_AudioRackShotgun()
    {
        AudioSystem.Game_Shotgun_Rack();
    }*/
    [ObserversRpc]
    private void Observer_AudioGameOverStart()
    {
        AudioSystem.Game_Over_Start();
    }
    [ObserversRpc]
    private void Observer_AudioGameOverEnd()
    {
        AudioSystem.Game_Over_End();
    }
    [ObserversRpc]
    private void Observer_AudioSpawnShell()
    {
        AudioSystem.Game_Shell_Spawn();
    }
#endregion Audio
    

    public void Button_Shoot(bool _self)
    {
        i_TurnUI.SetActive(false);
        i_ItemUI.SetActive(false);
        Server_Shoot(base.LocalConnection, _self);
    }
}
