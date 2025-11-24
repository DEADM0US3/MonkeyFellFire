using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.FPS.Game;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        [Header("General")]
        public float SelfDestructYHeight = -20f;
        public float OrientationSpeed = 12f;
        public float PathReachingRadius = 1.8f;
        public float DeathDuration = 0.5f;

        [Header("Movement")]
        public bool RandomMovement = true;
        public float MovementRadius = 12f;
        public float ChangeDirectionDelay = 3f;
        float lastMoveTime;

        [Header("Combat")]
        public bool SwapToNextWeapon = false;
        public float DelayAfterWeaponSwap = 0.8f;
        float lastSwapTime;

        [Header("Loot")]
        public GameObject LootPrefab;
        [Range(0, 1)] public float DropRate = 1;

        [Header("VFX - SFX")]
        public GameObject DeathVfx;
        public Transform DeathVfxSpawnPoint;
        public AudioClip DamageTick;

        // Public events
        public UnityAction onAttack;
        public UnityAction onStab;
        public UnityAction onDetectedTarget;
        public UnityAction onLostTarget;
        public UnityAction onDamaged;

        // References
        EnemyManager enemyManager;
        DetectionModule detection;
        NavMeshAgent agent;
        Health health;
        Actor actor;
        GameFlowManager gameFlow;

        // Weapons
        WeaponController[] weapons;
        int weaponIndex = 0;
        WeaponController currentWeapon;

        // Internal
        Collider[] selfColliders;
        bool damagedFrame = false;

        // -------------------------
        // PROPIEDADES NECESARIAS PARA ENEMY MOBILE
        // -------------------------
        public NavMeshAgent NavMeshAgent => agent;
        public DetectionModule DetectionModule => detection;

        public GameObject KnownDetectedTarget => detection.KnownDetectedTarget;
        public bool IsSeeingTarget => detection.IsSeeingTarget;
        public bool IsTargetInAttackRange => detection.IsTargetInAttackRange;

        void Start()
        {
            enemyManager = FindAnyObjectByType<EnemyManager>();
            enemyManager.RegisterEnemy(this);

            detection = GetComponentInChildren<DetectionModule>();
            detection.onDetectedTarget += HandleTargetDetected;
            detection.onLostTarget += HandleTargetLost;

            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            actor = GetComponent<Actor>();
            selfColliders = GetComponentsInChildren<Collider>();

            gameFlow = FindAnyObjectByType<GameFlowManager>();

            health.OnDie += OnDie;
            health.OnDamaged += OnDamagedInternal;

            InitWeapons();
        }

        void InitWeapons()
        {
            weapons = GetComponentsInChildren<WeaponController>();
            if (weapons.Length > 0)
            {
                foreach (var w in weapons) w.Owner = gameObject;
                SetWeapon(0);
            }
        }

        void SetWeapon(int index)
        {
            weaponIndex = index;
            currentWeapon = weapons[index];
            lastSwapTime = SwapToNextWeapon ? Time.time : Mathf.NegativeInfinity;
        }

        void Update()
        {
            if (transform.position.y < SelfDestructYHeight)
            {
                Destroy(gameObject);
                return;
            }

            detection.HandleTargetDetection(actor, selfColliders);

            if (!KnownDetectedTarget && RandomMovement)
                Patrol();

            damagedFrame = false;
        }

        // -------------------------
        // Movement / Patrol
        // -------------------------
        void Patrol()
        {
            if (Time.time - lastMoveTime < ChangeDirectionDelay) return;

            Vector3 random = transform.position + Random.insideUnitSphere * MovementRadius;
            if (NavMesh.SamplePosition(random, out NavMeshHit hit, MovementRadius, NavMesh.AllAreas))
            {
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(hit.position);
                }

                lastMoveTime = Time.time;
            }
        }

        // -------------------------
        // Detection
        // -------------------------
        void HandleTargetDetected()
        {
            onDetectedTarget?.Invoke();

            if (KnownDetectedTarget != null)
            {
                agent.isStopped = false;
                agent.SetDestination(KnownDetectedTarget.transform.position);
            }
        }

        void HandleTargetLost()
        {
            onLostTarget?.Invoke();
            ResumePatrol();
        }

        void ResumePatrol()
        {
            RandomMovement = true;
            agent.isStopped = false;
            lastMoveTime = 0;
        }

        // -------------------------
        // Combat
        // -------------------------
        public void FaceTarget(Vector3 pos)
        {
            Vector3 dir = (pos - transform.position).normalized;
            dir.y = 0;
            if (dir.sqrMagnitude > 0)
            {
                Quaternion rot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * OrientationSpeed);
            }
        }

        public bool TryAttack(Vector3 targetPos)
        {
            if (gameFlow.GameIsEnding) return false;

            FaceTarget(targetPos);

            float dist = Vector3.Distance(transform.position, targetPos);
            WeaponController weapon = currentWeapon;

            // Melee
            if (dist <= weapon.MeleeRange)
            {
                Vector3 origin = weapon.WeaponMuzzle != null ?
                    weapon.WeaponMuzzle.position :
                    transform.position + transform.forward * 0.5f;

                Vector3 dir = (targetPos - origin).normalized;

                weapon.PerformMeleeAttackFrom(origin, dir);
                onStab?.Invoke();
                HandleSwap();
                return true;
            }

            // Ranged
            bool shot = weapon.HandleShootInputs(false, true, false);
            if (shot)
            {
                onAttack?.Invoke();
                HandleSwap();
            }

            return shot;
        }

        void HandleSwap()
        {
            if (SwapToNextWeapon && weapons.Length > 1)
            {
                int next = (weaponIndex + 1) % weapons.Length;
                SetWeapon(next);
            }
        }

        // -------------------------
        // Damage / Death
        // -------------------------
        void OnDamagedInternal(float dmg, GameObject source)
        {
            if (source && !source.GetComponent<EnemyController>())
                detection.OnDamaged(source);

            if (!damagedFrame)
            {
                damagedFrame = true;
                onDamaged?.Invoke();
                if (DamageTick)
                    AudioUtility.CreateSFX(DamageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);
            }
        }

        void OnDie()
        {
            enemyManager.UnregisterEnemy(this);

            if (DeathVfx)
            {
                var vfx = Instantiate(DeathVfx, DeathVfxSpawnPoint.position, Quaternion.identity);
                Destroy(vfx, 5f);
            }

            if (TryDrop())
                Instantiate(LootPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject, DeathDuration);
        }

        bool TryDrop()
        {
            if (!LootPrefab) return false;
            if (DropRate >= 1) return true;
            return Random.value <= DropRate;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, PathReachingRadius);

            if (detection)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, detection.DetectionRange);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, detection.AttackRange);
            }
        }

        // -------------------------
        // MÉTODOS QUE FALTABAN PARA ENEMY MOBILE
        // -------------------------

        public void SetNavDestination(Vector3 pos)
        {
            if (agent == null) return;
            agent.isStopped = false;
            agent.SetDestination(pos);
        }

        public void OrientTowards(Vector3 pos)
        {
            FaceTarget(pos);
        }

        public void OrientWeaponsTowards(Vector3 pos)
        {
            if (currentWeapon == null) return;

            Vector3 dir = (pos - currentWeapon.WeaponMuzzle.position).normalized;
            currentWeapon.transform.forward = dir;
        }
    }
}
