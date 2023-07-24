using TarodevController;
using UnityEngine;

/// <summary>
/// This is an example of how you can override the default behavior of the PlayerController.
/// </summary>
public class CustomPlayerController : PlayerController
{
    // Here we're overriding how we handle crouch. Originally we used the y input axis to determine if we should crouch.
    // protected override bool CrouchPressed => FrameInput.ExampleActionHeld;

    public Vector2 playerCenter { get; private set; }

    public Vector2 GetPlayerCenter() {
        if (Crouching) {
            playerCenter = CrouchingColliderRef.bounds.center;
        }
        else {
            playerCenter = StandingColliderRef.bounds.center;
        }

        return playerCenter;
    }
}