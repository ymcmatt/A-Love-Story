using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class SimpleAttach : MonoBehaviour
{
    private Interactable interactable;
    private bool soundPlayed;

    // Start is called before the first frame update
    void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    void HandHoverBegin(Hand hand)
    {
        hand.ShowGrabHint();
    }

    void HandHoverEnd(Hand hand)
    {
        hand.HideGrabHint();
    }

    void HandHoverUpdate(Hand hand)
    {
        GrabTypes grabType = hand.GetGrabStarting();
        bool isGrabEnding = hand.IsGrabEnding(gameObject);

        if (interactable.attachedToHand == null && grabType != GrabTypes.None)
        {
            hand.AttachObject(gameObject, grabType);
            hand.HoverLock(interactable);
            hand.HideGrabHint();
            if (!soundPlayed)
            {
                try
                {
                    GetComponent<AudioSource>().Play();
                    soundPlayed = true;
                }
                catch
                {

                }
            }
        }

        else if (isGrabEnding)
        {
            hand.DetachObject(gameObject);
            hand.HoverUnlock(interactable);
        }
    }
}
