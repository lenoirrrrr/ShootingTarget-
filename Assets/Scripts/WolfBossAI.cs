using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class WolfBossAI : MonoBehaviour
{
    private enum EnemyState { Patrolling, Chasing, Attacking, Dead }

    [Header("Health & Score")]
    [SerializeField] private int maxHealth = 500;
    [SerializeField] private float destroyDelay = 5f;
    [SerializeField] private GameObject ammoPickupPrefab;
    [SerializeField] private int scoreValue = 500;

    [Header("Target Ranges")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float loseTargetRange = 30f;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitAtPatrolPoint = 2f;

    [Header("Attack Settings")]
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private int attackDamage = 30;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackImpactDelay = 0.6f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hitSound;

    [Header("Animation State Names")]
    [SerializeField] private string idleStateName = "idle";
    [SerializeField] private string walkStateName = "walk";
    [SerializeField] private string runStateName = "run";
    [SerializeField] private string[] attackStateNames = new string[] { "attack1", "attack2", "attack3" };
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
    }

    private void Start()
    {
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

        if (audioSource != null && attackSound != null) audioSource.PlayOneShot(attackSound);

        lastPlayedAnim = "";
        if (attackStateNames != null && attackStateNames.Length > 0)
        {
            string randomAttack = attackStateNames[Random.Range(0, attackStateNames.Length)];
            PlayStateDirect(randomAttack);
        }

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
        
        // Check if any of the attack animations are playing
        bool isPlayingAttack = false;
        if (attackStateNames != null)
        {
            foreach (string attackName in attackStateNames)
            {
                if (stateInfo.IsName(attackName))
                {
                    if (stateInfo.normalizedTime < 1.0f)
                    {
                        isPlayingAttack = true;
                    }
                    break;
                }
            }
        }

        if (isPlayingAttack || (stateInfo.IsName(hitStateName) && stateInfo.normalizedTime < 1.0f))
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
            Debug.LogWarning($"[WolfBossAI] State '{stateName}' tidak ditemukan di Animator Controller! Periksa penulisan namanya di Inspector.");
        }
    }

    private bool CanUseAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }
}
