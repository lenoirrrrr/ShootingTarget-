using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isGrounded;
    private PlayerInput playerInput;

    [Header("Audio")]
    public AudioSource footstepAudioSource;
    public AudioClip footstepClip;
    public float footstepInterval = 0.5f;
    private float footstepTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = new PlayerInput();
    }

    void Update()
    {
        CheckGround();
        HandleFootsteps();
    }

    void HandleFootsteps()
    {
        if (isGrounded && moveInput.magnitude > 0.1f && rb.linearVelocity.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                if (footstepAudioSource != null && footstepClip != null)
                {
                    footstepAudioSource.PlayOneShot(footstepClip);
                }
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    void OnJump()
    {
        if (isGrounded)
        {
            rb.AddForce(
                new Vector3(0, jumpForce, 0),
                ForceMode.Impulse
            );
        }
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask
        );
    }

    public void OnMovement(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void MovePlayer()
    {
        Vector3 direction =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        direction.Normalize();

        rb.linearVelocity = new Vector3(
            direction.x * moveSpeed,
            rb.linearVelocity.y,
            direction.z * moveSpeed
        );
    }
}