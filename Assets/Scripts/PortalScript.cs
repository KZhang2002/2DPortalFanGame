using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PortalScript : MonoBehaviour
{
    public struct BoundsPoints {
        public BoundsPoints (Vector3 tf, Vector3 bf, Vector3 tb, Vector3 bb) {
            topFrontPoint = tf;
            bottomFrontPoint = bf;
            topBackPoint = tb;
            bottomBackPoint = bb;
        }
        
        public void SetBoundsPoints (Vector3 tf, Vector3 bf, Vector3 tb, Vector3 bb) {
            topFrontPoint = tf;
            bottomFrontPoint = bf;
            topBackPoint = tb;
            bottomBackPoint = bb;
        }

        public void LogVectors() {
            String debugMessage = "";

            debugMessage += "Top Front Vector: " + topFrontPoint.ToString();
            debugMessage += "Bottom Front Vector: " + bottomFrontPoint.ToString();
            debugMessage += "Top Back Vector: " + topBackPoint.ToString();
            debugMessage += "Bottom Back Vector: " + bottomBackPoint.ToString();
            
            Debug.Log(debugMessage);
        }
        
        // These points are in local space
        public Vector3 topFrontPoint;
        public Vector3 bottomFrontPoint;
        public Vector3 topBackPoint;
        public Vector3 bottomBackPoint;
    }
    
    //Debug
    [SerializeField] float gizmoLength = 1f;
    private Vector3[] _gizmoPoints;
    
    //Object Info
    [SerializeField] private PortalColor portalColor = PortalColor.None;
    private GameObject _surfaceObject;
    private Collider2D _surfaceObjectCol;
    private RaycastHit2D _hit;
    private List<LayerMask> _layerList;
    
    //References
    private GameObject _player;
    private GameObject _portalManager;
    private GameObject _portalPartner;
    
    //Collider Info
    [SerializeField] private BoxCollider2D mainBC;
    // [SerializeField] private BoxCollider2D topFrontBC;
    // [SerializeField] private BoxCollider2D bottomFrontBC;
    // [SerializeField] private BoxCollider2D topBackBC;
    // [SerializeField] private BoxCollider2D bottomBackBC;
    [SerializeField] private float collisionStep = 0.1f;
    [SerializeField] private int maxCollisionTests = 100;

    public BoundsPoints BoundsCorners { get; private set; }
    
    private void Awake() {
        
    }

    private void Start()
    {
        _surfaceObject = _hit.collider.gameObject;
        _surfaceObjectCol = _hit.collider;
        _player = GameObject.FindWithTag("Player");
        _portalManager = GameObject.FindWithTag("PortalManager");
        _portalPartner = _portalManager.GetComponent<PortalManager>().GetPartnerPortal(portalColor);
        _layerList = PortalManager.Instance.GetLayerList();
        
        OrientPortal();
        
        // Checks for collisions
        AssignCornerPoints();
        bool isPortalValid = CheckCollisions();
        
        // Creates portal
        if (isPortalValid) {
            PortalManager.Instance.AssignPortal(portalColor, gameObject);
        }
    }

    private void Update() {
        
    }

    private void OnTriggerEnter2D(Collider2D col) {
        GameObject obj = col.gameObject;
        if (_layerList.Contains(obj.layer)) {
            return;
        }
        else {
            
        }
    }

    public void SetRaycastHit(RaycastHit2D hitToPass) {
        _hit = hitToPass;
    }

    // todo: rewrite to use local space rather than world space
    private void OrientPortal() {
        Vector2 surfaceNormal = _hit.normal;
        float zValue = Mathf.Atan2(surfaceNormal.y, surfaceNormal.x) * Mathf.Rad2Deg;
        float yValue = 0;
        
        //Debug.Log(_hit.point + " " + player.transform.position);

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
            if (_hit.point.x > _player.transform.position.x && _hit.point.y > _player.transform.position.y) {
                yValue = 180f;
            } 
            else if (_hit.point.x < _player.transform.position.x && _hit.point.y < _player.transform.position.y) {
                yValue = 180f;
            }
        }

        Vector3 rotation = new Vector3(0f, yValue, zValue);
        //Debug.Log("Portal rotation: " + rotation);
        transform.eulerAngles = rotation;
        //mainBC.transform.Rotate(0f, yValue, zValue, Space.World);
        Physics2D.SyncTransforms();
    }
    
    private void AssignCornerPoints()
    {
        Vector3 colliderSize = mainBC.size;
        Vector3 colliderCenter = mainBC.bounds.center;
        Quaternion colliderRotation = mainBC.transform.rotation;

        // Calculate the local position of the corners relative to the collider's center
        Vector3 tf = new Vector3(colliderSize.x * 0.49f, colliderSize.y * 0.49f, 4f);
        Vector3 bf = new Vector3(colliderSize.x * 0.49f, -colliderSize.y * 0.49f, 4f);
        Vector3 tb = new Vector3(-colliderSize.x * 0.5f, colliderSize.y * 0.5f, 4f);
        Vector3 bb = new Vector3(-colliderSize.x * 0.5f, -colliderSize.y * 0.5f, 4f);

        BoundsCorners = new BoundsPoints(tf, bf, tb, bb);
        //BoundsCorners.LogVectors();
    }

    // todo: implement layer mask here, code may be unoptimized
    private bool CheckCollisions() {
        List<Collider2D> colliderList = new List<Collider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        LayerMask mask;

        if (portalColor == PortalColor.Blue) {
            mask = LayerMask.GetMask("Environment", "Orange Portal");
        } else if (portalColor == PortalColor.Orange) {
            mask = LayerMask.GetMask("Environment", "Blue Portal");
        }
        else {
            Debug.Log("Assigned portal has no color.");
            mask = LayerMask.GetMask("Environment");
        }

        // Configure filter's properties
        // filter.minDepth = 4; // Change from 4f -> -4.1f?
        // filter.maxDepth = 4; // Change from 4f -> 4.1f?
        // filter.useDepth = true;
        filter.useTriggers = true;

        int mainBCOverlapCount = mainBC.OverlapCollider(filter, colliderList);

        Collider2D tfOverlapCol = Physics2D.OverlapPoint(transform.TransformPoint(BoundsCorners.topFrontPoint), mask);
        Collider2D bfOverlapCol = Physics2D.OverlapPoint(transform.TransformPoint(BoundsCorners.bottomFrontPoint), mask);
        bool isTbOverlap = _surfaceObjectCol.OverlapPoint(transform.TransformPoint(BoundsCorners.topBackPoint));
        bool isBbOverlap = _surfaceObjectCol.OverlapPoint(transform.TransformPoint(BoundsCorners.bottomBackPoint));
        bool isTouchingPortalPartner = false;

        if (_portalPartner) {
            isTouchingPortalPartner = mainBC.IsTouching(_portalPartner.GetComponent<BoxCollider2D>());
        }

        String debugMessage = "[" + mainBCOverlapCount + "]";

        if (tfOverlapCol) {
            debugMessage += tfOverlapCol.name + ", "; 
        }
        else {
            debugMessage += "null, ";
        }
        
        if (bfOverlapCol) {
            debugMessage += bfOverlapCol.name + ", "; 
        }
        else {
            debugMessage += "null";
        }

        debugMessage += ", " + isTbOverlap + ", " + isBbOverlap;
        
        Debug.Log(debugMessage);
        //Debug.Log("[" + mainBCOverlapCount + "]" + tfOverlapCol.name + ", " + bfOverlapCol.name + ", " + isTbOverlap + ", " + isBbOverlap);

        int moveDirection = 0;
        int collisionMoves = 0;
        
        // Checks if original portal placement touches partner portal
        if (isTouchingPortalPartner) {
            
        }
        
        // Check deletion conditions
        if ((!isTbOverlap && !isBbOverlap) || (tfOverlapCol && bfOverlapCol)) {
            Debug.Log("Portal in invalid position, destroyed.");
            Destroy(gameObject);
            return false;
        }
        
        // Check direction to move in
        if (isBbOverlap && (!isTbOverlap || tfOverlapCol)) {
            // Change move direction to down
            moveDirection = -1;
        }
        else if (isTbOverlap && (!isBbOverlap || bfOverlapCol)) {
            // Change move direction to up
            moveDirection = 1;
        }
        
        while (!(isBbOverlap && isTbOverlap && !tfOverlapCol && !bfOverlapCol)) {
            if (collisionMoves >= maxCollisionTests) {
                Destroy(gameObject);
                return false;
            }
            
            if ((!isTbOverlap && !isBbOverlap) || (tfOverlapCol && bfOverlapCol)) {
                Debug.Log("Portal moved to invalid position during collision check, destroyed.");
                Destroy(gameObject);
                return false;
            }
            
            transform.position += moveDirection * transform.up * collisionStep;
            Physics2D.SyncTransforms();
            tfOverlapCol = Physics2D.OverlapPoint(transform.TransformPoint(BoundsCorners.topFrontPoint), mask);
            bfOverlapCol = Physics2D.OverlapPoint(transform.TransformPoint(BoundsCorners.bottomFrontPoint), mask);
            isTbOverlap = _surfaceObjectCol.OverlapPoint(transform.TransformPoint(BoundsCorners.topBackPoint));
            isBbOverlap = _surfaceObjectCol.OverlapPoint(transform.TransformPoint(BoundsCorners.bottomBackPoint));
            collisionMoves++;
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;

        Vector3 _objectPos = Vector3.zero;
        Vector3 _arrowTip = _objectPos + (Vector3.right * gizmoLength);
        
        _gizmoPoints = new Vector3[6]
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
        Gizmos.DrawLineList(_gizmoPoints);
        
        // Draws cube at portal bottom
        Gizmos.DrawWireCube(
            new Vector3(0, -transform.localScale.y/2, 0), 
            new Vector3(0.8f, 0.3f, 1));

        // Restore the original Gizmos matrix
        Gizmos.matrix = originalMatrix;
        
        //Gizmos.DrawLine(_objectPos, _objectPos + (transform.right * gizmoLength));
    }
}
