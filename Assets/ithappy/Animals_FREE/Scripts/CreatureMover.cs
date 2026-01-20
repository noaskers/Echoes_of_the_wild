using System;
using UnityEngine;

namespace ithappy.Animals_FREE
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public class CreatureMover : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float m_WalkSpeed = 1f; // meters/sec
        [SerializeField] private float m_RunSpeed = 4f;  // meters/sec
        [SerializeField, Range(0f, 360f)] private float m_RotateSpeed = 90f; // deg/sec
        [SerializeField] private Space m_Space = Space.Self;
        [SerializeField] private float m_JumpHeight = 0f; // not used currently, kept for API compatibility

        [Header("Animator")]
        [SerializeField] private string m_VerticalID = "Vert";
        [SerializeField] private string m_StateID = "State";
        [SerializeField] private LookWeight m_LookWeight = new LookWeight(1f, 0.3f, 0.7f, 1f);

        [Header("Auto Movement / Wandering")]
        [SerializeField] private bool m_AutoMove = true;
        [SerializeField] private float m_MinIdleTime = 2f;
        [SerializeField] private float m_MaxIdleTime = 4f;
        [SerializeField] private float m_MinMoveTime = 2f;
        [SerializeField] private float m_MaxMoveTime = 4f;
        [SerializeField] private float m_WanderRadius = 5f;

        private Transform m_Transform;
        private CharacterController m_Controller;
        private Animator m_Animator;

        private MovementHandler m_Movement;
        private AnimationHandler m_Animation;

        private Vector2 m_Axis;
        private Vector3 m_Target;
        private bool m_IsRun;
        private bool m_IsMoving;

        private float m_StateTimer;
        private enum AutoState { Idle, Move }
        private AutoState m_AutoState = AutoState.Idle;

        public Vector2 Axis => m_Axis;
        public Vector3 Target => m_Target;
        public bool IsRun => m_IsRun;

        private void OnValidate()
        {
            m_WalkSpeed = Mathf.Max(0f, m_WalkSpeed);
            m_RunSpeed = Mathf.Max(m_WalkSpeed, m_RunSpeed);
            m_RotateSpeed = Mathf.Clamp(m_RotateSpeed, 0f, 360f);

            if (m_Movement != null)
            {
                m_Movement.SetStats(m_WalkSpeed, m_RunSpeed, m_RotateSpeed, m_JumpHeight, m_Space);
            }
        }

        private void Awake()
        {
            m_Transform = transform;
            m_Controller = GetComponent<CharacterController>();
            m_Animator = GetComponent<Animator>();

            m_Movement = new MovementHandler(m_Controller, m_Transform, m_WalkSpeed, m_RunSpeed, m_RotateSpeed, m_JumpHeight, m_Space);
            m_Animation = new AnimationHandler(m_Animator, m_VerticalID, m_StateID);

            m_StateTimer = UnityEngine.Random.Range(m_MinIdleTime, m_MaxIdleTime);

            // initialize target to current forward point
            m_Target = m_Transform.position + m_Transform.forward * 2f;
        }

        private void Update()
        {
            // Defensive: ensure handlers are present
            if (m_Transform == null) m_Transform = transform;
            if (m_Controller == null) m_Controller = GetComponent<CharacterController>();
            if (m_Animator == null) m_Animator = GetComponent<Animator>();
            if (m_Movement == null && m_Controller != null && m_Transform != null)
                m_Movement = new MovementHandler(m_Controller, m_Transform, m_WalkSpeed, m_RunSpeed, m_RotateSpeed, m_JumpHeight, m_Space);
            if (m_Animation == null && m_Animator != null)
                m_Animation = new AnimationHandler(m_Animator, m_VerticalID, m_StateID);

            if (m_Movement == null || m_Animation == null)
            {
                Debug.LogError("[CreatureMover] Missing required components or handlers. Disabling script.", this);
                enabled = false;
                return;
            }

            // Auto movement decision-making
            if (m_AutoMove)
            {
                // First try to avoid obstacles ahead by picking a new direction
                if (IsObstacleAhead())
                {
                    bool found = false;
                    for (int i = 0; i < 6; i++)
                    {
                        var dir2D = UnityEngine.Random.insideUnitCircle.normalized;
                        Vector3 testDir = new Vector3(dir2D.x, 0f, dir2D.y);
                        if (!IsObstacleInDirection(testDir))
                        {
                            m_Axis = new Vector2(dir2D.x, dir2D.y);
                            m_Target = m_Transform.position + testDir * m_WanderRadius;
                            m_IsRun = UnityEngine.Random.value > 0.6f;
                            m_IsMoving = true;
                            m_AutoState = AutoState.Move;
                            m_StateTimer = UnityEngine.Random.Range(m_MinMoveTime, m_MaxMoveTime);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // all directions blocked: idle briefly
                        m_Axis = Vector2.zero;
                        m_IsMoving = false;
                        m_StateTimer = UnityEngine.Random.Range(m_MinIdleTime, m_MaxIdleTime);
                        m_AutoState = AutoState.Idle;
                    }
                }
                else
                {
                    AutoMoveLogic(Time.deltaTime);
                }
            }

            // Let Movement Handler compute displacement and animation axis
            m_Movement.Move(Time.deltaTime, in m_Axis, in m_Target, m_IsRun, m_IsMoving, out var animAxis, out var isAir);
            m_Animation.Animate(in animAxis, m_IsRun ? 1f : 0f, Time.deltaTime);
        }

        // Returns true if a prop/obstacle is detected in a given direction (within 1m)
        private bool IsObstacleInDirection(Vector3 dir)
        {
            float checkDistance = 1.0f;
            float checkRadius = 0.5f;
            Vector3 origin = m_Transform.position + Vector3.up * 0.5f;
            int propLayer = LayerMask.NameToLayer("Props");
            int mask = (propLayer >= 0) ? (1 << propLayer) : ~0;
            RaycastHit hit;
            if (Physics.SphereCast(origin, checkRadius, dir.normalized, out hit, checkDistance, mask))
            {
                return hit.collider != null;
            }
            return false;
        }

        // Returns true if a prop is detected ahead (within 1m)
        private bool IsObstacleAhead()
        {
            float checkDistance = 1.0f;
            float checkRadius = 0.5f;
            Vector3 origin = m_Transform.position + Vector3.up * 0.5f;
            Vector3 dir = m_Transform.forward;
            int propLayer = LayerMask.NameToLayer("Props");
            int mask = (propLayer >= 0) ? (1 << propLayer) : ~0;
            RaycastHit hit;
            if (Physics.SphereCast(origin, checkRadius, dir.normalized, out hit, checkDistance, mask))
            {
                return hit.collider != null;
            }
            return false;
        }

        private void OnAnimatorIK()
        {
            if (m_Animation != null)
                m_Animation.AnimateIK(in m_Target, m_LookWeight);
        }

        // **************************************
        //       AUTO WANDERING LOGIC
        // **************************************
        private void AutoMoveLogic(float deltaTime)
        {
            m_StateTimer -= deltaTime;

            if (m_StateTimer <= 0f)
            {
                if (m_AutoState == AutoState.Idle)
                {
                    // SWITCH TO WALK/RUN
                    m_AutoState = AutoState.Move;
                    ChooseRandomMovement();
                    m_StateTimer = UnityEngine.Random.Range(m_MinMoveTime, m_MaxMoveTime);
                }
                else
                {
                    // SWITCH TO IDLE
                    m_AutoState = AutoState.Idle;
                    m_Axis = Vector2.zero;
                    m_IsRun = false;
                    m_IsMoving = false;
                    m_StateTimer = UnityEngine.Random.Range(m_MinIdleTime, m_MaxIdleTime);
                }
            }

            m_IsMoving = m_Axis.sqrMagnitude > 0.01f;
        }

        private void ChooseRandomMovement()
        {
            // Pick random direction
            var dir2D = UnityEngine.Random.insideUnitCircle.normalized;
            m_Axis = dir2D;

            // Pick random target point
            Vector3 randomPos = m_Transform.position + new Vector3(dir2D.x, 0f, dir2D.y) * m_WanderRadius;
            m_Target = randomPos;

            // 40% chance to run
            m_IsRun = UnityEngine.Random.value > 0.6f;
            m_IsMoving = m_Axis.sqrMagnitude > 0.01f;
        }

        public void SetInput(in Vector2 axis, in Vector3 target, in bool isRun, in bool isJump)
        {
            // Still allow manual control if needed
            m_Axis = axis;
            m_Target = target;
            m_IsRun = isRun;

            if (m_Axis.sqrMagnitude < Mathf.Epsilon)
            {
                m_Axis = Vector2.zero;
                m_IsMoving = false;
            }
            else
            {
                m_Axis = Vector2.ClampMagnitude(m_Axis, 1f);
                m_IsMoving = true;
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > m_Controller.stepOffset)
                m_Movement.SetSurface(hit.normal);
        }

        [Serializable]
        private struct LookWeight
        {
            public float weight, body, head, eyes;
            public LookWeight(float w, float b, float h, float e)
            {
                weight = w; body = b; head = h; eyes = e;
            }
        }

        // ---------------------------------------------------------
        // MOVEMENT HANDLER
        // ---------------------------------------------------------
        private class MovementHandler
        {
            private readonly CharacterController m_Controller;
            private Transform m_Transform;
            private float m_WalkSpeed, m_RunSpeed, m_RotateSpeed;
            private Space m_Space;

            private Vector3 m_Normal = Vector3.up;

            private float m_VerticalVelocity = 0f;
            private const float k_TerminalVelocity = -50f;

            private Vector3 m_LastMovementForward = Vector3.forward;

            public MovementHandler(CharacterController controller, Transform transform, float walkSpeed, float runSpeed, float rotateSpeed, float jumpHeight, Space space)
            {
                m_Controller = controller;
                m_Transform = transform;
                m_WalkSpeed = walkSpeed;
                m_RunSpeed = runSpeed;
                m_RotateSpeed = rotateSpeed;
                m_Space = space;
            }

            public void SetStats(float walkSpeed, float runSpeed, float rotateSpeed, float jumpHeight, Space space)
            {
                m_WalkSpeed = walkSpeed;
                m_RunSpeed = runSpeed;
                m_RotateSpeed = rotateSpeed;
                m_Space = space;
            }

            public void SetSurface(in Vector3 normal) => m_Normal = normal;

            /// <summary>
            /// Moves the character controller and produces animation axis output.
            /// </summary>
            public void Move(float deltaTime, in Vector2 axis, in Vector3 target, bool isRun, bool isMoving, out Vector2 animAxis, out bool isAir)
            {
                // Compute camera/target look direction for "Space.Self" behaviour
                Vector3 lookDir = Vector3.zero;
                if ((target - m_Transform.position).sqrMagnitude > 0.001f)
                    lookDir = (target - m_Transform.position).normalized;
                else
                    lookDir = m_Transform.forward;

                // Convert axis to world movement vector (projected on surface)
                ConvertMovement(in axis, in lookDir, out var movement);

                if (movement.sqrMagnitude > 0.0001f)
                    m_LastMovementForward = movement.normalized;

                // Gravity & vertical motion
                CalculateGravity(deltaTime, out isAir);

                // Apply movement and gravity
                Displace(deltaTime, in movement, isRun);

                // Rotation
                Turn(in m_LastMovementForward, isMoving, deltaTime);
                // Animation axis generation
                GenAnimationAxis(in movement, out animAxis);
            }

            private void ConvertMovement(in Vector2 axis, in Vector3 targetForward, out Vector3 movement)
            {
                Vector3 forward;
                if (m_Space == Space.Self)
                {
                    forward = new Vector3(targetForward.x, 0f, targetForward.z);
                    if (forward.sqrMagnitude < 0.001f) forward = m_Transform.forward;
                    forward = forward.normalized;
                }
                else
                {
                    forward = Vector3.forward;
                }

                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                movement = axis.x * right + axis.y * forward;
                movement = Vector3.ProjectOnPlane(movement, m_Normal);
                // keep magnitude up to 1
                movement = Vector3.ClampMagnitude(movement, 1f);
            }

            private void Displace(float deltaTime, in Vector3 movement, bool isRun)
            {
                float speed = isRun ? m_RunSpeed : m_WalkSpeed;
                Vector3 horizontal = movement * speed;
                Vector3 total = horizontal + new Vector3(0f, m_VerticalVelocity, 0f);
                m_Controller.Move(total * deltaTime);
            }

            private void CalculateGravity(float deltaTime, out bool isAir)
            {
                if (m_Controller.isGrounded)
                {
                    // small negative to keep controller grounded
                    if (m_VerticalVelocity < 0f)
                        m_VerticalVelocity = -1f;
                    isAir = false;
                }
                else
                {
                    m_VerticalVelocity += Physics.gravity.y * deltaTime;
                    if (m_VerticalVelocity < k_TerminalVelocity)
                        m_VerticalVelocity = k_TerminalVelocity;
                    isAir = true;
                }
            }

            private void Turn(in Vector3 targetForward, bool isMoving, float deltaTime)
            {
                if (!isMoving || targetForward.sqrMagnitude < 0.0001f)
                    return;

                Quaternion current = m_Transform.rotation;
                Quaternion targetRot = Quaternion.LookRotation(new Vector3(targetForward.x, 0f, targetForward.z).normalized, Vector3.up);
                m_Transform.rotation = Quaternion.RotateTowards(current, targetRot, m_RotateSpeed * deltaTime);
            }

            private void GenAnimationAxis(in Vector3 movement, out Vector2 animAxis)
            {
                // Convert world movement into local space for animation blend parameters
                Vector3 local = m_Transform.InverseTransformDirection(movement);
                animAxis = new Vector2(local.x, local.z);
                // clamp to -1..1
                animAxis = Vector2.ClampMagnitude(animAxis, 1f);
            }
        }

        // ---------------------------------------------------------
        // ANIMATION HANDLER
        // ---------------------------------------------------------
        private class AnimationHandler
        {
            private readonly Animator m_Animator;
            private readonly string m_VerticalID;
            private readonly string m_StateID;

            private readonly float k_InputFlow = 4.5f;
            private float m_FlowState;
            private Vector2 m_FlowAxis;

            public AnimationHandler(Animator animator, string verticalID, string stateID)
            {
                m_Animator = animator;
                m_VerticalID = verticalID;
                m_StateID = stateID;
            }

            public void Animate(in Vector2 axis, float state, float deltaTime)
            {
                // Smooth axis and state
                m_FlowAxis = Vector2.Lerp(m_FlowAxis, axis, Mathf.Clamp01(k_InputFlow * deltaTime));
                m_FlowState = Mathf.Lerp(m_FlowState, state, Mathf.Clamp01(k_InputFlow * deltaTime));

                if (!string.IsNullOrEmpty(m_VerticalID))
                    m_Animator.SetFloat(m_VerticalID, m_FlowAxis.magnitude);

                if (!string.IsNullOrEmpty(m_StateID))
                    m_Animator.SetFloat(m_StateID, Mathf.Clamp01(m_FlowState));
            }

            public void AnimateIK(in Vector3 target, in LookWeight lookWeight)
            {
                if (m_Animator == null) return;
                m_Animator.SetLookAtPosition(target);
                m_Animator.SetLookAtWeight(lookWeight.weight, lookWeight.body, lookWeight.head, lookWeight.eyes);
            }
        }
    }
}
