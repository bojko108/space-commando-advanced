using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneShieldScript : MonoBehaviour
{
    [Tooltip("Set drone shield color when in partol mode")]
    public Color PatrolColor;
    [Tooltip("Set drone shield color when in scan mode")]
    public Color ScanColor;
    [Tooltip("Set drone shield color when in attack mode")]
    public Color AttackColor;

    [HideInInspector]
    public enumDronMode DronMode
    {
        get { return this.droneMode; }
        set
        {
            this.droneMode = value;

            this.SetColor();
        }
    }
    private enumDronMode droneMode;

    private Material shieldMaterial;

    private void Start()
    {
        this.shieldMaterial = GetComponent<Renderer>().material;

        this.SetColor();
    }

    private void SetColor()
    {
        switch (this.droneMode)
        {
            case enumDronMode.Scan:
                this.shieldMaterial.SetColor("Color_E9E5616A", this.ScanColor);
                break;
            case enumDronMode.Attack:
                this.shieldMaterial.SetColor("Color_E9E5616A", this.AttackColor);
                break;
            default:
                this.shieldMaterial.SetColor("Color_E9E5616A", this.PatrolColor);
                break;
        }
    }
}