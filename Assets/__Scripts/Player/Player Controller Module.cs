using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerModule : NetworkBehaviour
{
    [HideInInspector] public Controls PlayerInput;

    [Header("Movement")]
    public float RunSpeed = 7.5f;
    public float SprintSpeed = 11.5f;
    public float GroundAcceleration = 20f;
    public float AirAcceleration = 5f;
    public float Friction = 60f;
    public float Gravity = 20f;
    public float JumpHeight = 1.5f;

    [Header("Perfect Jump")]
    public float PerfectJumpSpeedBonus = 2f;
    public float PerfectJumpMaxSpeed = 10f;
    public float PerfectJumpAcceleration = 10f;
    public float PerfectJumpDecceleration = 60f;
    public float PerfectJumpThreshold = 0.15f;

    private float PerfectJumpCurrentAccelleration = 0f;
    private bool WasGrounded = true;
    private bool HasJumpedOnce = false;

    [Header("Look")]
    public float LookSensitivity = 0.2f;
    public float LookYLimit = 85f;

    [Header("Transform Components")]
    [SerializeField] private Transform Body;
    [SerializeField] private Transform HeadPivot;
    [SerializeField] private Transform CameraParent;
    [SerializeField] private GameObject TPRoot;

    private CharacterController CC;
    private Camera PlayerCamera;

    private float LookYaw;
    private float LookPitch;
    private quaternion TargetRotation;
    private Vector3 Velocity;

    private bool WishJump = false;
    private float WishJumpTimer;
    private const float WishJumpTime = 0.2f;

    [HideInInspector] public bool CanMove = false;
    [HideInInspector] public bool CanSprint = false;

    public void Init()
    {
        TPRoot.SetActive(false);
        PlayerCamera = Camera.main;
        PlayerCamera.transform.SetPositionAndRotation(CameraParent.transform.position, CameraParent.transform.rotation);
        PlayerCamera.transform.SetParent(CameraParent.transform);

        CC = GetComponent<CharacterController>();
        PlayerInput = new Controls();
        PlayerInput.Enable();
        PlayerInput.UI.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        CanMove = true;
    }

    private void Update()
    {
        if (!CanMove) return;

        float dt = Time.deltaTime;
        if(PlayerCamera!=null && !Cursor.visible)
        {
            UpdateCamera(dt);
        }
        if (CC.enabled)
        {
            UpdateMovement(dt);
        }
    }

    private void UpdateCamera(float dt)
    {
        Vector2 lookInput = PlayerInput.Player.Look.ReadValue<Vector2>();
        LookYaw += lookInput.x * LookSensitivity * dt;
        LookPitch -= lookInput.y * LookSensitivity * dt;
        LookPitch = math.clamp(LookPitch, -math.radians(LookYLimit), math.radians(LookYLimit));
        quaternion yawRotation = quaternion.RotateY(LookYaw);
        quaternion pitchRotation = quaternion.RotateX(LookPitch);
        TargetRotation = math.mul(yawRotation, pitchRotation);
        CameraParent.transform.rotation = TargetRotation;

        quaternion yRotation = quaternion.RotateY(LookYaw);
        HeadPivot.rotation = yRotation;
        Body.rotation = yRotation;
    }
    private void UpdateMovement(float dt)
    {
        Vector2 moveInput = PlayerInput.Player.Move.ReadValue<Vector2>();
        bool sprint = CanSprint && PlayerInput.Player.Sprint.IsPressed();
        float wishSpeed = sprint ? SprintSpeed : RunSpeed;

        SetWishJump(PlayerInput.Player.Jump.WasPressedThisFrame(), ref WishJump, ref WishJumpTimer, WishJumpTime, dt);

        bool isGrounded = CC.isGrounded;
        bool justLanded = isGrounded && !WasGrounded;

        Vector3 wishDir = GetWishDirection(moveInput);
        Vector3 horizontal = new(Velocity.x, 0f, Velocity.z);

        // Determine effective cap and acceleration before clamping
        bool hasPerfectJumpBoost = PerfectJumpCurrentAccelleration > 0f;
        float effectiveCap = wishSpeed + PerfectJumpCurrentAccelleration;
        float effectiveAccel = hasPerfectJumpBoost ? PerfectJumpAcceleration : GroundAcceleration;

        if (horizontal.magnitude > effectiveCap)
            horizontal = horizontal.normalized * effectiveCap;

        if (isGrounded)
        {
            horizontal = Accelerate(wishDir, effectiveCap, effectiveAccel, horizontal, dt);
            horizontal = ApplyFriction(horizontal, Friction, dt);
        }
        else
        {
            horizontal = Accelerate(wishDir, effectiveCap, AirAcceleration, horizontal, dt);
        }

        float vertY = Velocity.y;
        vertY = ApplyGravity(vertY, isGrounded, dt);

        bool nearGround = Physics.Raycast(transform.position - new Vector3(0,CC.height /2), Vector3.down, PerfectJumpThreshold);
        Debug.DrawRay(transform.position - new Vector3(0, CC.height / 2), Vector3.down * PerfectJumpThreshold, nearGround ? Color.green : Color.red);
        bool isPerfectJump = justLanded && WishJump && horizontal.magnitude > 0.1f && nearGround && HasJumpedOnce;

        if (isPerfectJump)
        {
            PerfectJumpCurrentAccelleration = Mathf.Min(PerfectJumpCurrentAccelleration + PerfectJumpSpeedBonus, PerfectJumpMaxSpeed - wishSpeed);
        }
        else if (justLanded && !WishJump)
        {
            HasJumpedOnce = false;
            PerfectJumpCurrentAccelleration = Mathf.Max(0f, PerfectJumpCurrentAccelleration - PerfectJumpSpeedBonus / PerfectJumpDecceleration);
        }

        vertY = TryJump(vertY, ref WishJump, isGrounded);

        Velocity = new Vector3(horizontal.x, vertY, horizontal.z);
        CC.Move(Velocity * dt);

        WasGrounded = isGrounded;
    }
    private Vector3 GetWishDirection(Vector2 moveInput)
    {
        Vector3 forward = Body.TransformDirection(Vector3.forward);
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f) forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 wishDir = forward * moveInput.y + right * moveInput.x;
        return wishDir == Vector3.zero ? Vector3.zero : wishDir.normalized;
    }
    private static Vector3 Accelerate(Vector3 wishDir, float wishSpeed, float accel, Vector3 vel, float dt)
    {
        float projected = Vector3.Dot(wishDir, vel);
        float addSpeed = wishSpeed - projected;
        if (addSpeed <= 0f) return vel;

        float totalAccel = Mathf.Min(dt * accel * wishSpeed, addSpeed);
        return vel + totalAccel * wishDir;
    }
    private static Vector3 ApplyFriction(Vector3 vel, float friction, float dt)
    {
        float speed = vel.magnitude;
        if (speed <= 0f) return Vector3.zero;

        float newSpeed = Mathf.Max(speed - friction * dt, 0f);
        return vel * (newSpeed / speed);
    }
    private float ApplyGravity(float vertY, bool isGrounded, float dt)
    {
        if (!isGrounded) return vertY - Gravity * dt;
        return vertY < 0f ? 0f : vertY;
    }
    private void SetWishJump(bool jumpPressed, ref bool wishJump, ref float wishJumpTimer, float wishJumpTime, float dt)
    {
        if (jumpPressed)
        {
            wishJump = true;
            wishJumpTimer = wishJumpTime;
        }
        if (wishJump)
        {
            wishJumpTimer -= dt;
            if (wishJumpTimer <= 0f)
            {
                wishJump = false;
            }
        }
    }
    private float TryJump(float vertY, ref bool jumpPressed, bool isGrounded)
    {
        if (jumpPressed && isGrounded)
        {
            jumpPressed = false;
            HasJumpedOnce = true;
            return Mathf.Sqrt(2f * Gravity * JumpHeight);
        }
        return vertY;
    }
}