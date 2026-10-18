using UnityEngine;

public class TitanfallFullMovement : MonoBehaviour
{
    [Header("Компоненты (Находят себя сами)")]
    private Rigidbody rb;
    private CapsuleCollider playerCollider;

    [Header("Камера")]
    public Transform cameraTransform;

    [Header("Параметры скорости")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float wallRunSpeed = 13f;
    public float maxSlideSpeed = 17f;
    public float slideSpeedMultiplier = 2f;

    [Header("Прыжки и Прыжковый Модуль")]
    public float jumpForce = 8f;
    public float wallKickForce = 10f;
    public float wallRunGravityModifier = 0.08f;
    public float airControlModifier = 0.4f;

    [Header("Камера и Мышь")]
    public float mouseSensitivity = 2f;
    public float maxCameraTilt = 15f;
    public float slideCameraTilt = 45f;
    public float cameraTiltSpeed = 8f;

    private bool isGrounded;
    private bool isSprinting;
    private bool isSliding;
    private bool isWallRunning;

    // Прыжковый модуль Купера (Максимум 2 прыжка в сумме)
    private int jumpCount = 0;
    private int maxJumps = 2;

    private Vector3 wallNormal;
    private float slideTimer;
    private float maxSlideTime = 0.7f;
    private float originalHeight;
    private float cameraPitch = 0f;
    private float currentCameraTilt = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        originalHeight = playerCollider.height;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        LookAround();
        CheckEnvironment();
        HandleInput();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void LookAround()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);

        float targetTilt = 0f;

        if (isSliding)
        {
            targetTilt = slideCameraTilt;
        }
        else if (isWallRunning)
        {
            RaycastHit tiltHit;
            if (Physics.Raycast(transform.position, transform.right, out tiltHit, 1.2f))
            {
                if (tiltHit.collider.CompareTag("Wall"))
                    targetTilt = maxCameraTilt;
            }
            else
            {
                targetTilt = -maxCameraTilt;
            }
        }

        currentCameraTilt = Mathf.Lerp(currentCameraTilt, targetTilt, Time.deltaTime * cameraTiltSpeed);

        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, currentCameraTilt);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void CheckEnvironment()
    {
        float rayLength = (playerCollider.height / 2f) + 0.2f;
        RaycastHit groundHit;

        if (Physics.Raycast(transform.position, Vector3.down, out groundHit, rayLength))
        {
            if (groundHit.collider.CompareTag("Ground"))
            {
                isGrounded = true;
                jumpCount = 0; // На земле модуль полностью готов
                isWallRunning = false;
            }
            else
            {
                isGrounded = false;
            }
        }
        else
        {
            isGrounded = false;
        }

        RaycastHit wallHit;
        bool hitSomethingSide = Physics.Raycast(transform.position, transform.right, out wallHit, 1.2f) ||
                               Physics.Raycast(transform.position, -transform.right, out wallHit, 1.2f);

        if (hitSomethingSide && wallHit.collider.CompareTag("Wall") && !isGrounded && Input.GetAxis("Vertical") > 0)
        {
            isWallRunning = true;
            wallNormal = wallHit.normal;
            jumpCount = 0; // Стены перезаряжают прыжковый модуль Купера
        }
        else
        {
            isWallRunning = false;
        }
    }

    private void HandleInput()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Vertical") > 0 && !isSliding)
            isSprinting = true;
        else
            isSprinting = false;

        if (Input.GetButtonDown("Jump"))
        {
            // 1. Прыжок с земли
            if (isGrounded)
            {
                Jump(jumpForce);
                if (isSliding) StopSlide();
            }
            // 2. Отскок от стены (Wall Kick)
            else if (isWallRunning)
            {
                isWallRunning = false;
                rb.useGravity = true;
                Vector3 wallKickDirection = (Vector3.up + wallNormal * 1.2f).normalized;
                rb.linearVelocity = wallKickDirection * wallKickForce;

                jumpCount = 1; // Отскок от стены считается за первый прыжок, оставляя еще один в воздухе
                if (isSliding) StopSlide();
            }
            // 3. Работа прыжкового модуля в воздухе (Двойной прыжок в любой момент)
            else if (jumpCount < maxJumps)
            {
                // Гасим скорость падения, чтобы реактивный импульс эффективно подбросил Купера вверх
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                Jump(jumpForce * 0.9f); // В воздухе прыжок чуть мягче
                if (isSliding) StopSlide();
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftControl) && !isSliding)
        {
            StartSlide();
        }
        if (Input.GetKeyUp(KeyCode.LeftControl) && isSliding)
        {
            StopSlide();
        }
    }

    private void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (isSliding)
        {
            slideTimer -= Time.fixedDeltaTime;
            if (slideTimer <= 0)
            {
                StopSlide();
            }

            rb.AddForce(-rb.linearVelocity.normalized * walkSpeed * 0.2f, ForceMode.Acceleration);
        }
        else if (isWallRunning)
        {
            rb.useGravity = false;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Physics.gravity.y * wallRunGravityModifier, rb.linearVelocity.z);

            Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
            if (Vector3.Dot(transform.forward, wallForward) < 0)
                wallForward = -wallForward;

            Vector3 targetWallVelocity = wallForward * wallRunSpeed;
            rb.linearVelocity = new Vector3(targetWallVelocity.x, rb.linearVelocity.y, targetWallVelocity.z);
        }
        else
        {
            rb.useGravity = true;
            float currentMoveSpeed = isSprinting ? sprintSpeed : walkSpeed;

            Vector3 targetVelocity = (transform.forward * z + transform.right * x) * currentMoveSpeed;

            if (isGrounded)
            {
                rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
            }
            else
            {
                if (targetVelocity.magnitude > 0)
                {
                    rb.AddForce(targetVelocity * airControlModifier, ForceMode.Acceleration);

                    Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                    if (horizontalVelocity.magnitude > maxSlideSpeed)
                    {
                        horizontalVelocity = horizontalVelocity.normalized * maxSlideSpeed;
                        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
                    }
                }
            }
        }
    }

    private void Jump(float force)
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, force, rb.linearVelocity.z);
        jumpCount++; // Засчитываем использование модуля
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = maxSlideTime;
        playerCollider.height = originalHeight * 0.5f;

        float currentSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        float boost = Mathf.Clamp(currentSpeed * slideSpeedMultiplier, sprintSpeed * 1.2f, maxSlideSpeed);

        rb.linearVelocity = transform.forward * boost;
    }

    private void StopSlide()
    {
        isSliding = false;
        playerCollider.height = originalHeight;
    }
}
