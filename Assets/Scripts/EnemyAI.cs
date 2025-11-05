using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    const string IDLE = "Idle";
    const string WALK = "Walk";
    const string ATTACK = "Attack";

    [Header("Combat Settings")]
    [SerializeField] float detectionRange = 10f;
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] int attackDamage = 1;
    [SerializeField] float attackSpeed = 1.0f;
    [SerializeField] float attackDelay = 0.3f;
    [SerializeField] ParticleSystem hitEffect;

    [Header("Movement")]
    [SerializeField] float rotationSpeed = 5f;

    Transform player;
    NavMeshAgent agent;
    Animator animator;
    Actor actor;

    bool isAttacking = false;
    float attackAnimationLength = 0f;
    float lastAttackTime = 0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        actor = GetComponent<Actor>();
    }

    void Start()
    {
        // Find the player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Get attack animation length
        if (animator != null)
        {
            RuntimeAnimatorController ac = animator.runtimeAnimatorController;
            foreach (AnimationClip clip in ac.animationClips)
            {
                if (clip.name == ATTACK)
                {
                    attackAnimationLength = clip.length;
                    break;
                }
            }

            if (attackAnimationLength == 0f)
            {
                attackAnimationLength = 1f;
            }
        }
    }

    void Update()
    {
        if (player == null || actor.currentHealth <= 0) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Always face the player if within detection range
        if (distanceToPlayer <= detectionRange)
        {
            FacePlayer();

            // If in attack range, attack
            if (distanceToPlayer <= attackRange)
            {
                agent.SetDestination(transform.position);
                TryAttack();
            }
            // Otherwise chase the player
            else
            {
                agent.SetDestination(player.position);
            }
        }

        SetAnimations();
    }

    void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Keep rotation only on Y axis

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void TryAttack()
    {
        if (isAttacking) return;

        // Check if enough time has passed since last attack
        float attackCooldown = attackAnimationLength / attackSpeed;
        if (Time.time - lastAttackTime < attackCooldown) return;

        isAttacking = true;
        lastAttackTime = Time.time;

        // Play attack animation with speed - force restart from beginning
        if (animator != null)
        {
            animator.speed = attackSpeed;
            // Play from the start (layer 0, normalized time 0)
            animator.Play(ATTACK, 0, 0f);
        }

        // Calculate timing based on animation and speed
        float delayToHit = attackDelay / attackSpeed;
        float attackDuration = attackAnimationLength / attackSpeed;

        // Deal damage after delay
        Invoke(nameof(DealDamage), delayToHit);
        // Reset attack state after animation completes
        Invoke(nameof(ResetAttack), attackDuration);
    }

    void DealDamage()
    {
        if (player == null) return;

        // Check if player is still in range
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            Actor playerActor = player.GetComponent<Actor>();

            if (playerActor != null)
            {
                playerActor.TakeDamage(attackDamage);

                // Spawn hit effect on player
                if (hitEffect != null)
                {
                    Instantiate(hitEffect, player.position + new Vector3(0, 1, 0), Quaternion.identity);
                }

                Debug.Log($"Enemy dealt {attackDamage} damage to player!");
            }
        }
    }

    void ResetAttack()
    {
        isAttacking = false;
        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    void SetAnimations()
    {
        if (animator == null) return;
        if (isAttacking) return;

        // Check if agent is moving (using velocity magnitude like player does)
        if (agent.velocity.magnitude > 0.1f)
        {
            animator.Play(WALK);
        }
        else
        {
            animator.Play(IDLE);
        }
    }

    // Visualize detection and attack ranges in editor
    void OnDrawGizmosSelected()
    {
        // Detection range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
