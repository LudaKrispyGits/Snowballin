using UnityEngine;
using UnityEngine.InputSystem;

namespace Snowballin
{
    /// <summary>
    /// Drives the ball: downhill target speed (distance + size driven), left/right steering, jump.
    /// Motion is velocity-controlled so the speed ramp and ramps stay tunable.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SnowballBody))]
    public class SnowballController : MonoBehaviour
    {
        public InputActionAsset inputActions;
        public GameConfig config;

        Rigidbody body;
        SnowballBody snowball;
        InputAction moveAction;
        InputAction jumpAction;

        public bool IsGrounded { get; private set; }
        public Vector3 DownhillDirection { get; set; } = Vector3.forward;

        // Jump latch: one launch per physical button press, so holding jump can never re-trigger it.
        bool jumpLatch;
        bool jumpQueued;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            snowball = GetComponent<SnowballBody>();

            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.useGravity = true;

            if (inputActions != null)
            {
                InputActionMap map = inputActions.FindActionMap("Player", true);
                moveAction = map.FindAction("Move", true);
                jumpAction = map.FindAction("Jump", true);
            }
            else
            {
                Debug.LogError("[Player] SnowballController has no InputActionAsset assigned.");
            }
        }

        void OnEnable()
        {
            moveAction?.Enable();
            jumpAction?.Enable();
        }

        void OnDisable()
        {
            moveAction?.Disable();
            jumpAction?.Disable();
        }

        void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.trackGenerator != null)
            {
                DownhillDirection = GameManager.Instance.trackGenerator.DownhillDirection;
            }
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            if (jumpAction != null && jumpAction.WasPressedThisFrame() && !jumpLatch)
            {
                jumpLatch = true;
                jumpQueued = true;
            }

            // Release clears the latch; holding jump never queues a second jump.
            if (jumpAction != null && !jumpAction.IsPressed() && !jumpAction.WasPressedThisFrame())
            {
                jumpLatch = false;
            }

            if (IsGrounded)
            {
                snowball.Grow(Time.deltaTime);
            }
        }

        void FixedUpdate()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            Vector3 forward = DownhillDirection.sqrMagnitude > 0.01f ? DownhillDirection : Vector3.forward;
            Vector3 right = Vector3.right;

            UpdateGroundedState();

            // The queued jump is consumed here so it always sees a fresh ground check.
            if (jumpQueued)
            {
                jumpQueued = false;
                if (IsGrounded) LaunchJump(gm);
            }

            Vector3 velocity = body.linearVelocity;
            float inputX = moveAction != null ? moveAction.ReadValue<Vector2>().x : 0f;
            float targetForward = gm.CurrentForwardSpeed;
            float targetLateral = inputX * gm.Upgrades.LateralSpeed;

            // Grounded: the ball tracks the slope, so forward carries the descent with it.
            // Airborne: horizontal motion is flat and the ballistic Y is left entirely to physics —
            // otherwise the slope's downward component would be re-added every frame and eat the jump.
            Vector3 motionForward = IsGrounded ? forward : new Vector3(forward.x, 0f, forward.z).normalized;
            Vector3 motionRight = IsGrounded ? right : new Vector3(right.x, 0f, right.z).normalized;

            float forwardSpeed = Vector3.Dot(velocity, motionForward);
            float lateralSpeed = Vector3.Dot(velocity, motionRight);

            float newForward = Mathf.MoveTowards(forwardSpeed, targetForward, config.forwardAcceleration * Time.fixedDeltaTime);
            float newLateral = Mathf.MoveTowards(lateralSpeed, targetLateral, config.lateralAcceleration * Time.fixedDeltaTime);

            body.linearVelocity = motionForward * newForward + motionRight * newLateral + Vector3.up * velocity.y;

            // Keep the ball's whole sphere inside the white border walls: the limit shrinks as the ball grows.
            // The walls have real colliders; this clamp is only a safety net behind them.
            float limit = Mathf.Min(config.lateralLimit, config.trackWidth * 0.5f - config.wallThickness - snowball.Radius - 0.1f);
            limit = Mathf.Max(0.5f, limit);

            Vector3 position = body.position;
            if (position.x < -limit || position.x > limit)
            {
                position.x = Mathf.Clamp(position.x, -limit, limit);
                body.position = position;
                Vector3 clamped = body.linearVelocity;
                body.linearVelocity = new Vector3(0f, clamped.y, clamped.z);
            }
        }

        void LaunchJump(GameManager gm)
        {
            // Never stack upward velocity: an airborne ball (or one already rising) ignores the launch.
            if (body.linearVelocity.y > 0.5f) return;

            // Jump is authored as a height in metres: v = sqrt(2 * g * h), so the run's gravity is baked in.
            float jumpHeight = gm.Upgrades.JumpHeight;
            float launch = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);

            Vector3 velocity = body.linearVelocity;
            velocity.y = launch;
            body.linearVelocity = velocity;
            IsGrounded = false;
            Debug.Log($"[Player] Jump {jumpHeight:F2} m high -> launch velocity {launch:F2} m/s (gravity {Physics.gravity.y:F1})");
        }

        void UpdateGroundedState()
        {
            float radius = snowball.Radius;
            bool hitTrack = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, radius + 0.35f,
                ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<TrackChunk>() != null;

            IsGrounded = hitTrack && body.linearVelocity.y <= 0.5f;
        }
    }
}
