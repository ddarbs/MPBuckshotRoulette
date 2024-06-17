using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ServerSideHealth : NetworkBehaviour 
{
    // turn system should have inspector refs to both PlayerOne ServerSideHealth script and PlayerTwo ServerSideHealth script to call Damage or Heal
    
#region Inspector Refs
    [SerializeField] private GameObject[] i_HealthOrbs;
    [SerializeField] private Image i_BloodLayer;
#endregion Inspector Refs

#region Variables
    private const int c_BaseHealth = 5;
    private readonly SyncVar<int> i_CurrentHealth = new SyncVar<int>(new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.Observers, 0.5f, Channel.Reliable));
    private bool i_Dying = false;
    private NetworkConnection i_Player;
#endregion Variables

#region Both
    private void Awake() // only matters on server since write permission is server only
    {
        i_CurrentHealth.Value = c_BaseHealth;
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
            
        i_CurrentHealth.OnChange += SyncVar_OnHealthChange;
        Debug.Log("OnStartServer in " + gameObject.name);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
            
        i_CurrentHealth.OnChange -= SyncVar_OnHealthChange;
        Debug.Log("OnStopServer in " + gameObject.name);
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
            
        i_CurrentHealth.OnChange += SyncVar_OnHealthChange; // BUG: this isn't being called when going from lobby->game on the client? why isn't the player seen as an observer?
        Debug.Log("OnStartClient in " + gameObject.name);
        Debug.Log(i_CurrentHealth + " " + i_CurrentHealth.Value);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
            
        i_CurrentHealth.OnChange -= SyncVar_OnHealthChange;
        Debug.Log("OnStopClient in " + gameObject.name);
    }

    private void SyncVar_OnHealthChange(int prev, int next, bool asServer) // server doesn't care it already knows the hp
    {
        Debug.Log($"prev {prev} - next {next}, asServer {asServer}");
        // only update ui on the clients
        if (asServer)
        {
            return;
        }
        // BUG: if they go -1 hp the fancy code fails
        if (next <= 0)
        {
            i_HealthOrbs[0].SetActive(false);
            i_HealthOrbs[1].SetActive(false);
            return;
        }
        // on new game
        if (next >= c_BaseHealth)
        {
            foreach (var _orb in i_HealthOrbs)
            {
                _orb.SetActive(true);
            }
            i_Dying = false;
            return;
        }

        if (Mathf.Abs(next - prev) > 1)
        {
            // deactivate if damage, activate if heal
            if (next < prev)
            {
                i_HealthOrbs[next].SetActive(false);
                i_HealthOrbs[next + 1].SetActive(false);
            }
            else if (next > prev)
            {
                i_HealthOrbs[prev].SetActive(true);
                i_HealthOrbs[prev + 1].SetActive(true);
            }
            else
            {
                // next == prev, unchanged
            }
        }
        else
        {
            // deactivate if damage, activate if heal
            if (next < prev)
            {
                i_HealthOrbs[next].SetActive(false);
            }
            else if (next > prev)
            {
                i_HealthOrbs[prev].SetActive(true);
            }
            else
            {
                // next == prev, unchanged
            }
        }
        
        if (next < prev)
        {
            AudioSystem.Game_HealthIndicator_Decrease();
        }
        else
        {
            AudioSystem.Game_HealthIndicator_Increase();
        }

        // owner-specific effects
        if (base.LocalConnection != i_Player)
        {
            return;
        }
        
        if (next < prev)
        {
            AudioSystem.Game_OnDamage(); // music dimming/heartbeat effect
            StartCoroutine(BloodEffect());
        }
    }
#endregion Both

#region Server
    public void Setup(NetworkConnection _conn) // could also just give _conn ownership and just check against ownership for owner-specific effects
    {
        i_Player = _conn;
        StartCoroutine(DelayUpdatePlayerInfo(_conn));
        Debug.Log($"{gameObject.name} will get setup for client id {_conn.ClientId}");
    }
    private IEnumerator DelayUpdatePlayerInfo(NetworkConnection _conn)
    {
        yield return new WaitForSeconds(2f);
        Target_UpdatePlayerInfo(_conn);
    }
    
    public void Damage(int _value) // only call this from the server
    {
        if (i_Dying)
        {
            return;
        }

        i_CurrentHealth.Value -= _value;

        if (i_CurrentHealth.Value <= 0)
        {
            i_Dying = true;
        }
    }

    public void Heal(int _value) // only call this from the server
    {
        if (i_Dying)
        {
            return;
        }

        i_CurrentHealth.Value = Mathf.Clamp(i_CurrentHealth.Value + _value, 0, c_BaseHealth);
    }

    public int GetHealth()
    {
        return i_CurrentHealth.Value;
    }

    public void EndGame()
    {
        i_Dying = false;
        i_CurrentHealth.Value = c_BaseHealth;
    }
#endregion Server

    [TargetRpc]
    private void Target_UpdatePlayerInfo(NetworkConnection _conn)
    {
        i_Player = _conn;
        Debug.Log($"(debug) {gameObject.name} got setup for client id {_conn.ClientId}, and my id is {base.LocalConnection.ClientId}");
    }

    private IEnumerator BloodEffect()
    {
        Color l_Color = i_BloodLayer.color;
        while (i_BloodLayer.color.a < 1f)
        {
            l_Color.a += 0.2f;
            i_BloodLayer.color = l_Color;
            yield return new WaitForSeconds(0.025f);
        }
        while (i_BloodLayer.color.a > 0f)
        {
            l_Color.a -= 0.05f;
            i_BloodLayer.color = l_Color;
            yield return new WaitForSeconds(0.15f);
        }
    }
}
