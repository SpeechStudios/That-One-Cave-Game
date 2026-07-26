using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerModule : NetworkBehaviour, IMoveable
{
    public PlayerLoadoutModule LoadoutModule;
    public struct ReplicationData : IReplicateData
    {
        public readonly Vector2 MoveInput;
        public readonly float LookYaw;
        public readonly bool Jump;

        public Vector3 KnockbackVelocity;
        public AbilityState AbilityState;
        public float ImmobilizeTimer;

        private uint Tick;

        public ReplicationData(Vector2 moveInput, float lookYaw, bool jump, Vector3 knockbackVelocity, AbilityState state, float immobilizeTimer)
        {
            MoveInput = moveInput;
            LookYaw = lookYaw;
            Jump = jump;
            KnockbackVelocity = knockbackVelocity;
            AbilityState = state;
            ImmobilizeTimer = immobilizeTimer;
            Tick = 0;
        }
        public readonly uint GetTick() => Tick;
        public void SetTick(uint value) => Tick = value;
        public void Dispose() { }
    }
    public struct ReconciliationData : IReconcileData
    {
        public readonly Vector3 Position;
        public readonly Vector3 Velocity;
        public readonly bool IsGrounded;
        public readonly bool WasGrounded;
        public readonly bool HasJumpedOnce;
        public readonly bool WishJump;
        public readonly float WishJumpTimer;
        public readonly float PerfectJumpAcceleration;
        public readonly float LookYaw;

        public readonly bool AbilityOverrideActive;
        public readonly bool IsPrimaryAbility;
        public readonly uint ActiveAbilityStartTick;

        public readonly float ImmobilizeTimer;

        private uint Tick;

        public ReconciliationData(Vector3 position, Vector3 velocity,
            bool isGrounded, bool wasGrounded, bool hasJumpedOnce,
            bool wishJump, float wishJumpTimer, float perfectJumpAccel, float lookYaw,
            bool abilityOverrideActive, bool isPrimaryAbility, uint activeAbilityStartTick,
            float immobilizeTimer)
        {
            Position = position;
            Velocity = velocity;
            LookYaw = lookYaw;
            IsGrounded = isGrounded;
            WasGrounded = wasGrounded;
            HasJumpedOnce = hasJumpedOnce;
            WishJump = wishJump;
            WishJumpTimer = wishJumpTimer;
            PerfectJumpAcceleration = perfectJumpAccel;
            AbilityOverrideActive = abilityOverrideActive;
            IsPrimaryAbility = isPrimaryAbility;
            ActiveAbilityStartTick = activeAbilityStartTick;
            ImmobilizeTimer = immobilizeTimer;
            Tick = 0;
        }
        public readonly uint GetTick() => Tick;
        public void SetTick(uint value) => Tick = value;
        public void Dispose() { }
    }

    [Header("Transform Components")]
    [SerializeField] internal CharacterController CC;
    [SerializeField] private Transform CameraParent;
    [SerializeField] private GameObject TPRoot;

    [Header("Movement")]
    public float RunSpeed = 7.5f;
    public float SprintSpeed = 11.5f;
    public float GroundAcceleration = 20f;
    public float AirAcceleration = 5f;
    public float Friction = 60f;
    public float Gravity = 20f;
    public float JumpHeight = 1.5f;

    [Header("Grounded")]
    public LayerMask GroundLayers;
    public float SphereCastRadius = 0.2f;
    public float SphereCastDownPosition = 0.9f;
    private readonly Collider[] GroundCheckResults = new Collider[8];
    public event System.Action OnLanded;

    [Header("Perfect Jump")]
    public float PerfectJumpSpeedBonus = 2f;
    public float PerfectJumpMaxSpeed = 10f;
    public float PerfectJumpAcceleration = 10f;
    public float PerfectJumpDeceleration = 60f;
    public float PerfectJumpThreshold = 0.15f;

    [Header("Look")]
    public float LookSensitivity = 0.5f;
    public float LookYLimit = 85f;

    private float PerfectJumpCurrentAcceleration = 0f;
    private bool WasGrounded = true;
    private bool PerfectJumpComplete = false;

    private Camera PlayerCamera;

    [HideInInspector] public Controls PlayerInput;
    private Vector2 MoveInput;
    private bool JumpInput;
    private float YawInput;
    private float PitchInput;
    private bool IsGrounded;

    private Vector3 KnockbackForce;
    private float KnockbackDecay = 4f;

    private float ImmobilizeTimer = 0f;

    private float LookYaw;
    private float LookPitch;
    private Vector3 Velocity;

    private bool WishJump = false;
    private float WishJumpTimer;
    private const float WishJumpTime = 0.2f;

    private AbilityState PendingAbility;
    private AbilityState ActiveAbility;

    private bool AbilityOverrideThisTick;

    [HideInInspector] public bool CanMove = false;
    [HideInInspector] public bool CanSprint = false;
    public void Init()
    {
        TPRoot.SetActive(false);
        PlayerCamera = Camera.main;
        PlayerCamera.transform.SetPositionAndRotation(CameraParent.transform.position, CameraParent.transform.rotation);
        PlayerCamera.transform.SetParent(CameraParent.transform);


        PlayerInput = new Controls();
        PlayerInput.Enable();
        PlayerInput.UI.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        CanMove = true;
    }
    public void BeginMovementOverride(bool isPrimary, uint currentTick)
    {
        PendingAbility = new AbilityState(true, isPrimary, currentTick);
    }
    public void ApplyKnockback(Vector3 velocity)
    {
        KnockbackForce = velocity;
    }
    public void ApplySlow(float multiplier, float duration)
    {
        throw new System.NotImplementedException();
    }
    public void ApplyImmobilize(float duration)
    {
        ImmobilizeTimer = Mathf.Max(ImmobilizeTimer, duration);
    }
    public override void OnStartNetwork()
    {
        TimeManager.OnTick += TimeManagerTickEventHandler;
        TimeManager.OnPostTick += TimeManagerPostTickEventHandler;
    }
    public override void OnStopNetwork()
    {
        TimeManager.OnTick -= TimeManagerTickEventHandler;
        TimeManager.OnPostTick -= TimeManagerPostTickEventHandler;
    }
    private void TimeManagerTickEventHandler()
    {
        if (IsOwner)
        {
            if (PendingAbility.Active)
            {
                ActiveAbility = PendingAbility;
                PendingAbility = default;
            }

            ReplicationData data = new(MoveInput, LookYaw, JumpInput, KnockbackForce, ActiveAbility, ImmobilizeTimer);
            Replicate(data);
            JumpInput = false;
        }
        else
        {
            Replicate(default);
        }
    }
    private void TimeManagerPostTickEventHandler()
    {
        CreateReconcile();
    }

    [Replicate]
    private void Replicate(ReplicationData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        float dt = (float)TimeManager.TickDelta;

        bool isImmobilized = ImmobilizeTimer > 0f;

        UpdateRotation(data.LookYaw);
        ResolveMovementAbility(ref data, state, dt);
        if (!AbilityOverrideThisTick)
        {
            Vector2 moveInput = isImmobilized ? Vector2.zero : data.MoveInput;
            bool jump = isImmobilized ? false : data.Jump;
            UpdatePosition(moveInput, data.KnockbackVelocity, jump, dt);
        }
        AbilityOverrideThisTick = false;

        ImmobilizeTimer = Mathf.Max(0f, ImmobilizeTimer - dt);

        ActiveAbility = data.AbilityState;
    }


    public void RefreshGroundedState(Vector3 currentVelocity)
    {
        Velocity = currentVelocity;
        IsGrounded = CheckGrounded();
        WasGrounded = IsGrounded;
    }
    public override void CreateReconcile()
    {
        ReconciliationData data = new(transform.position, Velocity, IsGrounded, WasGrounded, PerfectJumpComplete, WishJump, WishJumpTimer, PerfectJumpCurrentAcceleration, LookYaw, ActiveAbility.Active, ActiveAbility.IsPrimary, ActiveAbility.StartTick, ImmobilizeTimer);
        Reconcile(data);
    }
    [Reconcile]
    private void Reconcile(ReconciliationData data, Channel channel = Channel.Unreliable)
    {
        CC.enabled = false;
        transform.SetPositionAndRotation(data.Position, quaternion.RotateY(LookYaw));
        CC.enabled = true;

        Velocity = data.Velocity;

        IsGrounded = data.IsGrounded;
        WasGrounded = data.WasGrounded;
        PerfectJumpComplete = data.HasJumpedOnce;
        WishJump = data.WishJump;
        WishJumpTimer = data.WishJumpTimer;
        PerfectJumpCurrentAcceleration = data.PerfectJumpAcceleration;

        ActiveAbility = new AbilityState(data.AbilityOverrideActive, data.IsPrimaryAbility, data.ActiveAbilityStartTick);
        ImmobilizeTimer = data.ImmobilizeTimer;
    }

    private void Update()
    {
        if (!CanMove) return;

        if (PlayerCamera != null && !Cursor.visible)
        {
            SetLookInputs();
            SetCameraRotation();
        }
        if (CC.enabled)
        {
            SetMoveInputs();
        }
    }
    private void SetLookInputs()
    {
        Vector2 lookinput = PlayerInput.Player.Look.ReadValue<Vector2>();
        YawInput = lookinput.x;
        PitchInput = lookinput.y;
        LookYaw += YawInput * LookSensitivity * Time.deltaTime;
    }
    private void SetCameraRotation()
    {
        LookPitch -= PitchInput * LookSensitivity * Time.deltaTime;
        LookPitch = math.clamp(LookPitch, -math.radians(LookYLimit), math.radians(LookYLimit));
        CameraParent.localRotation = quaternion.RotateX(LookPitch);
    }
    private void SetMoveInputs()
    {
        MoveInput = PlayerInput.Player.Move.ReadValue<Vector2>();
        if (PlayerInput.Player.Jump.WasPressedThisFrame())
        {
            JumpInput = true;
        }
    }

    private void UpdateRotation(float lookYaw)
    {
        transform.rotation = quaternion.RotateY(lookYaw);
    }
    private void UpdatePosition(Vector2 moveInput, Vector3 knockbackVelocity, bool jump, float dt)
    {
        float wishSpeed = RunSpeed;

        SetWishJump(jump, ref WishJump, ref WishJumpTimer, WishJumpTime, dt);

        bool isGrounded = IsGrounded;
        bool justLanded = isGrounded && !WasGrounded;
        if (justLanded)
        {
            OnLanded?.Invoke();
        }

        Vector3 wishDir = GetWishDirection(moveInput);
        Vector3 horizontal = new(Velocity.x, 0f, Velocity.z);

        bool hasPerfectJumpBoost = PerfectJumpCurrentAcceleration > 0f;
        float effectiveCap = wishSpeed + PerfectJumpCurrentAcceleration;
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

        bool nearGround = Physics.Raycast(transform.position - new Vector3(0, CC.height / 2), Vector3.down, PerfectJumpThreshold);
        bool isPerfectJump = justLanded && WishJump && horizontal.magnitude > 0.1f && nearGround && PerfectJumpComplete;

        if (isPerfectJump)
            PerfectJumpCurrentAcceleration = Mathf.Min(PerfectJumpCurrentAcceleration + PerfectJumpSpeedBonus, PerfectJumpMaxSpeed - wishSpeed);
        else if (justLanded && !WishJump)
            PerfectJumpComplete = false;

        if (!isPerfectJump && isGrounded)
            PerfectJumpCurrentAcceleration = Mathf.Max(0f, PerfectJumpCurrentAcceleration - PerfectJumpSpeedBonus / PerfectJumpDeceleration);

        bool didJump = TryJump(ref vertY, ref WishJump, isGrounded);

        Velocity = new Vector3(horizontal.x, vertY, horizontal.z);
        if (knockbackVelocity.magnitude > 0)
        {
            KnockbackForce = knockbackVelocity;
        }
        KnockbackForce = Vector3.Lerp(KnockbackForce, Vector3.zero, KnockbackDecay * dt);
        CC.Move((Velocity + KnockbackForce) * dt);


        IsGrounded = !didJump && CheckGrounded();
        WasGrounded = isGrounded;
    }
    private void ResolveMovementAbility(ref ReplicationData data, ReplicateState state, float dt)
    {
        if (!data.AbilityState.Active) return;
        if (LoadoutModule.Weapon == null)
        {
            data.AbilityState = default;
            return;
        }

        MovementAbility ability = data.AbilityState.IsPrimary ? (LoadoutModule.Weapon.PrimaryQAbility as MovementAbility) : (LoadoutModule.Weapon.SecondaryEAbility as MovementAbility);
        uint currentTick = data.GetTick();
        float elapsed = (currentTick - data.AbilityState.StartTick) * dt;
        bool isServerTick = state.ContainsTicked() && !state.ContainsReplayed() && IsServerInitialized;
        bool isClientTick = state.ContainsTicked() && !state.ContainsReplayed() && IsClientInitialized;

        if (elapsed > ability.Duration)
        {
            data.AbilityState = default;
            if (isServerTick) ability.ServerOnMovementComplete(this);
            if (isClientTick) ability.ClientOnMovementComplete(this);
            return;
        }

        MovementAbilityResult result = ability.ExecuteMove(this, data.MoveInput, ref data.AbilityState, dt, elapsed);
        AbilityOverrideThisTick = true;
        if (result == MovementAbilityResult.Completed)
        {
            data.AbilityState = default;
            if (isServerTick) ability.ServerOnMovementComplete(this);
            if (isClientTick) ability.ClientOnMovementComplete(this);
        }
    }

    #region Movement Calculations
    internal Vector3 GetWishDirection(Vector2 moveInput)
    {
        Vector3 forward = transform.forward;
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
    private bool TryJump(ref float vertY, ref bool jumpPressed, bool isGrounded)
    {
        if (jumpPressed && isGrounded)
        {
            jumpPressed = false;
            PerfectJumpComplete = true;
            vertY = Mathf.Sqrt(2f * Gravity * JumpHeight);
            return true;
        }
        return false;
    }
    public bool CheckGrounded()
    {
        Vector3 spherePosition = transform.position + Vector3.down * SphereCastDownPosition;
        var count = Physics.OverlapSphereNonAlloc(spherePosition, SphereCastRadius, GroundCheckResults, GroundLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider col = GroundCheckResults[i];
            if (col == null) continue;
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
            return true;
        }
        return false;

    }
    #endregion


}