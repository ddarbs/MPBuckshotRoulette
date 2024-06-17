using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Demo.AdditiveScenes;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerUiController : NetworkBehaviour
{
    // darb - btw I setup health orb managing under ServerSideHealth script, since both clients are observers on them it'll show for both
    // also I was thinking we might want to handle the shell movement stuff on the server side and just let networktransform sync? not sure if we should local move spawned networkobjects

    // i is for private variables
    //c for constants
    [SerializeField] private GameObject i_player1DiceText;
    [SerializeField] private GameObject i_player2DiceText;
    [SerializeField] private GameObject i_statusUpdateText;
    [SerializeField] private GameObject i_Playertimer;
    [SerializeField] private GameObject i_readyButton;
    [SerializeField] private GameObject i_InitialUI;
    //public GameObject i_p1Health; // cant be deactivating gameobjects with scripts on them that client/server use
    //public GameObject i_p2Health;
    public GameObject i_Ammo;
    public GameObject i_Shotgun;

    public GameObject[] i_shotgunShells = new GameObject[10];
    public GameObject[] i_player1Health = new GameObject[5];
    public GameObject[] i_player2Health = new GameObject[5];
    public GameObject[] i_p1CameraPositions = new GameObject[5];
    public GameObject[] i_p2CameraPositions = new GameObject[5];
    public GameObject[] i_p1CameraLookAt = new GameObject[5];
    public GameObject[] i_p2CameraLookAt = new GameObject[5];

    private bool i_CameraMoving = false;
    private float i_moveSpeed = 4f; // Speed of the camera movement
    private float i_rotationSpeed = 4f; // Speed of the camera rotation
    private Vector3 i_targetPosition;
    private Vector3 i_lookAt;

    public override void OnStartServer()
    {
        base.OnStartServer();

        i_player1DiceText.SetActive(false);
        i_player2DiceText.SetActive(false);
        i_statusUpdateText.SetActive(false);
        i_Playertimer.SetActive(false);
        i_readyButton.SetActive(false);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        i_player1DiceText.SetActive(false);
        i_player2DiceText.SetActive(false);
        i_statusUpdateText.SetActive(false);
        i_Playertimer.SetActive(false);
        i_readyButton.SetActive(true);
        //i_p1Health.SetActive(false);
        //i_p2Health.SetActive(false);
        foreach (var _orb in i_player1Health)
        {
            _orb.SetActive(false);
        }
        foreach (var _orb in i_player2Health)
        {
            _orb.SetActive(false);
        }
        //i_Shotgun.SetActive(false); // never gets turned back on
        i_Ammo.SetActive(false);

        i_InitialUI.SetActive(true);
    }

    private void Update()
    {
        if (i_CameraMoving)
        {
            MoveCamera();
        }
    }

    [ObserversRpc]
    public void DiceRollUi()
    {
        i_player1DiceText.SetActive(true);
        i_player2DiceText.SetActive(true);
        i_readyButton.SetActive(false);
    }

    [ObserversRpc]
    public void SendPlayersUpdateMessage(string message)
    {
        UpdatePlayerMessages(message);
    }

    public void UpdatePlayerMessages(string message)
    {
        i_statusUpdateText.SetActive(true);
        i_statusUpdateText.GetComponent<TextMeshProUGUI>().text = message;
    }

    [ObserversRpc]
    public void disablePlayerUpdateMessage()
    {
        i_statusUpdateText.SetActive(false);
    }

    public void Server_EndGame()
    {
        Observer_BackToReadyUp();
    }

    [ObserversRpc]
    private void Observer_BackToReadyUp()
    {
        i_player1DiceText.SetActive(false);
        i_player2DiceText.SetActive(false);
        i_statusUpdateText.SetActive(false);
        i_Playertimer.SetActive(false);
        i_readyButton.SetActive(true);

        i_InitialUI.SetActive(true);
    }


    [TargetRpc]
    public void StartCameraMovement(NetworkConnection conn, Vector3 targetPosition, Vector3 lookAt )
    {
        i_lookAt = lookAt;
        i_targetPosition = targetPosition;
        i_CameraMoving = true;
    }

    private void MoveCamera()
    {
       
        if (Vector3.Distance(Camera.main.transform.position, i_targetPosition) > 0.1f)
        {
            Camera.main.transform.position = Vector3.MoveTowards(Camera.main.transform.position, i_targetPosition,
                i_moveSpeed * Time.deltaTime);
        }
        else
        {
            Quaternion targetRotation = Quaternion.LookRotation(i_lookAt - Camera.main.transform.position);
            Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, targetRotation, i_rotationSpeed * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                i_CameraMoving = false;
                Debug.Log("camera moving ended");
            }
        }
    }
    
    public void activateGameObject(GameObject Enablethis)
    {
        Enablethis.SetActive(true);
    }

    [ObserversRpc]
    public void Observer_ActivatePlayerOneHealth(int _hp)
    {
        for (int i = 0; i < _hp; i++)
        {
            i_player1Health[i].SetActive(true);
        }
    }
    
    [ObserversRpc]
    public void Observer_ActivatePlayerTwoHealth(int _hp)
    {
        for (int i = 0; i < _hp; i++)
        {
            i_player2Health[i].SetActive(true);
        }
    }


}
