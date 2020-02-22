using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class WallMover : MonoBehaviour
{
    public float Speed = 8.0f; // Speed when walking forward

    // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
    public float groundCheckDistance = 0.01f;
    public uint gravity = 30;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Vector3 groundContactNormal, currentNormal;
    private bool m_IsGrounded;

    private Vector3 direction;
    private bool isMoving, isSlowing, isWalking;

    private RaycastHit hit;
    private Ray ray;
    private Vector3 movingDirection, walkingDirection;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        capsule = GetComponent<CapsuleCollider>();

        currentNormal = Vector3.up;
        direction = Vector3.down;
        walkingDirection = Vector3.zero;
        isMoving = false;
        isSlowing = false;
        isWalking = true;
    }

    private void Update()
    {
        //isSlowing -> isMoving -> isWalking

        if (isMoving)
        {
            RotateCharacterToNormal();
            GroundCheck();
            if (m_IsGrounded)
            {
                isMoving = false;
                isWalking = true;
                direction = -groundContactNormal;
            }
        }

        if (isWalking)
        {
            GroundCheck();
            RotateCharacterToNormal();
        }
    }

    private void FixedUpdate()
    {
        if (isMoving)
        {
            //Вектор гравитации:
            rb.AddForce(direction * gravity, ForceMode.Acceleration);
            return;
        }

        if (isSlowing) return;

        if (isWalking)
        {
            //Вектор гравитации:
            rb.AddForce(direction * gravity, ForceMode.Acceleration);
            Vector2 input = GetInput();
            if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) && (m_IsGrounded))
            {
                walkingDirection = transform.forward * input.y + transform.right * input.x;
                walkingDirection = Vector3.ProjectOnPlane(walkingDirection, groundContactNormal).normalized;

                walkingDirection = walkingDirection * Speed;
                if (rb.velocity.sqrMagnitude < (Speed * Speed))
                {
                    rb.AddForce(walkingDirection, ForceMode.Impulse);
                }
            }
            else
            {
                walkingDirection = Vector3.zero;
            }

            WallCheck(walkingDirection);
        }
    }

    private Vector2 GetInput()
    {
        Vector2 input = new Vector2
        {
            x = 0,
            y = 1
        };
        return input;
    }

    private void GroundCheck()
    {
        RaycastHit groundHit;
        for (float i = 0.1f; i < 1; i += 0.1f)
        {
            if (Physics.SphereCast(transform.position, capsule.radius + i, direction,
                out groundHit,
                ((capsule.height / 2f) - capsule.radius) + groundCheckDistance,
                ~0,
                QueryTriggerInteraction.Ignore))
            {
                if (!groundHit.collider.isTrigger)
                {
                    m_IsGrounded = true;
                    rb.drag = 5f;
                    groundContactNormal = groundHit.normal;
                    direction = -groundContactNormal;
                }

                return;
            }
        }

        m_IsGrounded = false;
        groundContactNormal = -1 * direction;
    }

    private void WallCheck(Vector3 move)
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position, move);
        if (Physics.Raycast(ray, out hit, capsule.height))
        {
            groundContactNormal = hit.normal;
            direction = -groundContactNormal;
            RotateCharacterToNormal();
        }
    }

    private void RotateCharacterToNormal()
    {
        currentNormal = Vector3.Lerp(currentNormal, groundContactNormal, 10 * Time.deltaTime);
        Vector3 myForward = Vector3.Cross(transform.right, currentNormal);
        // align character to the new myNormal while keeping the forward direction:
        Quaternion targetRot = Quaternion.LookRotation(myForward, currentNormal);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, 10 * Time.deltaTime);
    }
}