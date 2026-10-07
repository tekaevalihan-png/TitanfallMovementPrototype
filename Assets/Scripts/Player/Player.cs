using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _speed = 5f;
    [Range(0f, 1f)]
    [SerializeField] private float _smoothTime = 0.15f;
    [SerializeField] private GameObject _camera;
    [SerializeField] private float _mouseSensitivity = 2f;

    [Header("Jump Settings")]
    [SerializeField] private float _jumpForce = 3f;
    [SerializeField] private string _groundTag = "Ground";

    private Vector2 _dir;
    private Vector3 _normalOfWall;
    private float _yaw, _pitch;
    private bool _isGrounded, _wallRun, _wallRight;
    private Rigidbody _rb;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void OnMove(InputAction.CallbackContext context) =>
        _dir = context.ReadValue<Vector2>();

    public void OnLook(InputAction.CallbackContext context)
    {
        Vector2 mouse = context.ReadValue<Vector2>();
        _yaw += mouse.x * _mouseSensitivity * 0.1f;
        _pitch -= mouse.y * _mouseSensitivity * 0.1f;
        _pitch = Mathf.Clamp(_pitch, -80f, 80f);

        if(!_wallRun) _rb.MoveRotation(Quaternion.Euler(0, _yaw, 0));
        _camera.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && _isGrounded)
        {
            _rb.AddForce(Vector3.up * Mathf.Sqrt(2 * 9.81f * _jumpForce), ForceMode.Impulse);
        }
    }

    private void Update()
    {
        //WallRun
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.right, out hit, 1f))
        {
            if (!_isGrounded)
            {
                _normalOfWall = hit.normal;
                _wallRight = true;
                if (_rb.linearVelocity.sqrMagnitude > 1f) _wallRun = true;
                return;
            }
        }

        if (Physics.Raycast(transform.position, -transform.right, out hit, 1f))
        {
            if (!_isGrounded)
            {
                _normalOfWall = hit.normal;
                _wallRight = false;

                if (_rb.linearVelocity.sqrMagnitude > 1f) _wallRun = true;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag(_groundTag))
        {
            _isGrounded = true;
        }

        if (collision.gameObject.CompareTag("Wall") && _wallRun)
            _rb.AddForce(-_normalOfWall * 25);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Wall") && _wallRun)
        {
            _rb.useGravity = false;

            if (_wallRight)
            {
                float i = 0f;
                Quaternion a = transform.rotation;

                StartCoroutine(SmoothRotateToZ(15f));
            }
            else
            {
                float i = 0f;
                Quaternion a = transform.rotation;

                StartCoroutine(SmoothRotateToZ(-15f));
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall") && _wallRun)
        {
            _rb.useGravity = true;
            _wallRun = false;

            StartCoroutine(SmoothRotateToZ(0f));
        }

        if (collision.gameObject.CompareTag(_groundTag)) _isGrounded = false;
    }

    private IEnumerator SmoothRotateToZ(float z)
    {
        float i = 0f;

        Quaternion targetRotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x,
            transform.rotation.eulerAngles.y,
            z
        );

        Quaternion a = transform.rotation;

        while (i < 1f)
        {
            transform.rotation = Quaternion.Slerp(a, targetRotation, i);

            i += Time.deltaTime * 2.0f;
            yield return null;
        }

        transform.rotation = targetRotation;
    }

    private void FixedUpdate()
    {
        Vector3 targetDir = (transform.forward * _dir.y + transform.right * _dir.x).normalized;
        Vector3 targetVelocity = targetDir * _speed;

        float newX = Mathf.Lerp(_rb.linearVelocity.x, targetVelocity.x, _smoothTime);
        float newZ = Mathf.Lerp(_rb.linearVelocity.z, targetVelocity.z, _smoothTime);

        _rb.linearVelocity = new Vector3(newX, _rb.linearVelocity.y, newZ);
    }
}