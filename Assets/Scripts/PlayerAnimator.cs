using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator anim;
    private PlayerController playerController;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        HandleAnimation();
    }

    private void HandleAnimation()
    {
        if (anim == null || playerController == null) return;

        // Get movement data from PlayerController
        Vector2 moveInput = playerController.MoveInput;
        bool isMoving = playerController.IsMoving;
        bool isSprinting = playerController.IsSprinting;
        float speed = moveInput.magnitude;

        // Update animator parameters
        // isRunning - true when player is moving (any movement)
        anim.SetBool("isRunning", isMoving);

        // Optional: isSprinting - true when player is sprinting
        // Only set if your animator has this parameter
        if (HasParameter(anim, "isSprinting"))
        {
            anim.SetBool("isSprinting", isSprinting);
        }

        // Optional: speed float for blend trees
        if (HasParameter(anim, "speed"))
        {
            anim.SetFloat("speed", speed);
        }

        // Optional: individual velocity components for strafe animations
        if (HasParameter(anim, "velocityX"))
        {
            anim.SetFloat("velocityX", moveInput.x);
        }

        if (HasParameter(anim, "velocityZ"))
        {
            anim.SetFloat("velocityZ", moveInput.y);
        }
    }

    // Helper method to check if animator has a parameter
    private bool HasParameter(Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}
