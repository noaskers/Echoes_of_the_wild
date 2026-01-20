using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimplePlayer : MonoBehaviour
{
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;
    public float acceleration = 12f;
    public float deceleration = 16f;
    public float mouseSensitivity = 2f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public float cameraBobAmount = 0.08f;
    public float cameraBobFrequency = 8f;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private Camera playerCamera;
    private Vector3 currentMove = Vector3.zero;
    private float currentSpeed = 0f;
    private float bobTimer = 0f;
    private Vector3 cameraInitialLocalPos;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            Debug.LogError("No camera found in children of player! Add a Camera child.");
        else
            cameraInitialLocalPos = playerCamera.transform.localPosition;
    }

    void Update()
    {
        if (playerCamera == null) return;

        // --- Mouse Look ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // --- Movement Input ---
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = (transform.right * x + transform.forward * z).normalized;
        bool isMoving = inputDir.magnitude > 0.1f;
        bool isSprinting = isMoving && Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // --- Acceleration/Deceleration ---
        if (isMoving)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }
        currentMove = inputDir * currentSpeed;

        // --- Gravity & Jumping ---
        bool onGround = controller.isGrounded;
        if (onGround && velocity.y < 0f)
            velocity.y = -2f; // stick to ground

        if (Input.GetButtonDown("Jump") && onGround)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        if (!onGround)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        Vector3 move = new Vector3(currentMove.x, 0f, currentMove.z);
        controller.Move(move * Time.deltaTime + velocity * Time.deltaTime);

        // --- Camera Bobbing ---
        if (isMoving && onGround)
        {
            bobTimer += Time.deltaTime * cameraBobFrequency * (isSprinting ? 1.5f : 1f);
            float bobOffset = Mathf.Sin(bobTimer) * cameraBobAmount * (isSprinting ? 1.3f : 1f);
            playerCamera.transform.localPosition = cameraInitialLocalPos + new Vector3(0f, bobOffset, 0f);
        }
        else
        {
            bobTimer = 0f;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, cameraInitialLocalPos, 8f * Time.deltaTime);
        }
    }

    // No water: always return very low value
    float GetWaterHeight(Vector3 position) { return -1000f; }
}