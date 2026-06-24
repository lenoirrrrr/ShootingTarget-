using UnityEngine;
using UnityEngine.AI;
using System.Linq;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class AnimalAI : MonoBehaviour
{
    private enum EnemyState { Patrolling, Chasing, Attacking, Dead }

    [Header("Health & Score")]
    [SerializeField] private int maxHealth = 150;
    [SerializeField] private float destroyDelay = 5f;
    [SerializeField] private GameObject ammoPickupPrefab;
    [SerializeField] private int scoreValue = 100;

    [Header("Target Ranges")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float loseTargetRange = 22f;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitAtPatrolPoint = 2f;

    [Header("Attack Settings")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackImpactDelay = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hitSound;

    [Header("Animation Modes")]
    [Tooltip("If true, triggers/parameters are used. If false, state names are played directly.")]
    [SerializeField] private bool useAnimatorParameters = false;

    [Header("Animation State Names (Direct Mode)")]
    [SerializeField] private string idleStateName = "idle";
    [SerializeField] private string walkStateName = "walk";
    [SerializeField] private string runStateName = "run";
    [SerializeField] private string attackStateName = "attack";
    [SerializeField] private string hitStateName = "hit";
    [SerializeField] private string deathStateName = "die";

    [Header("Animation Parameters (Parameter Mode)")]
    [SerializeField] private string idleParam = "Idle";
    [SerializeField] private string walkParam = "WalkForward";
    [SerializeField] private string runParam = "Run Forward";
    [SerializeField] private string attackParam = "Attack1";
    [SerializeField] private string hitParam = "Get Hit Front";
    [SerializeField] private string deathParam = "Death";

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

    // Hashes for parameter mode performance
    private int idleParamHash;
    private int walkParamHash;
    private int runParamHash;
    private int attackParamHash;
    private int hitParamHash;
    private int deathParamHash;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

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
        // Cache parameter hashes
        idleParamHash = Animator.StringToHash(idleParam);
        walkParamHash = Animator.StringToHash(walkParam);
        runParamHash = Animator.StringToHash(runParam);
        attackParamHash = Animator.StringToHash(attackParam);
        hitParamHash = Animator.StringToHash(hitParam);
        deathParamHash = Animator.StringToHash(deathParam);

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

        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        PlayAnimationTrigger(hitParamHash, hitStateName);

        if (player != null)
        {
            state = EnemyState.Chasing;
        }
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

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
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

        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        PlayAnimationTrigger(attackParamHash, attackStateName);

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
            else
            {
                player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
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
        bool isMoving = CanUseAgent() && agent.velocity.sqrMagnitude > 0.05f;
        bool isRunning = isMoving && state == EnemyState.Chasing;
        bool isWalking = isMoving && state == EnemyState.Patrolling;

        if (useAnimatorParameters)
        {
            animator.SetBool(idleParamHash, !isMoving && state != EnemyState.Attacking);
            animator.SetBool(walkParamHash, isWalking);
            animator.SetBool(runParamHash, isRunning);
        }
        else
        {
            if (state == EnemyState.Attacking) return; // Attack animation plays via Trigger code

            if (isRunning) PlayStateDirect(runStateName);
            else if (isWalking) PlayStateDirect(walkStateName);
            else PlayStateDirect(idleStateName);
        }
    }

    private void Die()
    {
        state = EnemyState.Dead;
        attackDamagePending = false;
        if (CanUseAgent()) agent.ResetPath();

        agent.enabled = false;

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        if (ammoPickupPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            Instantiate(ammoPickupPrefab, spawnPos, Quaternion.identity);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue);
        }

        if (useAnimatorParameters)
        {
            animator.SetBool(idleParamHash, false);
            animator.SetBool(walkParamHash, false);
            animator.SetBool(runParamHash, false);
            animator.SetBool(deathParamHash, true);
        }
        else
        {
            PlayStateDirect(deathStateName);
        }

        Destroy(gameObject, destroyDelay);
    }

    private void PlayAnimationTrigger(int paramHash, string stateName)
    {
        if (useAnimatorParameters)
        {
            animator.SetTrigger(paramHash);
        }
        else
        {
            PlayStateDirect(stateName);
        }
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
            Debug.LogWarning($"[AnimalAI] State '{stateName}' not found in the Animator Controller on {gameObject.name}. Please check the state name in the Inspector.");
        }
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
