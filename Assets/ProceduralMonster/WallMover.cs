using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class WallMover : MonoBehaviour
{
    public Transform Camera;
    public float CameraDistance = 10f;
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

        _cameraYRotation = 0;
        _cameraXRotation = 0;
    }

    private float _cameraYRotation = 0;
    private float _cameraXRotation = 0;
    private Vector3 _cameraDirection = Vector3.one;

    private void UpdateCameraRotation()
    {
        Camera.position = transform.position + Quaternion.AngleAxis(_cameraYRotation, transform.up) * transform.TransformDirection(
            new Vector3(
            0, 
            CameraDistance * Mathf.Sin(_cameraXRotation), 
            CameraDistance * Mathf.Cos(_cameraXRotation)));
        Camera.rotation = Quaternion.LookRotation(transform.position - Camera.position, transform.up);
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
                var forward = Vector3.ProjectOnPlane(transform.position - Camera.position, groundContactNormal);
                var right = Vector3.Cross(forward, groundContactNormal);
                walkingDirection = forward * input.y + -right * input.x;
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
        float xInput = Input.GetAxis("Mouse Y")/20f;
        _cameraYRotation += Input.GetAxis("Mouse X");
        _cameraXRotation += Mathf.Abs(xInput) > 0.01f ? xInput : 0;
        UpdateCameraRotation();
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
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