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
    
    private GameObject bluePortalClone;
    private GameObject orangePortalClone;
    
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
            if (bluePortalClone != null)
            {
                Destroy(bluePortalClone);
            }
            bluePortalClone = portal;
        }
        else if (color == PortalColor.Orange)
        {
            if (orangePortalClone != null)
            {
                Destroy(orangePortalClone);
            }
            orangePortalClone = portal;
        }
    }
    
}
