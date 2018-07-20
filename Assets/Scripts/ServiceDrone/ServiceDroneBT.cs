using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Panda;
using UnityEngine.Events;
using System;

public class ServiceDroneBT : MonoBehaviour
{
    [Tooltip("Set reference to engine location used when the drone starts repairing the ship")]
    public Transform EngineLocation;
    [Tooltip("Set reference to drone's working position")]
    public Transform WorkingLocation;
    [Tooltip("Set reference to the player position")]
    public Transform PlayerTransform;
    [Tooltip("Distance to detect player")]
    public float DetectPlayerDistance = 1f;
    [Tooltip("Set distance to reach target destination")]
    public float PositionAccuracy = 1f;
    [Tooltip("Set Service Drone waypoints array")]
    public Transform[] Waypoints;

    [Tooltip("Set drone move speed")]
    public float Speed = 5f;
    [Tooltip("Set rotation speed")]
    public float RotationSpeed = 10f;

    [Tooltip("Set ship repair time in seconds")]
    public float RepairTime = 5f;

    [Tooltip("Displays important messages to the player")]
    public GameObject InfoText;

    [Tooltip("The game object should have a line renderer and a particle system representing the sparks")]
    public GameObject DroneWorkingEffect;

    private int currentWaypointIndex = -1;

    // are the parts found by the player?
    private bool partsFound = false; // ------------------------------------------------------- set to false
    // is the ship repaired?
    private bool shipRepaired = false;

    // TRUE if the player delivered the parts to the drone
    [HideInInspector]   // public because can be saved in GameSaveLoad.SaveDrone()
    public bool partsDelivered = false;

    private IEnumerator waitForParts;
    private IEnumerator repairingShip;

    // displays repairing progress
    private Slider repairingSlider;

    private ShipEngineScript shipEngine;

    private UnityAction onPlayerHasDarkMatterModule;
    private UnityAction onSpaceshipRepaired;

    private void Awake()
    {
        this.onPlayerHasDarkMatterModule = new UnityAction(this.OnPlayerHasDarkMatterModule);
        EventManager.On(Resources.Events.PlayerHasDarkMatterModule, this.onPlayerHasDarkMatterModule);

        this.onSpaceshipRepaired = new UnityAction(this.OnSpaceshipRepaired);
        EventManager.On(Resources.Events.SpaceshipRepaired, this.onSpaceshipRepaired);

        this.shipEngine = GameObject.FindGameObjectWithTag(Resources.Tags.Ship).GetComponent<ShipEngineScript>();

        this.repairingSlider = GameObject.FindGameObjectWithTag(Resources.Tags.RepairSlider).GetComponent<Slider>();
        this.repairingSlider.gameObject.SetActive(false);  // hide from screen
        this.repairingSlider.minValue = 0f;
        this.repairingSlider.maxValue = this.RepairTime;
    }

    // used when loading saved game
    public void SetStatus(bool partsAreDelivered)
    {
        this.partsDelivered = partsAreDelivered;
    }

    // this event is emitted from GameManager script
    private void OnPlayerHasDarkMatterModule()
    {
        this.partsFound = true;
    }

    // this event is emitted from GameManager script
    private void OnSpaceshipRepaired()
    {
        this.shipRepaired = true;
    }

    private void DisplayInfoText(string text)
    {
        this.InfoText.transform.localScale = Vector3.one;
        this.InfoText.GetComponent<Text>().text = text;
    }

    private void HideInfoText()
    {
        this.InfoText.transform.localScale = Vector3.zero;
    }


    [Task]
    private void Idle()
    {
        this.HideInfoText();

        Task.current.Succeed();
    }

    [Task]
    private void BoardShip()
    {
        // base commanders are blocking the runway, so kill them!
        int baseCommandersCount = GameObject.FindGameObjectsWithTag(Resources.Tags.BaseCommander).Length;
        bool finishGame = baseCommandersCount < 1;
        if (finishGame)
        {
            //Task.current.Succeed();

            StartCoroutine(this.FlyAway(Task.current));
        }
        else
        {
            this.DisplayInfoText(Resources.Messages.KillBaseCommanders);
        }
    }

    [Task]
    private bool IsPlayerNear()
    {
        return Vector3.Distance(this.transform.position, this.PlayerTransform.position) < this.DetectPlayerDistance;
    }

    [Task]
    private void LookAtPlayer()
    {
        Vector3 direction = this.PlayerTransform.position - this.transform.position;
        this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(direction), this.RotationSpeed * Time.deltaTime);

        bool lookDirectionIsOk = (Vector3.Angle(direction, this.transform.forward) < 5f);

        if (lookDirectionIsOk)
        {
            Task.current.Succeed();
        }

        //Task.current.Complete(lookDirectionIsOk);
    }

    #region Patrol Specific

    [Task]
    private void GoToDestination()
    {
        Task task = Task.current;

        if (task.isStarting)
        {
            // must be between 0 and (waypoints.length - 1)
            this.currentWaypointIndex = (this.currentWaypointIndex >= this.Waypoints.Length ? 0 : this.currentWaypointIndex);
            this.currentWaypointIndex = (this.currentWaypointIndex < 0 ? 0 : this.currentWaypointIndex);
        }

        #region set destination

        // calculate rotation and move vector to working location
        Vector3 direction = this.Waypoints[this.currentWaypointIndex].position - this.transform.position;
        Quaternion rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(direction), this.RotationSpeed * Time.deltaTime);

        this.transform.rotation = rotation;

        this.transform.Translate(0, 0, this.Speed * Time.deltaTime);

        Debug.DrawLine(this.transform.position, this.Waypoints[this.currentWaypointIndex].position, Color.yellow);

        #endregion

        bool distanceIsOk = direction.magnitude < this.PositionAccuracy;

        if (distanceIsOk)
        {
            // destination reached - move to next waypoint
            this.currentWaypointIndex++;

            task.Succeed();
        }

        Task.current.debugInfo = string.Format("[Waypoint = {0}]", this.currentWaypointIndex);
    }

    #endregion

    #region Repair Specific

    //[Task]
    private bool IsNearEngine()
    {
        return Vector3.Distance(this.transform.position, this.WorkingLocation.position) < this.PositionAccuracy;
    }

    [Task]
    private bool PlayerHaveParts()
    {
        return this.partsFound;
    }

    [Task]
    private bool DroneHaveParts()
    {
        return this.partsDelivered;
    }

    [Task]
    private bool IsShipRepaired()
    {
        return this.shipRepaired;
    }

    [Task]
    private void CheckParts()
    {
        if (this.partsFound == false)
        {
            this.DisplayInfoText(Resources.Messages.FindDarkMatterModule);
        }

        Task.current.Succeed();
    }

    [Task]
    private void WaitForParts()
    {
        Task task = Task.current;

        if (task.isStarting)
        {
            this.partsDelivered = false;

            if (this.waitForParts != null) StopCoroutine(this.waitForParts);
            this.waitForParts = this.WaitForPartsEnumerator(task);
            StartCoroutine(this.waitForParts);
        }

        // task will succeed in the IEnumerator!
        //task.Complete(this.partsDelivered);
    }

    [Task]
    private void GoToShip()
    {
        // calculate rotation and move vector to working location
        Vector3 direction = this.WorkingLocation.position - this.transform.position;
        Quaternion rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(direction), this.RotationSpeed * Time.deltaTime);

        this.transform.rotation = rotation;

        this.transform.Translate(0, 0, this.Speed * Time.deltaTime);

        Debug.DrawLine(this.transform.position, this.WorkingLocation.position, Color.green);

        bool distanceIsOk = direction.magnitude < this.PositionAccuracy;

        if (distanceIsOk)
        {
            Task.current.Succeed();
        }
        //Task.current.Complete(distanceIsOk);
    }

    [Task]
    private void FaceTheEngine()
    {
        Vector3 direction = this.EngineLocation.position - this.transform.position;
        Quaternion rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(direction), this.RotationSpeed * Time.deltaTime);

        this.transform.rotation = rotation;

        bool lookDirectionIsOk = (Vector3.Angle(direction, this.transform.forward) < 5f);

        Debug.DrawLine(this.transform.position, this.EngineLocation.position, Color.blue);

        if (lookDirectionIsOk)
        {
            Task.current.Succeed();
        }

        //Task.current.Complete(lookDirectionIsOk);
    }

    [Task]
    private void RepairShip()
    {
        Task task = Task.current;

        if (task.isStarting)
        {
            this.shipRepaired = false;

            if (this.repairingShip != null) StopCoroutine(this.repairingShip);
            this.repairingShip = this.RepairingShipEnumerator(task);
            StartCoroutine(this.repairingShip);

            this.DisplayInfoText(Resources.Messages.RepairingShip);
        }

        // task will succeed in the IEnumerator!
        //if (this.shipRepaired)
        //{
        //    this.shipEngine.Repaired();

        //    this.DisplayInfoText(Resources.Messages.ShipReady);

        //    Task.current.Succeed();
        //}
    }

    private IEnumerator WaitForPartsEnumerator(Task task)
    {
        this.DisplayInfoText(Resources.Messages.DeliverParts);

        while (true)
        {
            if (Input.GetKey(KeyCode.F))
            {
                this.partsDelivered = true;

                task.debugInfo = "[Part Delivered!]";
                task.Succeed();

                break;
            }

            yield return new WaitForEndOfFrame();
        }

        this.HideInfoText();
    }

    private IEnumerator RepairingShipEnumerator(Task task)
    {
        if (this.DroneWorkingEffect != null)
        {
            this.DroneWorkingEffect.SetActive(true);
            this.DroneWorkingEffect.GetComponent<ParticleSystem>().Play();
        }

        this.repairingSlider.gameObject.SetActive(true);

        while (this.shipRepaired == false)
        {
            if (this.IsNearEngine() == false)
            {
                task.Fail();
                break;
            }

            if (this.repairingSlider.value < this.repairingSlider.maxValue)
            {
                this.repairingSlider.value += 1;
                yield return new WaitForSeconds(1f);
            }
            else
            {
                this.shipRepaired = true;

                this.shipEngine.Repaired();

                this.DisplayInfoText(Resources.Messages.ShipReady);

                task.debugInfo = "[Ship Repaired!]";
                task.Succeed();

                // it's null!
                //Task.current.Succeed();
            }
        }

        if (this.DroneWorkingEffect != null)
        {
            this.DroneWorkingEffect.SetActive(false);
            this.DroneWorkingEffect.GetComponent<ParticleSystem>().Stop();
        }

        this.repairingSlider.gameObject.SetActive(false);
    }

    #endregion


    private IEnumerator FlyAway(Task task)
    {
        this.DisplayInfoText("BOARDING SHIP!");

        task.Succeed();

        yield return new WaitForSeconds(3f);

        EventManager.Emit(Resources.Events.GameFinish);
    }
}
