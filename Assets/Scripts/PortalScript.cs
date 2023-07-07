using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PortalColor { None, Blue, Orange }

public class PortalScript : MonoBehaviour
{
    private Vector3 _objectPos;
    private Vector3 _arrowTip;
    [SerializeField] float gizmoLength = 1f;
    [SerializeField] private PortalColor portalColor = PortalColor.None;

    private Vector3[] _points;

    private void Awake()
    {
        PortalManager.Instance.CreatePortal(portalColor, gameObject);
    }

    private void Start()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;

        _objectPos = Vector3.zero;
        _arrowTip = _objectPos + (Vector3.right * gizmoLength);
        
        _points = new Vector3[6]
        {
            _objectPos,
            _arrowTip,
            _arrowTip,
            _arrowTip + new Vector3(-0.2f, -0.2f, 0),
            _arrowTip,
            _arrowTip + new Vector3(-0.2f, 0.2f, 0)
        };
        
        // Store the original Gizmos matrix
        Matrix4x4 originalMatrix = Gizmos.matrix;

        // Apply the rotation of the GameObject
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        // Draw your Gizmos here, they will inherit the rotation of the GameObject
        // Draws arrow pointing from front
        Gizmos.DrawLineList(_points);
        
        // Draws cube at portal bottom
        Gizmos.DrawWireCube(
            new Vector3(0, -transform.localScale.y/2, 0), 
            new Vector3(0.8f, 0.3f, 1));

        // Restore the original Gizmos matrix
        Gizmos.matrix = originalMatrix;
        
        //Gizmos.DrawLine(_objectPos, _objectPos + (transform.right * gizmoLength));
    }
}
