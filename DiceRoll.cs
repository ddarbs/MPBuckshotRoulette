using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

public class DiceRoll : NetworkBehaviour
{
    //visual changes of the dice roll
    //call server to pick a number
    //have server shit numbers back out to players


    public int i_Player1Roll;
    public int i_Player2Roll;
    public int i_visualRollMax = 15;
    public int i_visualRoll;

    private int i_p1Placeholder = 0;
    private int i_p2Placeholder = 0;

    private int i_p1DummyRoll;
    private int i_p2DummyRoll;

    public TextMeshProUGUI i_Player1RollText;
    public TextMeshProUGUI i_Player2RollText;
    private float i_nextNumberCD = 0f;

    public bool i_ShowRolls = false;

    [SerializeField] private PlayerSpawnSystem i_PlayerSpawnSystem;
    [SerializeField] private PlayerItemSystem i_PlayerItemSystem;
    [SerializeField] private TurnSystem i_TempTurnSystem;
    [SerializeField] private GameObject i_RollUI;
    
    private void Update()
    {
        i_nextNumberCD += Time.deltaTime;
    
        if (i_ShowRolls == true && i_nextNumberCD > .2f)
        {
            i_p1DummyRoll = Random.Range(1, 7);
            i_p2DummyRoll = Random.Range(1, 7);

            while (i_p1DummyRoll == i_p1Placeholder)
            {
                i_p1DummyRoll = Random.Range(1, 7);
            }
            
            while (i_p2DummyRoll == i_p2Placeholder)
            {
                i_p2DummyRoll = Random.Range(1, 7);
            }

            i_p1Placeholder = i_p1DummyRoll;
            i_p2Placeholder = i_p2DummyRoll;
            
            
            i_nextNumberCD = 0f;
            i_visualRoll += 1;
            
            i_Player1RollText.text = i_p1DummyRoll.ToString();
            i_Player2RollText.text = i_p2DummyRoll.ToString();
            
            
            if (i_visualRoll > i_visualRollMax)
            {
                i_ShowRolls = false;
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    [ObserversRpc]
    public void ShowRollsOnClient(int Player1Roll, int Player2Roll)
    {
        i_ShowRolls = false;
        if (Player1Roll > Player2Roll)
        {
            i_Player1RollText.color = Color.green;
            i_Player2RollText.color = Color.red;
            this.GetComponent<PlayerUiController>().UpdatePlayerMessages("Player 1 Starts");
        } else if (Player1Roll < Player2Roll)
        {
            i_Player1RollText.color = Color.red;
            i_Player2RollText.color = Color.green;
            this.GetComponent<PlayerUiController>().UpdatePlayerMessages("Player 2 Starts");
        }
        i_Player1RollText.text = Player1Roll.ToString();
        i_Player2RollText.text = Player2Roll.ToString();
    }
    
    [ObserversRpc]
    public void StartDummyRolls()
    {
        i_nextNumberCD = 0;
        i_visualRoll = 0;
        i_RollUI.SetActive(true);
        i_ShowRolls = true;
    }

    [ObserversRpc]
    private void Observer_HideRolls()
    {
        i_RollUI.SetActive(false);
    }

    public IEnumerator DiceRolling()
    {
        i_Player1RollText.color = Color.yellow;
        i_Player2RollText.color = Color.yellow;
        StartDummyRolls();
         
        yield return StartCoroutine(WaitTimer(3.0f));
        i_Player1Roll = Random.Range(1, 7);
        i_Player2Roll = Random.Range(1, 7);

        while (i_Player1Roll == i_Player2Roll)
        {
            i_Player2Roll = Random.Range(1, 7);
        }
        Debug.Log(i_Player1Roll);
        Debug.Log(i_Player2Roll);
        ShowRollsOnClient(i_Player1Roll, i_Player2Roll);
        yield return StartCoroutine(WaitTimer(3.0f));
        // start function on turn system
        Observer_HideRolls();
        i_PlayerItemSystem.Setup(new NetworkConnection[] {i_PlayerSpawnSystem.i_PlayerOne, i_PlayerSpawnSystem.i_PlayerTwo});
        i_TempTurnSystem.StartTurnSystem(i_Player1Roll > i_Player2Roll);
    }
    
    private IEnumerator WaitTimer(float _time)
    {
        yield return new WaitForSeconds(_time);
    }
}
