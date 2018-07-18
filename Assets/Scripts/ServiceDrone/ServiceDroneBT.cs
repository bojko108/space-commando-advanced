using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Panda;

public class ServiceDroneBT : MonoBehaviour
{
    // set reference to engine location used when the drone starts repairing the ship
    public Transform EngineLocation;
    // set reference to drone's working position
    public Transform WorkingLocation;
    // set reference to the player position
    public Transform PlayerTransform;
    public float DetectPlayerDistance = 1f;

    // set drone move speed
    public float Speed = 5f;
    // set rotation speed
    public float RotationSpeed = 10f;

    // set ship repair time in seconds
    public float RepairTime = 5f;

    // are the parts found by the player?
    public bool PartsFound = false;
    // is the ship repaired?
    public bool ShipRepaired = false;

    // TRUE if the player delivered the parts to the drone
    private bool partsDelivered = false;
    
    private IEnumerator waitForParts;
    private IEnumerator repairingShip;
    
    [Task]
    private void Idle()
    {
        Task.current.debugInfo = string.Format("[Info = {0}]", "asdsadasd");
        Task.current.Succeed();
    }

    [Task]
    private void ShipReadyForTakeOff()
    {
        Task.current.debugInfo = "[Ship Ready!]";
        Task.current.Succeed();
    }

    #region Patrol Specific

    [Task]
    private bool IsPlayerNear()
    {
        return Vector3.Distance(this.transform.position, this.PlayerTransform.position) < this.DetectPlayerDistance;
    }

    [Task]
    private void LookAtPlayer()
    {
        //Task task = Task.current;

        Vector3 direction = this.PlayerTransform.position - this.transform.position;
        this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(direction), this.RotationSpeed * Time.deltaTime);

        bool lookDirectionIsOk = (Vector3.Angle(direction, this.transform.forward) < 5f);

        Task.current.Complete(lookDirectionIsOk);
    }

    #endregion

    #region Repair Specific

    [Task]
    private bool PlayerHaveParts()
    {
        // use EventManager to listen for DarkMatterModuleFound....
        return this.PartsFound;
    }

    [Task]
    private bool DroneHaveParts()
    {
        return this.partsDelivered;
    }

    [Task]
    private bool IsShipRepaired()
    {
        return this.ShipRepaired;
    }

    [Task]
    private void WaitForParts()
    {
        Task task = Task.current;

        if (task.isStarting)
        {
            this.partsDelivered = false;

            if (this.waitForParts != null) StopCoroutine(this.waitForParts);
            this.waitForParts = this.WaitForPartsEnumerator();
            StartCoroutine(this.waitForParts);
        }

        Task.current.Complete(this.partsDelivered);
    }

    [Task]
    private void GoToShip()
    {
        // calculate rotation and move vector to working location
        Vector3 direction = this.WorkingLocation.position - this.transform.position;
        Quaternion rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(direction), this.RotationSpeed * Time.deltaTime);
        
        this.transform.rotation = rotation;

        this.transform.Translate(0, 0, this.Speed * Time.deltaTime);

        Debug.DrawLine(this.transform.position, this.WorkingLocation.position);

        bool distanceIsOk = direction.magnitude < 1f;
        //this.RotationSpeed = distanceIsOk ? 2f : this.RotationSpeed;

        Task.current.Complete(distanceIsOk);
    }

    [Task]
    private void FaceTheEngine()
    {
        Vector3 direction = this.EngineLocation.position - this.transform.position;
        Quaternion rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(direction), this.RotationSpeed * Time.deltaTime);

        this.transform.rotation = rotation;

        bool lookDirectionIsOk = (Vector3.Angle(direction, this.transform.forward) < 5f);

        Task.current.Complete(lookDirectionIsOk);
    }

    [Task]
    private void RepairShip()
    {
        Task task = Task.current;

        if (task.isStarting)
        {
            this.ShipRepaired = false;

            if (this.repairingShip != null) StopCoroutine(this.repairingShip);
            this.repairingShip = this.RepairingShipEnumerator();
            StartCoroutine(this.repairingShip);
        }
        else
        {
            if (this.ShipRepaired)
            {
                Task.current.Succeed();
            }
        }
    }

    private IEnumerator WaitForPartsEnumerator()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                this.partsDelivered = true;
                break;
            }

            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator RepairingShipEnumerator()
    {
        yield return new WaitForSeconds(this.RepairTime);

        this.ShipRepaired = true;
    }

    #endregion
}
