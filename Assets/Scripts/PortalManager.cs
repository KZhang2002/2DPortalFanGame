using System.Collections;
using System.Collections.Generic;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Events;

public enum PortalColor { None, Blue, Orange }

public class BluePortalCreationEvent : UnityEvent<GameObject> { }

public class OrangePortalCreationEvent : UnityEvent<GameObject> { }
public class PortalManager : MonoBehaviour
{
    public static PortalManager Instance;
    
    public GameObject BluePortal { get; private set; }
    public GameObject OrangePortal { get; private set; }
    
    [SerializeField] public List<LayerMask> layerList = new List<LayerMask>() {
        LayerMask.NameToLayer("Environment"),
        LayerMask.NameToLayer("Blue Portal"),
        LayerMask.NameToLayer("Orange Portal")
    };

    public List<LayerMask> GetLayerList() {
        return layerList;
    }
    
    public GameObject GetPartnerPortal(PortalColor color) {
        if (color == PortalColor.Blue) {
            return OrangePortal;
        }
        
        if (color == PortalColor.Orange) {
            return BluePortal;
        }

        return null;
    }

    // Singleton for PortalManager
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

    public void AssignPortal(PortalColor color, GameObject portal)
    {
        if (portal != null) {
            Debug.Log("Portal assigned.");
            if (color == PortalColor.Blue)
            {
                if (BluePortal != null)
                {
                    //Debug.Log("Old portal destroyed.");
                    Destroy(BluePortal);
                }
                //Debug.Log(color.ToString() + " portal assigned.");
                BluePortal = portal;
            }
            else if (color == PortalColor.Orange)
            {
                if (OrangePortal != null)
                {
                    //Debug.Log("Old portal destroyed.");
                    Destroy(OrangePortal);
                }
                //Debug.Log(color.ToString() + " portal assigned.");
                OrangePortal = portal;
            }
        }
    }

    public void UnassignPortal(PortalColor color, GameObject portal) {
        if (portal != null) {
            if (color == PortalColor.Blue)
            {
                Destroy(BluePortal);
            }
            else if (color == PortalColor.Orange)
            {
                Destroy(OrangePortal);
            }
        }
    }
}
