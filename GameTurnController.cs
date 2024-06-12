using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class GameTurnController : NetworkBehaviour
{
    private bool i_Player1Ready = false;
    private bool i_Player2Ready = false;

    [SerializeField] private PlayerItemSystem i_PlayerItemSystem;
    [SerializeField] private PlayerSpawnSystem i_PlayerSpawnSystem;
    [SerializeField] private PlayerUiController i_PlayerUIController;
    [SerializeField] private GameObject[] ShellController = new GameObject[10];
    
    //needs to check that players are in the lobby
    //needs to check and implement lobby settings, items, player timer, number of lives
    //ready button for players
    [ServerRpc (RequireOwnership = false)] 
    private void ServerPlayerReadyUp(NetworkConnection Conn)
    {
        if (Conn == i_PlayerSpawnSystem.i_PlayerOne)
        {
            i_Player1Ready = true;
            if (i_Player2Ready == false)
            {
                i_PlayerUIController.SendPlayersUpdateMessage("Player 1 is Ready (1/2)");
            }
            Debug.Log("this is player 1");
            
        }
        else if (Conn == i_PlayerSpawnSystem.i_PlayerTwo)
        {
            i_Player2Ready = true;
            if (i_Player1Ready == false)
            {
                i_PlayerUIController.SendPlayersUpdateMessage("Player 2 is Ready (1/2)");
            }
            Debug.Log("this is player 2");
            
        }

        if (i_Player1Ready == true && i_Player2Ready == true)
        {
            i_PlayerUIController.DiceRollUi();
            i_PlayerUIController.disablePlayerUpdateMessage();
            StartCoroutine(this.GetComponent<DiceRoll>().DiceRolling());
        }
    }

    public void PlayerReadyUp()
    {
        ServerPlayerReadyUp(base.LocalConnection);
    }

    public void Server_EndGame()
    {
        i_Player1Ready = false;
        i_Player2Ready = false;
    }
    
    private void Update()
    {
        
    }
    
    //roll the dice for players and begin choosing who goes first
    //change the UI
    
    //show the shells however many
    //control the timers for each players turn, default is shoot self other with shotgun
    //prompt player to use items
    //game ends when player dies
}
