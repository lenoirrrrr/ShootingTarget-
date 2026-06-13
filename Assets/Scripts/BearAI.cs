using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class BearAI : MonoBehaviour
{
    private enum BearState
    {
        Patrolling,
        Chasing,
        Attacking,
        Dead
    }

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float destroyDelay = 5f;

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float loseTargetRange = 22f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitAtPatrolPoint = 2f;

    [Header("Attack")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackImpactDelay = 0.45f;

    private static readonly int IdleParameter = Animator.StringToHash("Idle");
    private static readonly int WalkParameter = Animator.StringToHash("WalkForward");
    private static readonly int RunParameter = Animator.StringToHash("Run Forward");
    private static readonly int AttackParameter = Animator.StringToHash("Attack1");
    private static readonly int HitParameter = Animator.StringToHash("Get Hit Front");
    private static readonly int DeathParameter = Animator.StringToHash("Death");

    private Animator animator;
    private NavMeshAgent agent;
    private BearState state = BearState.Patrolling;
    private int currentHealth;
    private int patrolIndex;
    private float patrolWaitTimer;
    private float nextAttackTime;
    private bool attackDamagePending;
    private float attackImpactTime;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        // Automatically find patrol points in the scene if not assigned in Inspector
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            GameObject patrolPointsParent = GameObject.FindWithTag("PatrolPoint");
            if (patrolPointsParent != null)
            {
                patrolPoints = patrolPointsParent.GetComponentsInChildren<Transform>()
                    .Where(t => t != patrolPointsParent.transform)
                    .ToArray();
                Debug.Log($"[BearAI] Automatically found {patrolPoints.Length} patrol points under target parent.");
            }
        }
    }

    private void Start()
    {
        if (agent == null)
        {
            Debug.LogError("[BearAI] NavMeshAgent component is missing!");
        }
        else
        {
            Debug.Log($"[BearAI] NavMeshAgent status: enabled={agent.enabled}, isOnNavMesh={agent.isOnNavMesh}");
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning("[BearAI] Patrol Points array is empty or null! Bear won't patrol.");
        }
        else
        {
            Debug.Log($"[BearAI] Patrol Points count: {patrolPoints.Length}");
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null)
                {
                    Debug.LogWarning($"[BearAI] Patrol Point at index {i} is null!");
                }
            }
        }

        SetPatrolDestination();
    }

    private BearState previousState = BearState.Patrolling;

    private void Update()
    {
        if (state == BearState.Dead)
        {
            return;
        }

        UpdatePendingAttackDamage();

        float distanceToPlayer = player == null
            ? float.PositiveInfinity
            : Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            state = BearState.Attacking;
        }
        else if (distanceToPlayer <= detectionRange ||
                 (state == BearState.Chasing && distanceToPlayer <= loseTargetRange))
        {
            state = BearState.Chasing;
        }
        else
        {
            state = BearState.Patrolling;
        }

        if (state != previousState)
        {
            Debug.Log($"[BearAI] State changed from {previousState} to {state}. Distance to player: {distanceToPlayer}");
            previousState = state;
        }

        switch (state)
        {
            case BearState.Patrolling:
                Patrol();
                break;
            case BearState.Chasing:
                Chase();
                break;
            case BearState.Attacking:
                Attack();
                break;
        }

        UpdateMovementAnimation();
    }

    public void TakeDamage(int damage)
    {
        if (state == BearState.Dead || damage <= 0)
        {
            return;
        }

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        animator.SetTrigger(HitParameter);

        if (player != null)
        {
            state = BearState.Chasing;
        }
    }

    private void Patrol()
    {
        if (!CanUseAgent())
        {
            return;
        }

        agent.speed = patrolSpeed;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.ResetPath();
            return;
        }

        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.1f)
        {
            return;
        }

        patrolWaitTimer += Time.deltaTime;
        if (patrolWaitTimer < waitAtPatrolPoint)
        {
            return;
        }

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        patrolWaitTimer = 0f;
        SetPatrolDestination();
    }

    private void Chase()
    {
        if (player == null || !CanUseAgent())
        {
            state = BearState.Patrolling;
            return;
        }

        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange * 0.85f;
        agent.SetDestination(player.position);
    }

    private void Attack()
    {
        if (player == null)
        {
            state = BearState.Patrolling;
            return;
        }

        if (CanUseAgent())
        {
            agent.ResetPath();
        }

        FacePlayer();

        if (Time.time < nextAttackTime)
        {
            return;
        }

        animator.SetTrigger(AttackParameter);
        nextAttackTime = Time.time + attackCooldown;
        attackImpactTime = Time.time + attackImpactDelay;
        attackDamagePending = true;
    }

    private void UpdatePendingAttackDamage()
    {
        if (!attackDamagePending || Time.time < attackImpactTime)
        {
            return;
        }

        attackDamagePending = false;

        if (player != null &&
            Vector3.Distance(transform.position, player.position) <= attackRange + 0.5f)
        {
            Debug.Log($"[BearAI] Attack hit! Distance: {Vector3.Distance(transform.position, player.position)}");

            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = player.GetComponentInChildren<PlayerHealth>();
            }
            if (playerHealth == null)
            {
                playerHealth = player.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
            else
            {
                Debug.LogWarning("[BearAI] Could not find PlayerHealth component on the player, its children, or its parent!");
                player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private void SetPatrolDestination()
    {
        if (!CanUseAgent() ||
            patrolPoints == null ||
            patrolPoints.Length == 0 ||
            patrolPoints[patrolIndex] == null)
        {
            return;
        }

        agent.stoppingDistance = 0.2f;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 8f);
        }
    }

    private void UpdateMovementAnimation()
    {
        bool isMoving = CanUseAgent() && agent.velocity.sqrMagnitude > 0.05f;
        bool isRunning = isMoving && state == BearState.Chasing;
        bool isWalking = isMoving && state == BearState.Patrolling;

        animator.SetBool(IdleParameter, !isMoving && state != BearState.Attacking);
        animator.SetBool(WalkParameter, isWalking);
        animator.SetBool(RunParameter, isRunning);
    }

    private void Die()
    {
        state = BearState.Dead;
        attackDamagePending = false;
        if (CanUseAgent())
        {
            agent.ResetPath();
        }

        agent.enabled = false;

        foreach (Collider bearCollider in GetComponentsInChildren<Collider>())
        {
            bearCollider.enabled = false;
        }

        animator.SetBool(IdleParameter, false);
        animator.SetBool(WalkParameter, false);
        animator.SetBool(RunParameter, false);
        animator.SetBool(DeathParameter, true);

        Destroy(gameObject, destroyDelay);
    }

    private bool CanUseAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
