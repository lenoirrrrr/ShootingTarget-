using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.AI;


public class Enemy : MonoBehaviour
{
    public int health = 100;
    private Rigidbody rb;

    //AI setting
    public int currentPointIndex = 0;
    public Vector3 currentTarget;
    public float positionThreshold;
    public float idleTime = 5f;
    public float attackDistance = 5f;
    public float maxVisionDistance = 20f;
    public float minChasingHealth = 30f;

    public Transform[] patrolPoints;
    private float idleTimeCounter;
    private Transform playerTransform;
    private bool canSeePlayer;
    private Vector3 lastKnownPlayerPosition;
    
    private NavMeshAgent agent;
    public enum State {Idle, Patrolling, Chasing, Attacking}
    public State state = State.Idle;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        playerTransform = GameObject.FindWithTag("Player").GetComponent<Transform>();

        GameObject patrolPointsParent = GameObject.FindWithTag("PatrolPoint");
        patrolPoints = patrolPointsParent.GetComponentsInChildren<Transform>().Where(t => t != patrolPointsParent.transform).ToArray();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Damage")
        {
            health -= 10;
            
            if(health <= 0)
            {
                Die();
            }
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
    
    private void Update()
    {
        LookForPlayer();
        switch (state)
        {
            case State.Idle:
                Idle();
                break;
            case State.Patrolling:
                Patrolling();
                break;
            case State.Chasing:
                Chasing();
                break;
            case State.Attacking:
                Attacking();
                break;
        }

        rb.linearVelocity = Vector3.zero;

        LookForPlayer();
        SetLastKnownPlayerPosition();
    }

    private void LookForPlayer()
    {
        Vector3 directionToPlayer = playerTransform.position - transform.position;

        if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, maxVisionDistance))
        {
            canSeePlayer = hit.transform == playerTransform;

            if (canSeePlayer && state != State.Attacking)
            {
                state = State.Chasing;
            }
        }
    }

    private void Idle()
    {
        agent.ResetPath();

        idleTimeCounter -= Time.deltaTime;

        if (idleTimeCounter < 0)
        {
            state = State.Patrolling;
            idleTimeCounter = idleTime;
        }
    }

    private void Patrolling()
    {
        if (Vector3.Distance(currentTarget, transform.position) < positionThreshold)
        {
            float chance = Random.Range(0, 100);

            if (chance < 10)
            {
                state = State.Idle;
                return;
            }

            currentPointIndex++;
            currentTarget = patrolPoints[currentPointIndex % patrolPoints.Length].position;
        }
        else
        {
            agent.SetDestination(currentTarget);
        }
    }

    private void Attacking()
    {
        idleTimeCounter = idleTime;
        agent.ResetPath();

        //Shoot();

        if (Vector3.Distance(transform.position, playerTransform.position) > attackDistance || !canSeePlayer)
        {
            if (health < minChasingHealth)
            {
                state = State.Patrolling; //Cautios
            }
            else
            {
                state = State.Chasing;
            }
        }
    }

    private void Chasing()
    {
        idleTimeCounter = idleTime;
        agent.SetDestination(lastKnownPlayerPosition);

        if (health < minChasingHealth)
        {
            state = State.Patrolling;
        }
        else if (Vector3.Distance(transform.position, playerTransform.position) <= attackDistance && canSeePlayer)
        {
            state = State.Attacking;
        }
        else if (Vector3.Distance(transform.position, playerTransform.position) > maxVisionDistance)
        {
            state = State.Patrolling;
        }
        else if (Vector3.Distance(transform.position, playerTransform.position) < positionThreshold && !canSeePlayer)
        {
            state = State.Patrolling;
        }
    }
    
    private void LookAtPlayer()
    {
        if (canSeePlayer)
        {
            transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));
        }
    }

    private void SetLastKnownPlayerPosition()
    {
        if (canSeePlayer)
        {
            lastKnownPlayerPosition = playerTransform.position;
        }
    }
}
