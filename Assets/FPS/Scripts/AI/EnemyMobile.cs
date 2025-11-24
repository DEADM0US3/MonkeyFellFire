using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(EnemyController))]
    public class EnemyMobile : MonoBehaviour
    {
        public enum AIState
        {
            Patrol,
            Follow,
            Attack,
        }

        [Header("Components")]
        public Animator Animator;

        [Tooltip("Fraction of the enemy's attack range at which it will stop moving towards target while attacking")]
        [Range(0f, 1f)]
        public float AttackStopDistanceRatio = 0.5f;

        [Tooltip("The random hit damage effects")]
        public ParticleSystem[] RandomHitSparks;

        [Tooltip("Particle systems played when detecting the player")]
        public ParticleSystem[] OnDetectVfx;
        public AudioClip OnDetectSfx;

        [Header("Sound")]
        public AudioClip MovementSound;
        public MinMaxFloat PitchDistortionMovementSpeed;

        // State
        public AIState AiState { get; private set; }

        // Internal refs
        EnemyController m_EnemyController;
        AudioSource m_AudioSource;

        const string k_AnimMoveSpeedParameter = "MoveSpeed";
        const string k_AnimStabParameter = "Stabbed";
        const string k_AnimAttackParameter = "Attack";
        const string k_AnimAlertedParameter = "Alerted";
        const string k_AnimOnDamagedParameter = "OnDamaged";

        void Start()
        {
            // Cache controller
            m_EnemyController = GetComponent<EnemyController>();
            if (m_EnemyController == null)
            {
                Debug.LogError($"EnemyMobile on '{name}' requires EnemyController but none was found. Disabling EnemyMobile.");
                enabled = false;
                return;
            }

            // Subscribe safely
            m_EnemyController.onStab += OnStab;
            m_EnemyController.onAttack += OnAttack;
            m_EnemyController.onDetectedTarget += OnDetectedTarget;
            m_EnemyController.onLostTarget += OnLostTarget;
            m_EnemyController.onDamaged += OnDamaged;

            // Start patrolling
            AiState = AIState.Patrol;

            // AudioSource (optional)
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                // Not fatal: we just won't play movement sound
                Debug.LogWarning($"EnemyMobile on '{name}' doesn't have an AudioSource. Movement sound will be skipped.");
            }
            else
            {
                if (MovementSound != null)
                {
                    m_AudioSource.clip = MovementSound;
                    m_AudioSource.loop = true;
                    m_AudioSource.Play();
                }
                else
                {
                    // No clip assigned; don't try to play
                    m_AudioSource.clip = null;
                }
            }

            // Animator check
            if (Animator == null)
            {
                Debug.LogWarning($"EnemyMobile on '{name}' has no Animator assigned. Animation parameters will be skipped.");
            }
        }

        void Update()
        {
            if (m_EnemyController == null) return;

            UpdateAiStateTransitions();
            UpdateCurrentAiState();

            // movement-based animator / audio updates
            var agent = m_EnemyController.NavMeshAgent;
            float moveSpeed = 0f;
            float agentMaxSpeed = 1f;

            if (agent != null)
            {
                // safety: if agent not on navmesh, consider speed zero
                if (agent.isOnNavMesh)
                {
                    moveSpeed = agent.velocity.magnitude;
                    agentMaxSpeed = Mathf.Max(0.0001f, agent.speed);
                }
                else
                {
                    moveSpeed = 0f;
                    agentMaxSpeed = 1f;
                }
            }

            if (Animator != null)
            {
                Animator.SetFloat(k_AnimMoveSpeedParameter, moveSpeed);
            }

            if (m_AudioSource != null && m_AudioSource.clip != null)
            {
                // normalize pitch between Min and Max using agent speed proportion
                float t = Mathf.Clamp01(moveSpeed / agentMaxSpeed);
                m_AudioSource.pitch = Mathf.Lerp(PitchDistortionMovementSpeed.Min, PitchDistortionMovementSpeed.Max, t);
            }
        }

        void UpdateAiStateTransitions()
        {
            if (m_EnemyController == null) return;

            // Handle transitions 
            switch (AiState)
            {
                case AIState.Follow:
                    // Transition to attack when there is a line of sight to the target
                    if (m_EnemyController.IsSeeingTarget && m_EnemyController.IsTargetInAttackRange)
                    {
                        AiState = AIState.Attack;
                        // stop movement while attacking
                        var agent = m_EnemyController.NavMeshAgent;
                        if (agent != null && agent.isOnNavMesh)
                            agent.SetDestination(transform.position);
                    }

                    break;
                case AIState.Attack:
                    // Transition to follow when no longer a target in attack range
                    if (!m_EnemyController.IsTargetInAttackRange)
                    {
                        AiState = AIState.Follow;
                    }

                    break;
            }
        }

        void UpdateCurrentAiState()
        {
            if (m_EnemyController == null) return;

            if (m_EnemyController.KnownDetectedTarget == null)
            {
                AiState = AIState.Patrol;
                return;
            }

            switch (AiState)
            {
                case AIState.Follow:
                    SafeSetNavDestinationToTargetPosition();
                    m_EnemyController.OrientTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                    m_EnemyController.OrientWeaponsTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                    break;

                case AIState.Attack:
                    if (m_EnemyController.DetectionModule == null ||
                        m_EnemyController.DetectionModule.DetectionSourcePoint == null)
                    {
                        Debug.LogError($"EnemyMobile ({name}): DetectionModule or DetectionSourcePoint is null");
                        return;
                    }

                    float distToTarget = Vector3.Distance(
                        m_EnemyController.KnownDetectedTarget.transform.position,
                        m_EnemyController.DetectionModule.DetectionSourcePoint.position);

                    if (distToTarget >= (AttackStopDistanceRatio * m_EnemyController.DetectionModule.AttackRange))
                    {
                        SafeSetNavDestinationToTargetPosition();
                    }
                    else
                    {
                        // Stop moving (set destination to self if agent available)
                        var agent = m_EnemyController.NavMeshAgent;
                        if (agent != null && agent.isOnNavMesh)
                        {
                            agent.SetDestination(transform.position);
                        }
                    }

                    m_EnemyController.OrientTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                    m_EnemyController.TryAttack(m_EnemyController.KnownDetectedTarget.transform.position);
                    break;
            }
        }

        void SafeSetNavDestinationToTargetPosition()
        {
            if (m_EnemyController == null || m_EnemyController.KnownDetectedTarget == null) return;

            var agent = m_EnemyController.NavMeshAgent;
            if (agent == null)
            {
                // no agent to move
                return;
            }

            if (!agent.isOnNavMesh)
            {
                // if agent is not placed on navmesh, try to warp it to current position if reasonable,
                // or skip setting destination.
                Debug.LogWarning($"EnemyMobile ({name}): NavMeshAgent is not on NavMesh. Skipping SetDestination.");
                return;
            }

            agent.SetDestination(m_EnemyController.KnownDetectedTarget.transform.position);
        }

        void OnAttack()
        {
            if (Animator != null)
                Animator.SetTrigger(k_AnimAttackParameter);
        }

        void OnStab()
        {
            if (Animator != null)
                Animator.SetTrigger(k_AnimStabParameter);
        }

        void OnDetectedTarget()
        {
            if (AiState == AIState.Patrol)
            {
                AiState = AIState.Follow;
            }

            // Play detection VFX safely
            if (OnDetectVfx != null && OnDetectVfx.Length > 0)
            {
                for (int i = 0; i < OnDetectVfx.Length; i++)
                {
                    var ps = OnDetectVfx[i];
                    if (ps != null)
                    {
                        ps.Play();
                    }
                    else
                    {
                        Debug.LogWarning($"EnemyMobile ({name}): OnDetectVfx[{i}] is null.");
                    }
                }
            }
            else
            {
                // only warn once; this can happen if prefab lacks VFX
                Debug.LogWarning($"EnemyMobile ({name}): No OnDetectVfx assigned.");
            }

            // Play SFX if available
            if (OnDetectSfx != null)
            {
                AudioUtility.CreateSFX(OnDetectSfx, transform.position, AudioUtility.AudioGroups.EnemyDetection, 1f);
            }

            if (Animator != null)
                Animator.SetBool(k_AnimAlertedParameter, true);
        }

        void OnLostTarget()
        {
            if (AiState == AIState.Follow || AiState == AIState.Attack)
            {
                AiState = AIState.Patrol;
            }

            if (OnDetectVfx != null && OnDetectVfx.Length > 0)
            {
                for (int i = 0; i < OnDetectVfx.Length; i++)
                {
                    var ps = OnDetectVfx[i];
                    if (ps != null)
                    {
                        ps.Stop();
                    }
                }
            }

            if (Animator != null)
                Animator.SetBool(k_AnimAlertedParameter, false);
        }

        void OnDamaged()
        {
            if (RandomHitSparks != null && RandomHitSparks.Length > 0)
            {
                int n = Random.Range(0, RandomHitSparks.Length); // correct range
                if (RandomHitSparks[n] != null)
                    RandomHitSparks[n].Play();
            }

            if (Animator != null)
                Animator.SetTrigger(k_AnimOnDamagedParameter);
        }
    }
}
