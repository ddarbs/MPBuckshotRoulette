using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using UnityEngine;

namespace TTVadumb
{
    namespace Lobby
    {
        /// <summary>
        /// 0 - Browsing 
        /// 1 - in a Lobby
        /// 2 - in a Game
        /// </summary>
        public enum PlayerState
        {
            Browsing,
            Lobby,
            Game
        }    
    
        public class LobbyPlayer
        {
            public string name;
            public PlayerState status;
            public int lobbyID;

            public LobbyPlayer(string _name, PlayerState _status = PlayerState.Browsing)
            {
                name = _name;
                status = _status;
                lobbyID = -1;
            }
        }

        public class Lobby
        {
            public GameObject handler;
            public int id;
            public NetworkConnection owner;
            public NetworkConnection playertwo;
            public int playerCount;
            public bool inGame;
            public bool cfg_ItemsEnabled;
            [Range(1, 8)] public int cfg_ItemCount;
            public bool cfg_StaticShellCountEnabled;
            [Range(4, 10)] public int cfg_ShellCount;
            public bool cfg_TurnTimerEnabled;
            [Range(0, 4)] public int cfg_TurnTimeoutIndex;
            public Lobby(GameObject _handler, int _id, NetworkConnection _owner)
            {
                handler = _handler;
                id = _id;
                owner = _owner;
                playerCount = 1;
                
                cfg_ItemsEnabled = true;
                cfg_ItemCount = 3;
                
                cfg_StaticShellCountEnabled = true;
                cfg_ShellCount = 7;
                
                cfg_TurnTimerEnabled = true;
                cfg_TurnTimeoutIndex = 5;
            }
        }
    }
    
}
