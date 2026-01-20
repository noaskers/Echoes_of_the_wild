using UnityEngine;
using UnityEngine.AI;
using ithappy.Animals_FREE;

public class Animal : MonoBehaviour
{
    [Header("Movement Settings")]
    public float wanderRadius = 10f;
    public float wanderTimer = 5f;
    public float moveSpeed = 2f;
    public float runSpeed = 4f;
    public float rotationSpeed = 2f;

    [Header("Animation Settings")]
    public string idleAnimation = "Idle";
    public string walkAnimation = "Walk";
    public string runAnimation = "Run";

    private Animator animator;
    private NavMeshAgent agent;
    private CreatureMover creatureMover;
    private Rigidbody rb;
    private Collider animalCollider;
    private float timer;
    private Vector3 currentDestination;
    private bool hasDestination = false;
    private bool isMoving = false;
    [Header("Debug")]
    public bool debugAI = false;
    private float debugLogTimer = 0f;

    void Start()
    {
        // Get components
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        animalCollider = GetComponent<Collider>();

        // CRITICAL: Disable any player input components immediately
        DisablePlayerInputComponents();

        // If this prefab contains an authored character mover (from ithappy),
        // we'll use it for local movement/animation. Otherwise use NavMeshAgent.
        creatureMover = GetComponent<CreatureMover>();

        // Setup physics
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        // Make the rigidbody kinematic by default so NavMeshAgent movement
        // isn't fighting the physics system and animals don't push each other.
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Setup collider if missing
        if (animalCollider == null)
        {
            animalCollider = gameObject.AddComponent<CapsuleCollider>();
        }

        // Configure NavMesh - let the animal decide if it can use it
        // If we don't have a CreatureMover, ensure a NavMeshAgent exists so
        // navigation works. If CreatureMover exists, we will drive it from AI.
        if (creatureMover == null)
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    agent = gameObject.AddComponent<NavMeshAgent>();
                }
            }
        }

        ConfigureMovement();

        // Initial state
        timer = wanderTimer;
        currentDestination = transform.position;

        // Start with idle animation and ensure animator updates even if offscreen
        if (animator != null)
        {
            // Ensure this animal has its own AnimatorController instance so
            // runtime changes to a shared controller won't affect other animators.
            if (animator.runtimeAnimatorController != null)
            {
                animator.runtimeAnimatorController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            }

            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.applyRootMotion = false;
            animator.enabled = true;
            // Reset any parameters to a known state
            if (HasAnimatorParameter("IsMoving")) animator.SetBool("IsMoving", false);
            if (HasAnimatorParameter("Speed")) animator.SetFloat("Speed", 0f);
        }
        PlayAnimation(idleAnimation);

        // Start wandering immediately
        Wander();

        // Start autonomous AI loop to pick new wander targets regularly
        StartCoroutine(AIMovementLoop());

        Debug.Log($"{gameObject.name} initialized with independent AI movement");
    }

    void DisablePlayerInputComponents()
    {
        // Disable any components that might be listening to player input
        MonoBehaviour[] allComponents = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in allComponents)
        {
            if (component != this &&
                (component.GetType().Name.ToLower().Contains("input") ||
                 component.GetType().Name.ToLower().Contains("player") ||
                 component.GetType().Name.ToLower().Contains("controller")))
            {
                component.enabled = false;
            }
        }

        // Specifically disable CharacterController
        CharacterController charController = GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false;
        }
    }

    void ConfigureMovement()
    {
        // Try to use NavMesh if available, otherwise use simple movement
        if (agent != null && agent.isOnNavMesh)
        {
            // Configure NavMesh agent
            agent.speed = moveSpeed;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;
            agent.autoBraking = true;
            // Improve local avoidance so agents don't physically push each other.
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = Random.Range(20, 80);
            // Ensure agent moves transform (agent will control position)
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
        else
        {
            // Remove NavMeshAgent if we can't use it
            if (agent != null)
            {
                Destroy(agent);
                agent = null;
            }
            // For simple transform-based movement, keep RB kinematic to avoid
            // physics pushes and allow smooth transform updates.
            if (rb != null)
                rb.isKinematic = true;
        }
    }

    void Update()
    {
        // ANIMAL'S OWN AI BEHAVIOR - NO PLAYER INPUT
        timer += Time.deltaTime;

        if (timer >= wanderTimer)
        {
            Wander();
            timer = 0f;
        }

        // If using a CreatureMover (the ithappy mover), the mover handles
        // movement and animation. Skip the NavMesh/animator updates in that case.
        if (creatureMover == null)
        {
            CheckMovementState();

            // Keep animator parameters updated every frame so animation doesn't
            // depend on other scripts (like the player's animator).
            UpdateAnimatorParameters();
        }

        // Optional debug: confirm Update runs even when player is idle
        if (debugAI)
        {
            debugLogTimer -= Time.deltaTime;
            if (debugLogTimer <= 0f)
            {
                Debug.Log($"[Animal] {gameObject.name} Update running. isMoving={isMoving} hasDestination={hasDestination} timer={timer:F2}");
                debugLogTimer = 1f; // log at most once per second
            }
        }

        // Handle simple transform movement only if we're not using CreatureMover
        // and don't have a navmesh agent.
        if (creatureMover == null && agent == null && hasDestination)
        {
            MoveToDestinationSimple();
        }
    }

    void UpdateAnimatorParameters()
    {
        if (animator == null) return;

        float speed = 0f;
        if (agent != null && agent.isOnNavMesh)
        {
            speed = agent.velocity.magnitude;
        }
        else
        {
            // approximate speed for simple movement
            speed = isMoving ? moveSpeed : 0f;
        }

        float speedNormalized = (runSpeed > 0f) ? Mathf.Clamp01(speed / runSpeed) : (isMoving ? 0.5f : 0f);
        // Only set parameters if they exist to avoid runtime errors
        if (HasAnimatorParameter("IsMoving"))
            animator.SetBool("IsMoving", isMoving);
        if (HasAnimatorParameter("Speed"))
            animator.SetFloat("Speed", speedNormalized);
    }

    void CheckMovementState()
    {
        bool wasMoving = isMoving;

        if (creatureMover != null)
        {
            // CreatureMover's internal state drives animation; we can approximate
            // movement by checking whether we currently have a destination.
            isMoving = hasDestination;
        }
        else if (agent != null && agent.isOnNavMesh)
        {
            isMoving = agent.velocity.magnitude > 0.1f && agent.remainingDistance > agent.stoppingDistance;
        }
        else
        {
            isMoving = hasDestination && Vector3.Distance(transform.position, currentDestination) > 0.5f;
        }

        if (wasMoving != isMoving)
        {
            if (isMoving)
            {
                PlayAnimation(walkAnimation);
            }
            else
            {
                PlayAnimation(idleAnimation);
            }
        }
    }

    void PlayAnimation(string animationName)
    {
        if (animator == null) return;
        // If a CreatureMover is present it controls animation internally
        if (creatureMover != null) return;
        bool hasIsMoving = HasAnimatorParameter("IsMoving");
        bool hasSpeed = HasAnimatorParameter("Speed");

        if (hasIsMoving || hasSpeed)
        {
            switch (animationName.ToLower())
            {
                case "idle":
                    if (hasIsMoving) animator.SetBool("IsMoving", false);
                    if (hasSpeed) animator.SetFloat("Speed", 0f);
                    break;
                case "walk":
                    if (hasIsMoving) animator.SetBool("IsMoving", true);
                    if (hasSpeed) animator.SetFloat("Speed", 0.5f);
                    break;
                case "run":
                    if (hasIsMoving) animator.SetBool("IsMoving", true);
                    if (hasSpeed) animator.SetFloat("Speed", 1f);
                    break;
                default:
                    if (hasIsMoving) animator.SetBool("IsMoving", true);
                    if (hasSpeed) animator.SetFloat("Speed", 0.5f);
                    break;
            }
        }
        else
        {
            // Fallback: try to play the animation state directly
            try { animator.Play(animationName); }
            catch { }
        }
    }

    bool HasAnimatorParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }

    void Wander()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        randomDirection.y = transform.position.y;

        if (agent != null && agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                currentDestination = hit.position;
                agent.SetDestination(currentDestination);
                hasDestination = true;

                // Animal's own decision to run or walk
                if (Random.Range(0f, 1f) > 0.8f)
                {
                    agent.speed = runSpeed;
                }
                else
                {
                    agent.speed = moveSpeed;
                }
            }
        }
        else
        {
            // Simple wandering
            currentDestination = randomDirection;
            hasDestination = true;
        }
    }

    void MoveToDestinationSimple()
    {
        if (!hasDestination) return;

        Vector3 direction = (currentDestination - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, currentDestination);

        // Move towards destination
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Rotate towards movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Check if reached destination
        if (distance < 0.5f)
        {
            hasDestination = false;
            PlayAnimation(idleAnimation);
        }
    }

    private System.Collections.IEnumerator AIMovementLoop()
    {
        // Very small delay so Start() finishes and NavMesh (if being built) can settle
        yield return new WaitForSeconds(0.1f);

        while (true)
        {
            // If we have a navmesh agent and it's placed on the NavMesh, choose a destination
            if (agent != null && agent.isOnNavMesh)
            {
                // If agent doesn't currently have a destination, pick one
                if (!hasDestination || agent.remainingDistance <= agent.stoppingDistance)
                {
                    Wander();
                }

                // Let the agent move for a while; if it reaches its target or gets stuck,
                // we'll pick a new one on the next loop.
                float wait = Random.Range(wanderTimer * 0.5f, wanderTimer * 1.5f);
                float elapsed = 0f;
                while (elapsed < wait)
                {
                    // if agent reached destination early, break and pick a new one
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                        break;

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                // No NavMeshAgent available: use simple transform movement decisions
                if (!hasDestination)
                    Wander();

                // Move for a while (Update will perform the actual movement)
                float wait = Random.Range(wanderTimer * 0.5f, wanderTimer * 1.5f);
                float elapsed = 0f;
                while (elapsed < wait && hasDestination)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            // Short pause between decisions
            yield return new WaitForSeconds(Random.Range(0.5f, 2f));
        }
    }
}