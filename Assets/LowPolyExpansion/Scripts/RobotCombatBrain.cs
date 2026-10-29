using UnityEngine;
using UnityEngine.AI;
using Unity.FPS.Game;
using Unity.FPS.AI;

namespace LowPolyExpansion
{
    // Uses the template's detection, damage, weapons and enemy objective registration.
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(EnemyController))]
    public class RobotCombatBrain : MonoBehaviour
    {
        public enum RobotKind { ScoutDrone, AssaultWalker, SiegeTank, RailSentry }
        public RobotKind Kind;
        public Transform Body;
        public Transform[] Legs = new Transform[0];
        public Transform[] Rotors = new Transform[0];
        public Transform ChargeIndicator;
        public float PreferredRange = 9f;
        public float PatrolRadius = 6f;
        EnemyController enemy;
        NavMeshAgent agent;
        Health health;
        Vector3 home, bodyRest, lastPosition;
        Quaternion bodyRotation;
        float phase, nextPath, nextShot, chargeStarted = -1f, kick, deathTime = -1f;
        Quaternion[] legRest;
        bool dead;

        void Start()
        {
            enemy = GetComponent<EnemyController>();
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            home = lastPosition = transform.position;
            bodyRest = Body.localPosition;
            bodyRotation = Body.localRotation;
            phase = Random.value * Mathf.PI * 2;
            legRest = new Quaternion[Legs.Length];
            for (int i = 0; i < Legs.Length; i++) legRest[i] = Legs[i].localRotation;
            agent.updateRotation = false;
            enemy.onAttack += OnAttack;
            enemy.onDetectedTarget += OnDetected;
            enemy.onLostTarget += OnLost;
            health.OnDie += OnDeath;
        }

        void OnAttack() { kick = 1; }
        void OnDetected() { nextPath = 0; }
        void OnLost() { chargeStarted = -1; nextPath = 0; }

        void Update()
        {
            if (dead) return;
            if (!agent.enabled || !agent.isOnNavMesh) return;
            GameObject target = enemy.KnownDetectedTarget;
            if (target)
            {
                Vector3 delta = target.transform.position - transform.position;
                Vector3 direction = Vector3.ProjectOnPlane(delta, Vector3.up).normalized;
                float distance = Vector3.ProjectOnPlane(delta, Vector3.up).magnitude;
                enemy.OrientTowards(target.transform.position);
                enemy.OrientWeaponsTowards(target.transform.position);
                if (Time.time >= nextPath)
                {
                    Vector3 destination = transform.position;
                    if (!enemy.IsSeeingTarget || distance > PreferredRange + 2)
                        destination = target.transform.position - direction * PreferredRange * .7f;
                    else if (Kind == RobotKind.ScoutDrone || Kind == RobotKind.AssaultWalker)
                        destination = target.transform.position - direction * PreferredRange +
                            Vector3.Cross(direction, Vector3.up) * Mathf.Sin(Time.time * 1.3f + phase) * 5;
                    else if (Kind == RobotKind.RailSentry && distance < PreferredRange * .65f)
                        destination = transform.position - direction * 5;
                    MoveTo(destination);
                    nextPath = Time.time + .35f;
                }
                bool canAttack = enemy.IsSeeingTarget && enemy.IsTargetInAttackRange;
                if (Kind == RobotKind.RailSentry)
                {
                    // A visible wind-up gives the player a chance to evade a heavy shot.
                    if (!canAttack) chargeStarted = -1;
                    else if (Time.time >= nextShot)
                    {
                        if (chargeStarted < 0) chargeStarted = Time.time;
                        if (Time.time - chargeStarted >= .9f && enemy.TryAtack(target.transform.position))
                        { chargeStarted = -1; nextShot = Time.time + 2.8f; }
                    }
                }
                else if (canAttack) enemy.TryAtack(target.transform.position);
            }
            else if (Time.time >= nextPath)
            {
                float angle = Time.time * .35f + phase;
                MoveTo(home + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * PatrolRadius);
                if (agent.desiredVelocity.sqrMagnitude > .1f)
                    enemy.OrientTowards(transform.position + agent.desiredVelocity);
                nextPath = Time.time + 2.5f;
            }
        }

        void MoveTo(Vector3 destination)
        {
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, agent.areaMask))
                agent.SetDestination(hit.position);
        }

        void LateUpdate()
        {
            if (!Body) return;
            if (dead)
            {
                float t = Mathf.Clamp01((Time.time - deathTime) / .6f);
                Body.localRotation = bodyRotation * Quaternion.Euler(t * t * 75, 0, t * 22);
                Body.localPosition = bodyRest + Vector3.down * t * .7f;
                return;
            }
            float speed = agent && agent.enabled && agent.isOnNavMesh ? agent.velocity.magnitude : 0;
            float travel = Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;
            phase += travel * 4;
            kick *= Mathf.Exp(-Time.deltaTime * 14);
            float bob = Kind == RobotKind.ScoutDrone ? Mathf.Sin(Time.time * 3 + phase) * .15f :
                Mathf.Abs(Mathf.Sin(phase)) * .055f * Mathf.Clamp01(speed);
            Body.localPosition = bodyRest + Vector3.up * bob + Vector3.back * kick * .1f;
            Body.localRotation = bodyRotation * Quaternion.Euler(-kick * 9, 0,
                Kind == RobotKind.ScoutDrone ? Mathf.Sin(Time.time * 2 + phase) * 7 : 0);
            for (int i = 0; i < Legs.Length; i++)
                Legs[i].localRotation = legRest[i] * Quaternion.Euler(
                    Mathf.Sin(phase + (i % 2) * Mathf.PI) * 28 * Mathf.Clamp01(speed), 0, 0);
            foreach (Transform rotor in Rotors) rotor.Rotate(0, (Kind == RobotKind.ScoutDrone ? 720 : speed * 140) * Time.deltaTime, 0);
            if (ChargeIndicator)
            {
                float charge = chargeStarted < 0 ? 0 : Mathf.Clamp01((Time.time - chargeStarted) / .9f);
                ChargeIndicator.localScale = Vector3.one * (.12f + charge * .3f);
            }
        }

        void OnDeath()
        {
            dead = true;
            deathTime = Time.time;
            enemy.enabled = false;
            if (agent && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        }

        void OnDestroy()
        {
            if (health) health.OnDie -= OnDeath;
            if (!enemy) return;
            enemy.onAttack -= OnAttack;
            enemy.onDetectedTarget -= OnDetected;
            enemy.onLostTarget -= OnLost;
        }
    }
}
