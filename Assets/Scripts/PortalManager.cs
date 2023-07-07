using System.Collections;
using System.Collections.Generic;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Events;

public class BluePortalCreationEvent : UnityEvent<GameObject> { }

public class OrangePortalCreationEvent : UnityEvent<GameObject> { }
public class PortalManager : MonoBehaviour
{
    public static PortalManager Instance;
    
    [SerializeField] private GameObject bluePortal;
    [SerializeField] private GameObject orangePortal;
    
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void CreatePortal(PortalColor color, GameObject portal)
    {
        if (color == PortalColor.Blue)
        {
            if (bluePortal != null)
            {
                Destroy(bluePortal);
            }
            bluePortal = portal;
        }
        else if (color == PortalColor.Orange)
        {
            if (orangePortal != null)
            {
                Destroy(orangePortal);
            }
            orangePortal = portal;
        }
    }
    
}
