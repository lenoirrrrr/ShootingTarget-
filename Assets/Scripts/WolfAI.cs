using UnityEngine;
using UnityEngine.AI;
using System.Linq;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class WolfAI : MonoBehaviour
{
    private enum EnemyState { Patrolling, Chasing, Attacking, Dead }

    [Header("Health & Score")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private GameObject ammoPickupPrefab;
    [SerializeField] private int scoreValue = 50;

    [Header("Target Ranges")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float loseTargetRange = 18f;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitAtPatrolPoint = 2f;

    [Header("Attack Settings")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackImpactDelay = 0.4f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hitSound;

    [Header("Animation States")]
    [SerializeField] private string idleStateName = "idle";
    [SerializeField] private string walkStateName = "walk";
    [SerializeField] private string runStateName = "run";
    [SerializeField] private string attackStateName = "attack";
    [SerializeField] private string hitStateName = "hit";
    [SerializeField] private string deathStateName = "die";

    private Animator animator;
    private NavMeshAgent agent;
    private EnemyState state = EnemyState.Patrolling;
    private int currentHealth;
    private int patrolIndex;
    private float patrolWaitTimer;
    private float nextAttackTime;
    private bool attackDamagePending;
    private float attackImpactTime;
    private string lastPlayedAnim = "";

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
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
            }
        }
    }

    private void Start()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            patrolIndex = Random.Range(0, patrolPoints.Length);
        }
        SetPatrolDestination();
    }

    private void Update()
    {
        if (state == EnemyState.Dead) return;

        UpdatePendingAttackDamage();

        float distanceToPlayer = player == null ? float.PositiveInfinity : Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            state = EnemyState.Attacking;
        }
        else if (distanceToPlayer <= detectionRange || (state == EnemyState.Chasing && distanceToPlayer <= loseTargetRange))
        {
            state = EnemyState.Chasing;
        }
        else
        {
            state = EnemyState.Patrolling;
        }

        switch (state)
        {
            case EnemyState.Patrolling:
                Patrol();
                break;
            case EnemyState.Chasing:
                Chase();
                break;
            case EnemyState.Attacking:
                Attack();
                break;
        }

        UpdateMovementAnimation();
    }

    public void TakeDamage(int damage)
    {
        if (state == EnemyState.Dead || damage <= 0) return;

        currentHealth -= damage;
        if (audioSource != null && hitSound != null) audioSource.PlayOneShot(hitSound);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        lastPlayedAnim = "";
        PlayStateDirect(hitStateName);

        if (player != null) state = EnemyState.Chasing;
    }

    private void Patrol()
    {
        if (!CanUseAgent()) return;
        agent.speed = patrolSpeed;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.ResetPath();
            return;
        }

        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.1f) return;

        patrolWaitTimer += Time.deltaTime;
        if (patrolWaitTimer < waitAtPatrolPoint) return;

        if (patrolPoints.Length > 1)
        {
            int nextIndex = patrolIndex;
            while (nextIndex == patrolIndex)
            {
                nextIndex = Random.Range(0, patrolPoints.Length);
            }
            patrolIndex = nextIndex;
        }
        else
        {
            patrolIndex = 0;
        }

        patrolWaitTimer = 0f;
        SetPatrolDestination();
    }

    private void Chase()
    {
        if (player == null || !CanUseAgent())
        {
            state = EnemyState.Patrolling;
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
            state = EnemyState.Patrolling;
            return;
        }

        if (CanUseAgent()) agent.ResetPath();
        FacePlayer();

        if (Time.time < nextAttackTime) return;

        if (audioSource != null && attackSound != null) audioSource.PlayOneShot(attackSound);

        lastPlayedAnim = "";
        PlayStateDirect(attackStateName);

        nextAttackTime = Time.time + attackCooldown;
        attackImpactTime = Time.time + attackImpactDelay;
        attackDamagePending = true;
    }

    private void UpdatePendingAttackDamage()
    {
        if (!attackDamagePending || Time.time < attackImpactTime) return;
        attackDamagePending = false;

        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange + 0.5f)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>() ?? player.GetComponentInChildren<PlayerHealth>() ?? player.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    private void SetPatrolDestination()
    {
        if (!CanUseAgent() || patrolPoints == null || patrolPoints.Length == 0 || patrolPoints[patrolIndex] == null) return;
        agent.stoppingDistance = 0.2f;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);
        }
    }

    private void UpdateMovementAnimation()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if ((stateInfo.IsName(attackStateName) || stateInfo.IsName(hitStateName)) && stateInfo.normalizedTime < 1.0f)
        {
            return;
        }

        bool isMoving = CanUseAgent() && agent.velocity.sqrMagnitude > 0.05f;
        bool isRunning = isMoving && state == EnemyState.Chasing;
        bool isWalking = isMoving && state == EnemyState.Patrolling;

        if (isRunning) PlayStateDirect(runStateName);
        else if (isWalking) PlayStateDirect(walkStateName);
        else PlayStateDirect(idleStateName);
    }

    private void Die()
    {
        state = EnemyState.Dead;
        attackDamagePending = false;
        if (CanUseAgent()) agent.ResetPath();
        agent.enabled = false;

        foreach (Collider col in GetComponentsInChildren<Collider>()) col.enabled = false;

        if (ammoPickupPrefab != null)
        {
            Instantiate(ammoPickupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(scoreValue);

        // Notify the spawner about the death
        if (WolfSpawner.Instance != null)
        {
            WolfSpawner.Instance.OnWolfKilled();
        }

        PlayStateDirect(deathStateName);

        Destroy(gameObject, destroyDelay);
    }

    private void PlayStateDirect(string stateName)
    {
        if (string.IsNullOrEmpty(stateName) || lastPlayedAnim == stateName) return;
        int stateHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, stateHash))
        {
            animator.CrossFade(stateName, 0.15f);
            lastPlayedAnim = stateName;
        }
        else
        {
            Debug.LogWarning($"[WolfAI] State '{stateName}' tidak ditemukan di Animator Controller! Periksa penulisan namanya di Inspector.");
        }
    }

    private bool CanUseAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }
}
