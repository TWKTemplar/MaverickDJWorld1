
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerAnimTrigger : UdonSharpBehaviour
{
    public Animator animator;
    public override void OnPlayerTriggerEnter(VRC.SDKBase.VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            animator.SetBool("DoorOpen", true);
        }
    }
    public override void OnPlayerTriggerExit(VRC.SDKBase.VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            animator.SetBool("DoorOpen", false );
        }
    }
}
