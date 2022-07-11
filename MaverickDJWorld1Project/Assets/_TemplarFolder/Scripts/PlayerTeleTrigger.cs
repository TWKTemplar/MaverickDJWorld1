
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerTeleTrigger : UdonSharpBehaviour
{
    public Transform Telepoint;
    public VRCPlayerApi localPlayer;
    void Start()
    {
        localPlayer = Networking.LocalPlayer; 
    }

    public override void OnPlayerTriggerEnter(VRC.SDKBase.VRCPlayerApi player) 
    {
        if(player.isLocal)
        {
            player.TeleportTo(Telepoint.position, Telepoint.rotation);
        }
    }
    public override void OnPlayerTriggerExit(VRC.SDKBase.VRCPlayerApi player) 
    {
    
    }

}
