using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    const string IDLE = "Idle";
    const string RUN = "Run";

    CustomActions input;

    NavMeshAgent agent;
    Animator animator;

    [Header("Movement")]
    [SerializeField] ParticleSystem clickEffect;
    [SerializeField] LayerMask clickableLayers;

    float lookRotationSpeed = 8f;
    Vector3 lastMoveDirection;

    float clickEffectInterval = 0.2f; // 200ms in seconds
    float lastClickEffectTime = 0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        input = new CustomActions();
        AssignInputs();
    }

    void AssignInputs()
    {
        // No need to subscribe to performed event anymore
        // We'll check the button state in Update instead
    }

    void ClickToMove()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100, clickableLayers))
        {
            agent.destination = hit.point;
        }
    }

    void SpawnClickEffect()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100, clickableLayers))
        {
            if (clickEffect != null)
            { Instantiate(clickEffect, hit.point + new Vector3(0, 0.1f, 0), clickEffect.transform.rotation); }
        }
    }

    void OnEnable()
    { input.Enable(); }

    void OnDisable()
    { input.Disable(); }

    void Update()
    {
        // Check if Mouse.current is available
        if (Mouse.current == null) return;

        // Spawn effect immediately on first press
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            SpawnClickEffect();
            lastClickEffectTime = Time.time;
            Debug.Log("First click - Effect spawned at time: " + Time.time);
        }

        // Check if right mouse button is held down
        if (Mouse.current.rightButton.isPressed)
        {
            ClickToMove();

            // Spawn effect repeatedly every 300ms while holding
            float timeSinceLastEffect = Time.time - lastClickEffectTime;
            if (timeSinceLastEffect >= clickEffectInterval)
            {
                SpawnClickEffect();
                lastClickEffectTime = Time.time;
                Debug.Log("Repeat effect spawned at time: " + Time.time + " (interval: " + timeSinceLastEffect + ")");
            }
        }

        FaceTarget();
        SetAnimations();
    }

    void FaceTarget()
    {
        // Only rotate if moving
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            // Use actual movement direction (velocity) instead of destination
            lastMoveDirection = agent.velocity.normalized;
        }

        // If we have a valid direction to face, rotate towards it
        if (lastMoveDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(lastMoveDirection.x, 0, lastMoveDirection.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * lookRotationSpeed);
        }
    }

    void SetAnimations()
    {
        if (agent.velocity == Vector3.zero)
        { animator.Play(IDLE); }
        else
        { animator.Play(RUN); }
    }
}