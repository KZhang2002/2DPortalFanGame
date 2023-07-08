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
    private GameObject _surfaceObject;
    private RaycastHit2D _hit;

    private Vector3[] _points;

    private void Awake() {
        
    }

    private void Start()
    {
        _surfaceObject = _hit.collider.gameObject;
        
        // Checks for collisions
        OrientPortal();
        CheckCollisions();
        
        // Creates portal
        PortalManager.Instance.AssignPortal(portalColor, gameObject);
    }

    private void Update() {
        throw new NotImplementedException();
    }

    public void SetRaycastHit(RaycastHit2D hitToPass) {
        _hit = hitToPass;
    }

    // todo: rewrite to use local space rather than world space
    private void OrientPortal() {
        Vector2 surfaceNormal = _hit.normal;
        float zValue = Mathf.Atan2(surfaceNormal.y, surfaceNormal.x) * Mathf.Rad2Deg;
        float yValue = 0;

        // Adjusts rotation so bottom of portal always 
        // faces the floor when placed on a tilted surface
        // todo: Change threshold for ceiling (90f -> 80f)?
        if (zValue > 90f) {
            zValue = 180f - zValue;
            yValue = 180f;
        }
        else if (zValue < -90f) {
            zValue = -180f - zValue;
            yValue = 180f;
        }
        // Adjusts rotation so bottom of portal 
        // faces the player when placed on ceiling or floor
        else if (zValue is -90f or 90f) {
            // If this code is pulled out of the player, change transform.position.x
            // to the transform of the object specifically
            // todo: this implementation is really sloppy but im just spitballin here
            if (_hit.point.x < transform.position.x && _hit.point.y > transform.position.y) {
                yValue = 180f;
            } 
            else if (_hit.point.x > transform.position.x && _hit.point.y < transform.position.y) {
                yValue = 180f;
            }
        }

        Vector3 rotation = new Vector3(0f, yValue, zValue);
        Debug.Log("Portal rotation: " + rotation);
        transform.Rotate(0f, yValue, zValue, Space.World);
    }

    private void CheckCollisions() {
        
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
