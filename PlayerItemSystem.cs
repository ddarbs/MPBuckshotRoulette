using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.CodeGenerating;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class PlayerItemSystem : NetworkBehaviour
{
#region Inspector Refs
    [SerializeField] private Transform[] i_PlayerOneSpawns, i_PlayerTwoSpawns;
    [SerializeField] private NetworkObject[] i_ItemPrefabs;
    [SerializeField] private ServerSideHealth i_PlayerOneHealth, i_PlayerTwoHealth;
    [SerializeField] private TurnSystem i_TurnSystem;
    [SerializeField] private TextMeshProUGUI i_ItemDescriptionText;
#endregion Inspector Refs

#region Variables
    private const float i_PillHealChance = 0.4f;

    private static PlayerItemSystem i_Instance;
    private Scene i_Scene;
    private Dictionary<int, bool> i_PlayerOneSlotDictionary, i_PlayerTwoSlotDictionary; // false meaning no item is in that slot, true meaning an item is in that slot
    private Dictionary<NetworkObject, int> i_PlayerOneItemDictionary, i_PlayerTwoItemDictionary;
    private NetworkConnection i_PlayerOne, i_PlayerTwo;
    private bool i_SawUsed = false;
#endregion Variables

    private void Awake()
    {
        i_Instance = this;
    }

#region Server
    public void SceneSetup(Scene _scene)
    {
        i_Scene = _scene;
    }
    
    public void Setup(NetworkConnection[] _conns)
    {
        i_PlayerOne = _conns[0];
        i_PlayerTwo = _conns[1];
        
        i_PlayerOneSlotDictionary = new Dictionary<int, bool>();
        i_PlayerOneItemDictionary = new Dictionary<NetworkObject, int>();
        for (int i = 0; i < i_PlayerOneSpawns.Length; i++)
        {
            i_PlayerOneSlotDictionary.Add(i, false);
        }
        
        i_PlayerTwoSlotDictionary = new Dictionary<int, bool>();
        i_PlayerTwoItemDictionary = new Dictionary<NetworkObject, int>();
        for (int i = 0; i < i_PlayerTwoSpawns.Length; i++)
        {
            i_PlayerTwoSlotDictionary.Add(i, false);
        }
    }

    public void SpawnItems(int _items) // call from turn or round system?
    {
        // player one
        int l_Spawned = 0;
        for (int i = 0; i < i_PlayerOneSlotDictionary.Count; i++)
        {
            if (l_Spawned == _items) // if we spawned the amount we should
            {
                break;
            }
            if (i_PlayerOneSlotDictionary[i]) // if slot is occupied go to next one
            {
                continue;
            }
            
            NetworkObject l_Item = Instantiate(RandomItem(), i_PlayerOneSpawns[i].position, Quaternion.identity);
            i_PlayerOneItemDictionary.Add(l_Item, i);
            ServerManager.Spawn(l_Item, i_PlayerOne, i_Scene);
            i_PlayerOneSlotDictionary[i] = true;
            l_Spawned++;
        }
        if (l_Spawned < _items)
        {
            // TODO: slots full couldn't spawn all the items, make fun of the player?
            Debug.Log("can't spawn any more items for Player One");
        }
        
        // player two
        l_Spawned = 0;
        for (int i = 0; i < i_PlayerTwoSlotDictionary.Count; i++)
        {
            if (l_Spawned == _items) // if we spawned the amount we should
            {
                break;
            }
            if (i_PlayerTwoSlotDictionary[i]) // if slot is occupied go to next one
            {
                continue;
            }
            
            NetworkObject l_Item = Instantiate(RandomItem(), i_PlayerTwoSpawns[i].position, Quaternion.identity);
            i_PlayerTwoItemDictionary.Add(l_Item, i);
            ServerManager.Spawn(l_Item, i_PlayerTwo, i_Scene);
            i_PlayerTwoSlotDictionary[i] = true;
            l_Spawned++;
        }
        if (l_Spawned < _items)
        {
            // TODO: slots full couldn't spawn all the items, make fun of the player?
            Debug.Log("can't spawn any more items for Player Two");
        }
    }

    private void UseItem(int _player, NetworkObject _item) // static call from the item scripts?
    {
        // can only use your items and can only use them on your turn
        if ((_player == 1 && _item.Owner != i_PlayerOne) || (_player == 1 && !i_TurnSystem.GetPlayerOneTurn()))
        {
            return;
        }
        if ((_player == 2 && _item.Owner != i_PlayerTwo) || (_player == 2 && i_TurnSystem.GetPlayerOneTurn()))
        {
            return;
        }
        if (i_SawUsed && _item.CompareTag("Item_Saw"))
        {
            return;
        }
        
        // TODO: item-specific functions? bring up a ui menu to confirm? need to double check what buckshot does I can't remember
        
        int l_Slot = -1;
        switch (_player)
        {
            case 1:
                if (i_PlayerOneItemDictionary.TryGetValue(_item, out l_Slot))
                {
                    Debug.Log("(server) Player One clicked slot: " + l_Slot + ", " + _item.tag);
                    switch (_item.tag)
                    {
                        case "Item_Smokes":
                            i_PlayerOneHealth.Heal(1);
                            Observer_AudioItemSmoke();
                            break;
                        case "Item_Beer":
                            i_TurnSystem.Server_EjectShell();
                            Observer_AudioItemBeer();
                            break;
                        case "Item_Pills":
                            if (Random.value > i_PillHealChance)
                            {
                                i_PlayerOneHealth.Damage(1);
                            }
                            else
                            {
                                i_PlayerOneHealth.Heal(2);
                            }
                            Observer_AudioItemPills();
                            break;
                        case "Item_Inverter":
                            i_TurnSystem.Server_InvertShell();
                            Observer_AudioItemInverter();
                            break;
                        case "Item_Magnifying":
                            i_TurnSystem.Server_MagnifyShell();
                            Observer_AudioItemMagnify();
                            break;
                        case "Item_Saw":
                            i_TurnSystem.Server_DoubleDamage();
                            i_SawUsed = true;
                            Observer_AudioItemSaw();
                            break;
                        case "Item_Phone":
                            i_TurnSystem.Server_PhoneCall();
                            Observer_AudioItemPhone();
                            break;
                        /* TODO:
                         * find models and implement the prefabs + slot onto item system
                         * no adrenaline or handcuffs 
                         */
                    }
                    i_PlayerOneItemDictionary.Remove(_item);
                    ServerManager.Despawn(_item);
                    i_PlayerOneSlotDictionary[l_Slot] = false;
                    Target_ItemUsed(i_PlayerOne);
                }
                break;
            case 2:
                if (i_PlayerTwoItemDictionary.TryGetValue(_item, out l_Slot))
                {
                    Debug.Log("(server) Player Two clicked slot: " + l_Slot + ", " + _item.tag);
                    switch (_item.tag)
                    {
                        case "Item_Smokes":
                            i_PlayerTwoHealth.Heal(1);
                            Observer_AudioItemSmoke();
                            break;
                        case "Item_Beer":
                            i_TurnSystem.Server_EjectShell();
                            Observer_AudioItemBeer();
                            break;
                        case "Item_Pills":
                            if (Random.value > i_PillHealChance)
                            {
                                i_PlayerTwoHealth.Damage(1);
                            }
                            else
                            {
                                i_PlayerTwoHealth.Heal(2);
                            }
                            Observer_AudioItemPills();
                            break;
                        case "Item_Inverter":
                            i_TurnSystem.Server_InvertShell();
                            Observer_AudioItemInverter();
                            break;
                        case "Item_Magnifying":
                            i_TurnSystem.Server_MagnifyShell();
                            Observer_AudioItemMagnify();
                            break;
                        case "Item_Saw":
                            i_TurnSystem.Server_DoubleDamage();
                            i_SawUsed = true;
                            Observer_AudioItemSaw();
                            break;
                        case "Item_Phone":
                            i_TurnSystem.Server_PhoneCall();
                            Observer_AudioItemPhone();
                            break;
                    }
                    i_PlayerTwoItemDictionary.Remove(_item);
                    ServerManager.Despawn(_item);
                    i_PlayerTwoSlotDictionary[l_Slot] = false;
                    Target_ItemUsed(i_PlayerTwo);
                }
                break;
        }
    }

    private NetworkObject RandomItem()
    {
        return i_ItemPrefabs[Random.Range(0, i_ItemPrefabs.Length)];
    }

    public void ResetSpecialItems()
    {
        i_SawUsed = false;
    }

    public void EndGame()
    {
        foreach (NetworkObject _item in i_PlayerOneItemDictionary.Keys)
        {
            ServerManager.Despawn(_item);
        }
        foreach (NetworkObject _item in i_PlayerTwoItemDictionary.Keys)
        {
            ServerManager.Despawn(_item);
        }

        Observer_EndGame();
    }
#endregion Server

    [ServerRpc(RequireOwnership = false)]
    private void Server_UseItem(NetworkConnection _conn, NetworkObject _item)
    {
        UseItem(_conn == i_PlayerOne ? 1 : 2, _item);
    }

    public static void Client_ClickItem(GameObject _item)
    {
        i_Instance.Server_UseItem(i_Instance.LocalConnection, _item.GetComponent<NetworkObject>());
    }

    public static void Client_StartHoverItem(string _itemTag)
    {
        switch (_itemTag)
        {
            case "Item_Smokes":
                i_Instance.i_ItemDescriptionText.text = "heals one health";
                break;
            case "Item_Beer":
                i_Instance.i_ItemDescriptionText.text = "ejects the current shell";
                break;
            case "Item_Pills":
                i_Instance.i_ItemDescriptionText.text = "40% chance to heal 2 hp, if not take 1 dmg";
                break;
            case "Item_Inverter":
                i_Instance.i_ItemDescriptionText.text = "change status of the current shell";
                break;
            case "Item_Magnifying":
                i_Instance.i_ItemDescriptionText.text = "look at the current shell";
                break;
            case "Item_Saw":
                i_Instance.i_ItemDescriptionText.text = "double damage on the current shell";
                break;
            case "Item_Phone":
                i_Instance.i_ItemDescriptionText.text = "look into the future";
                break;
        }
    }

    public static void Client_StopHoverItem()
    {
        i_Instance.i_ItemDescriptionText.text = "";
    }

    [TargetRpc]
    private void Target_ItemUsed(NetworkConnection _conn)
    {
        Client_StopHoverItem();
    }

    [ObserversRpc]
    private void Observer_EndGame()
    {
        Client_StopHoverItem();
    }

#region Audio
    [ObserversRpc]
    private void Observer_AudioItemBeer()
    {
        AudioSystem.Game_Item_Beer();
    }
    [ObserversRpc]
    private void Observer_AudioItemSmoke()
    {
        AudioSystem.Game_Item_Smoke();
    }
    [ObserversRpc]
    private void Observer_AudioItemMagnify()
    {
        AudioSystem.Game_Item_Magnify();
    }
    [ObserversRpc]
    private void Observer_AudioItemInverter()
    {
        AudioSystem.Game_Item_Inverter();
    }
    [ObserversRpc]
    private void Observer_AudioItemPhone()
    {
        AudioSystem.Game_Item_Phone();
    }
    [ObserversRpc]
    private void Observer_AudioItemSaw()
    {
        AudioSystem.Game_Item_Saw();
    }
    [ObserversRpc]
    private void Observer_AudioItemPills()
    {
        AudioSystem.Game_Item_Pills();
    }
#endregion Audio
}
