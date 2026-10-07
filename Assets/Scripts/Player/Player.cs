using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _speed = 5f;
    [SerializeField] private GameObject _camera;
    [SerializeField] private float _mouseSensivity = 2f;
    [Header("Jump Settings", order = 1)]
    [SerializeField] private float _jumpForce = 3f;
    [SerializeField] private string _groundTag = "Ground";
    private Vector2 _dir;
    private float _yaw, _pitch;
    private bool _isGrounded;
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
        _yaw += mouse.x * _mouseSensivity * Time.deltaTime;
        _pitch -= mouse.y * _mouseSensivity * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, -90, 90);
        transform.rotation = Quaternion.Euler(0, _yaw, 0);
        _camera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);
    }


    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (_isGrounded)
            {
                _rb.AddForce(Vector3.up * Mathf.Sqrt(2 * 9.81f * _jumpForce), ForceMode.Impulse);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if(collision.gameObject.CompareTag(_groundTag))
        {
            _isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(_groundTag))
        {
            _isGrounded = false;
        }
    }

    private void FixedUpdate()
    {
        Vector3 movement = new Vector3(_dir.x, 0, _dir.y).normalized * _speed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + transform.TransformDirection(movement));
    }
}