
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Common;
public class AnimChanger : UdonSharpBehaviour
{
    public Animator[] AnimsToEffect;//List
    public int SetToThis = 0; // Target anim name
    public override void Interact()//if button pushed
    {
        
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(egg)); //play function egg for all players
    }
    public void egg() //run the code in here 
    {
        foreach (Animator anim in AnimsToEffect) //loop through entire list
        {
            anim.Play(SetToThis.ToString()); //for each anim play number animation
        }
    }
}
