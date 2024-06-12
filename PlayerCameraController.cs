using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCameraController : NetworkBehaviour
{
#region Inspector Refs
    [SerializeField] public Camera i_Camera;
    [SerializeField] private AudioListener i_AudioListener;
    [SerializeField] private PhysicsRaycaster i_CameraRaycaster;
#endregion Inspector Refs
    

#region Client
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
        {
            return;
        }

       
        i_AudioListener.enabled = true;
        i_Camera.enabled = true;
        i_CameraRaycaster.enabled = true;
    }
#endregion Client
}
