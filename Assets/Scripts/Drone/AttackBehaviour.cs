using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AttackBehaviour : BaseBehaviour
{
    [HideInInspector]
    private DroneShooting shootingLogic;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);

        this.shootingLogic = this.Drone.GetComponentInChildren<DroneShooting>();

        this.DroneLogic.SignalLight.DronMode = enumDronMode.Attack;
        this.DroneLogic.DroneShield.DronMode = enumDronMode.Attack;

        this.DroneLogic.StartAttackMode();

        // to highlight command in UI
        GameObject.FindGameObjectWithTag(Resources.Tags.CommandAttack).GetComponent<UnityEngine.UI.Text>().color = Color.white;

        this.NavAgent.updateRotation = false;
        //this.NavAgent.stoppingDistance = 10f;
        this.NavAgent.speed = this.DroneLogic.AttackSpeed;
        this.NavAgent.angularSpeed = this.DroneLogic.AttackAngularSpeed;
        this.NavAgent.acceleration = this.DroneLogic.AttackAcceleration;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        this.DroneLogic.SwitchTarget();

        // set destination
        if (Time.frameCount % 10 == 0 && this.DestinationReached())
        {
            if (this.DroneLogic.CurrentTarget == null)
            {
                Vector3 destination = this.GetRandomDestination(this.PlayerTransform.position, this.DroneLogic.MaxDistance, NavMesh.AllAreas);
                this.NavAgent.SetDestination(destination);
            }
            else
            {
                base.GetRandomDestination(this.DroneLogic.CurrentTarget.transform.position, 10f, NavMesh.AllAreas);
                this.NavAgent.SetDestination(this.DroneLogic.CurrentTarget.transform.position);
            }
        }

        // faster acceleration when in attack mode
        //Vector3 dir = this.NavAgent.steeringTarget - this.DroneTransform.position;
        //float turnAngle = Vector3.Angle(this.DroneTransform.forward, dir);
        //this.NavAgent.acceleration = turnAngle * this.NavAgent.speed;

        // try to attack target
        if (this.DroneLogic.CurrentTarget != null)
        {
            Vector3 target = this.DroneLogic.CurrentTarget.transform.position;
            target.y += 2f;

#if UNITY_EDITOR
            Debug.DrawLine(this.DroneTransform.position, target);
#endif

            Vector3 direction = target - this.DroneTransform.position;

            // look at target
            this.DroneTransform.rotation = Quaternion.Slerp(this.DroneTransform.rotation, Quaternion.LookRotation(direction), Time.fixedDeltaTime * this.DroneLogic.AttackAngularSpeed);

            // shoot if target is visible
            if (this.CanSeeTarget(target))
            {
                this.shootingLogic.Shoot(this.DroneTransform.position, Quaternion.LookRotation(direction));
            }
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        this.NavAgent.updateRotation = true;
        //this.NavAgent.stoppingDistance = 3f;

        // to highlight command in UI
        GameObject.FindGameObjectWithTag(Resources.Tags.CommandAttack).GetComponent<UnityEngine.UI.Text>().color = Color.black;

        // used to manage drone battery
        this.DroneLogic.EndAttackMode();
    }

    private bool CanSeeTarget(Vector3 target)
    {
        Vector3 direction = target - this.DroneTransform.position;
        if (Vector3.Angle(direction, this.DroneTransform.forward) < this.DroneLogic.MaxAttackAngle)
        {
            return Physics.Linecast(this.DroneTransform.position, target, LayerMask.GetMask(Resources.Layers.Buildings)) == false;
        }

        return false;

        // check distance to target?
    }
}
