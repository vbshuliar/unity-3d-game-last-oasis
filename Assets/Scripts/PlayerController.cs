using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    const string IDLE = "Idle";
    const string WALK = "Walk";
    const string ATTACK = "Attack";
    const string PICKUP = "Pickup";

    string currentAnimation;

    CustomActions input;

    NavMeshAgent agent;
    Animator animator;
    Actor actor;

    [Header("Movement")]
    [SerializeField] ParticleSystem clickEffect;
    [SerializeField] LayerMask clickableLayers;

    float lookRotationSpeed = 8f;
    Vector3 lastMoveDirection;

    float clickInterval = 0.2f; // 200ms in seconds
    float lastClickTime = 0f;

    [Header("Attack")]
    [SerializeField] float attackSpeed = 1.5f;
    [SerializeField] float attackDelay = 0.3f;
    [SerializeField] float attackDistance = 1.5f;
    [SerializeField] int attackDamage = 1;
    [SerializeField] ParticleSystem hitEffect;

    bool playerBusy = false;
    Interactable target;
    float attackAnimationLength = 0f;

    // Power-up tracking
    bool isPoweredUp = false;
    Vector3 originalScale;
    float originalSpeed;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        actor = GetComponent<Actor>();

        input = new CustomActions();
        AssignInputs();
    }

    void Start()
    {
        // Get the actual length of the attack animation
        RuntimeAnimatorController ac = animator.runtimeAnimatorController;
        foreach (AnimationClip clip in ac.animationClips)
        {
            if (clip.name == ATTACK)
            {
                attackAnimationLength = clip.length;
                Debug.Log($"Attack animation found: {clip.name}, Length: {attackAnimationLength} seconds");
                break;
            }
        }

        // Fallback if animation not found
        if (attackAnimationLength == 0f)
        {
            attackAnimationLength = 1f;
            Debug.LogWarning("Attack animation not found, using default length of 1 second");
        }

        // Store original values for power-ups
        originalScale = transform.localScale;
        originalSpeed = agent.speed;
    }

    void AssignInputs()
    {
        // Input will be handled in Update for hold-to-click support
    }

    void ClickToMove()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100, clickableLayers))
        {
            if (hit.transform.CompareTag("Interactable"))
            {
                target = hit.transform.GetComponent<Interactable>();
                if (clickEffect != null)
                { Instantiate(clickEffect, hit.transform.position + new Vector3(0, 0.1f, 0), clickEffect.transform.rotation); }
            }
            else
            {
                target = null;

                agent.destination = hit.point;
                if (clickEffect != null)
                { Instantiate(clickEffect, hit.point + new Vector3(0, 0.1f, 0), clickEffect.transform.rotation); }
            }
        }
    }

    void OnEnable()
    { input.Enable(); }

    void OnDisable()
    { input.Disable(); }

    void Update()
    {
        // Stop all actions if player is dead
        if (actor != null && actor.currentHealth <= 0)
        {
            agent.SetDestination(transform.position);
            return;
        }

        HandleMouseInput();
        FollowTarget();
        FaceTarget();
        SetAnimations();
    }

    void HandleMouseInput()
    {
        // Check if Mouse.current is available
        if (Mouse.current == null) return;

        // First press - execute immediately
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            ClickToMove();
            lastClickTime = Time.time;
        }
        // While holding - execute every 200ms
        else if (Mouse.current.rightButton.isPressed)
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            if (timeSinceLastClick >= clickInterval)
            {
                ClickToMove();
                lastClickTime = Time.time;
            }
        }
    }

    void FollowTarget()
    {
        if (target == null) return;

        if (Vector3.Distance(target.transform.position, transform.position) <= attackDistance)
        { ReachDistance(); }
        else
        { agent.SetDestination(target.transform.position); }
    }

    void FaceTarget()
    {
        Vector3 facing = Vector3.zero;

        // If we have a target (enemy/item), face that
        if (target != null)
        {
            facing = target.transform.position;
        }
        // Otherwise, use actual movement direction for smooth rotation
        else
        {
            // Only update direction if moving
            if (agent.velocity.sqrMagnitude > 0.1f)
            {
                lastMoveDirection = agent.velocity.normalized;
            }

            // If we have a valid last direction, use it
            if (lastMoveDirection != Vector3.zero)
            {
                facing = transform.position + lastMoveDirection;
            }
            else
            {
                return; // No direction to face
            }
        }

        Vector3 direction = (facing - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * lookRotationSpeed);
    }

    void ReachDistance()
    {
        agent.SetDestination(transform.position);

        if (playerBusy) return;

        playerBusy = true;

        switch (target.interactionType)
        {
            case InteractableType.Enemy:

                // Set animator speed to control attack animation playback speed
                animator.speed = attackSpeed;
                // Play attack from the start (layer 0, normalized time 0) - ensures full animation every hit
                animator.Play(ATTACK, 0, 0f);

                // Calculate timing based on actual animation length and attack speed
                float attackDuration = attackAnimationLength / attackSpeed;
                float delayToHit = attackDelay / attackSpeed;

                // Send damage at the specified delay point in the animation
                Invoke(nameof(SendAttack), delayToHit);
                // Reset after the full animation completes
                Invoke(nameof(ResetBusyState), attackDuration);
                break;
            case InteractableType.Item:

                target.InteractWithItem(this);
                target = null;

                Invoke(nameof(ResetBusyState), 0.5f);
                break;
        }
    }

    void SendAttack()
    {
        if (target == null) return;

        // Don't attack if player is dead
        if (actor != null && actor.currentHealth <= 0) return;

        if (target.myActor.currentHealth <= 0)
        { target = null; return; }

        Instantiate(hitEffect, target.transform.position + new Vector3(0, 1, 0), Quaternion.identity);
        target.GetComponent<Actor>().TakeDamage(attackDamage);
    }

    void ResetBusyState()
    {
        playerBusy = false;
        animator.speed = 1f; // Reset animator speed to normal
        SetAnimations();
    }

    void SetAnimations()
    {
        if (playerBusy) return;

        // Check if agent is moving (using velocity magnitude for better accuracy)
        if (agent.velocity.magnitude > 0.1f)
        { animator.Play(WALK); }
        else
        { animator.Play(IDLE); }
    }

    public void ApplyGreenPotionEffect(float sizeMultiplier, float speedMultiplier, float duration)
    {
        // If already powered up, stop the current coroutine
        if (isPoweredUp)
        {
            StopCoroutine("PowerUpCoroutine");
        }

        StartCoroutine(PowerUpCoroutine(sizeMultiplier, speedMultiplier, duration));
    }

    IEnumerator PowerUpCoroutine(float sizeMultiplier, float speedMultiplier, float duration)
    {
        isPoweredUp = true;

        // Apply power-up effects
        transform.localScale = originalScale * sizeMultiplier;
        agent.speed = originalSpeed * speedMultiplier;

        Debug.Log($"Power-up activated! Size: {sizeMultiplier}x, Speed: {speedMultiplier}x for {duration} seconds");

        // Wait for duration
        yield return new WaitForSeconds(duration);

        // Revert to original values
        transform.localScale = originalScale;
        agent.speed = originalSpeed;

        isPoweredUp = false;

        Debug.Log("Power-up expired. Returned to normal.");
    }
}
