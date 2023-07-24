using System;
using System.Collections;
using System.Collections.Generic;
using TarodevController;
using Unity.VisualScripting;
using UnityEngine;

public class PortalScript : MonoBehaviour
{
    public struct BoundsPoints {
        public BoundsPoints (Vector3 tf, Vector3 bf, Vector3 tb, Vector3 bb, Vector3 cen) {
            topFrontPoint = tf;
            bottomFrontPoint = bf;
            topBackPoint = tb;
            bottomBackPoint = bb;
            center = cen;
        }
        
        public void SetBoundsPoints (Vector3 tf, Vector3 bf, Vector3 tb, Vector3 bb, Vector3 cen) {
            topFrontPoint = tf;
            bottomFrontPoint = bf;
            topBackPoint = tb;
            bottomBackPoint = bb;
            center = cen;
        }

        public void LogVectors() {
            String debugMessage = "";

            debugMessage += "Top Front Vector: " + topFrontPoint.ToString();
            debugMessage += "Bottom Front Vector: " + bottomFrontPoint.ToString();
            debugMessage += "Top Back Vector: " + topBackPoint.ToString();
            debugMessage += "Bottom Back Vector: " + bottomBackPoint.ToString();
            debugMessage += "Center Vector: " + center.ToString();
            
            Debug.Log(debugMessage);
        }
        
        // These points are in local space
        public Vector3 topFrontPoint;
        public Vector3 bottomFrontPoint;
        public Vector3 topBackPoint;
        public Vector3 bottomBackPoint;
        public Vector3 center;
    }
    
    //Debug
    [SerializeField] float gizmoLength = 1f;
    private Vector3[] _gizmoPoints;
    
    //Object Info
    [SerializeField] private PortalColor portalColor = PortalColor.None;
    private GameObject _surfaceObject;
    private Collider2D _surfaceObjectCol;
    private RaycastHit2D _hit;
    private List<int> _portalBlockList;
    
    //References
    private GameObject _player;
    private PlayerController _playerController;
    private GameObject _portalManager;
    private GameObject _portalPartner;
    private EdgeController _edgeController;
    
    //Collider Info
    [SerializeField] private BoxCollider2D boxCol;
    [SerializeField] private PolygonCollider2D polyCol;
    [SerializeField] private GameObject portalEdgeObj;
    private CapsuleCollider2D _standingCollider;
    private CapsuleCollider2D _crouchingCollider;
    
    //Collider Detection Options
    [SerializeField] private float collisionStep = 0.1f;
    [SerializeField] private int maxCollisionTests = 100;
    [SerializeField] private float portalPlayerEntryThreshold = 0.25f;
    [SerializeField] private float portalPlayerExitOffset = 0.25f;
    [SerializeField] private float portalObjectExitOffset = 0.25f;

    public BoundsPoints BoundsCorners { get; private set; }
    
    private void Awake() {
        
    }

    private void Start()
    {
        //References initialization
        _surfaceObject = _hit.collider.gameObject;
        _surfaceObjectCol = _hit.collider;
        _player = GameObject.FindWithTag("Player");
        _playerController = _player.GetComponent<PlayerController>();
        _standingCollider = _playerController.StandingColliderRef;
        _crouchingCollider = _playerController.CrouchingColliderRef;
        _portalManager = GameObject.FindWithTag("PortalManager");
        _portalPartner = _portalManager.GetComponent<PortalManager>().GetPartnerPortal(portalColor);
        _portalBlockList = PortalManager.Instance.GetPortalBlockList();
        _edgeController = portalEdgeObj.GetComponent<EdgeController>();

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
        if (_portalPartner) {
            Debug.Log(portalColor + " portal entered.");
            _edgeController.EnableEdges();
            Physics2D.IgnoreCollision(col, _surfaceObjectCol, true);
            //Debug.Log("Portal edges enabled. Surface collider disabled.");
            CheckPortalCollision(col);
        }
        
    }

    private void OnTriggerStay2D(Collider2D col) {
        if (_portalPartner) {
            CheckPortalCollision(col);
        }
    }

    private void OnTriggerExit2D(Collider2D col) {
        if (_portalPartner) {
            Debug.Log(portalColor + " portal exited.");
            _edgeController.DisableEdges();
            Physics2D.IgnoreCollision(col, _surfaceObjectCol, false);
            //Debug.Log("Portal edges disabled. Surface collider enabled.");
        }
        
    }

    private void CheckPortalCollision(Collider2D col) {
        Collider2D playerCol;

        if (_playerController.Crouching) {
            playerCol = _playerController.CrouchingColliderRef;
        }
        else {
            playerCol = _playerController.StandingColliderRef;
        }

        if (col.CompareTag("Player")) {
            if (polyCol.OverlapPoint(playerCol.bounds.center)) {
                Debug.Log(col.name + " intersects object");
                OnEnterPortal(col);
            }
        }
        else {
            if (polyCol.OverlapPoint(col.bounds.center)) {
                Debug.Log(col.name + " intersects object");
                OnEnterPortal(col);
            }
        }
        

        // if (col.CompareTag("Player")) {
        //     if (edgeCol.Distance(col).distance < -portalPlayerEntryThreshold) {
        //         OnEnterPortal(col);
        //         Debug.Log("Player teleported");
        //     }
        // }
        // else {
        //     if (edgeCol.Distance(col).distance < -col.bounds.extents.x) {
        //         OnEnterPortal(col);
        //         Debug.Log("Object teleported");
        //     }
        // }
    }

    private void OnEnterPortal(Collider2D col) {
        GameObject obj = col.gameObject;
        
        if (_portalBlockList.Contains(obj.layer) || !_portalPartner) {
            Debug.Log("Object entering portal is on invalid layer: " + LayerMask.LayerToName(obj.layer));
            return;
        }
        
        Vector3 offset = col.transform.position - transform.position;
        Vector3 offCenterOffset;

        if (col.CompareTag("Player")) {
            offCenterOffset = transform.TransformDirection(Vector3.right) * portalPlayerExitOffset;
        }
        else {
            offCenterOffset = transform.TransformDirection(Vector3.right) * portalObjectExitOffset;
        }

        col.transform.position = _portalPartner.transform.position + offset + offCenterOffset;
        
        Vector2 targetNormal = _portalPartner.GetComponent<PortalScript>()._hit.normal;
        float dotProduct;
        Vector2 newVelocity;
        
        if (col.CompareTag("Player")) {
            dotProduct = Vector2.Dot(_playerController.Velocity, targetNormal);
            newVelocity = dotProduct * targetNormal;
            _playerController.SetVelocity(newVelocity, PlayerForce.Burst);
        }
        else {
            dotProduct = Vector3.Dot(col.attachedRigidbody.velocity, targetNormal);
            newVelocity = dotProduct * targetNormal;
            col.attachedRigidbody.velocity = newVelocity;
        }
    }

    //I think theres an easier/shorter way of making setters here
    //todo: condense
    public void SetPortalPartner(GameObject portal) {
        _portalPartner = portal;
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
        Vector3 colliderSize = boxCol.size;
        Vector3 colliderCenter = boxCol.bounds.center;
        Quaternion colliderRotation = boxCol.transform.rotation;

        // Calculate the local position of the corners relative to the collider's center
        Vector3 tf = new Vector3(colliderSize.x * 0.49f, colliderSize.y * 0.49f, 4f);
        Vector3 bf = new Vector3(colliderSize.x * 0.49f, -colliderSize.y * 0.49f, 4f);
        Vector3 tb = new Vector3(-colliderSize.x * 0.5f, colliderSize.y * 0.5f, 4f);
        Vector3 bb = new Vector3(-colliderSize.x * 0.5f, -colliderSize.y * 0.5f, 4f);
        Vector3 center = new Vector3(0, 0, 4f);

        BoundsCorners = new BoundsPoints(tf, bf, tb, bb, center);
        //BoundsCorners.LogVectors();
    }

    // todo: implement layer mask here, code may be unoptimized
    private bool CheckCollisions() {
        // List<Collider2D> colliderList = new List<Collider2D>();
        // ContactFilter2D filter = new ContactFilter2D();
        LayerMask mask;
        bool isTouchingPortalPartner = false;
        BoxCollider2D partnerBoxCol = null;
        
        if (_portalPartner) {
            partnerBoxCol = _portalPartner.GetComponent<BoxCollider2D>();
            partnerBoxCol.enabled = true;
            //Physics2D.SyncTransforms();
            isTouchingPortalPartner = boxCol.IsTouching(partnerBoxCol);
            //Debug.Log(isTouchingPortalPartner);
        }

        if (portalColor == PortalColor.Blue) {
            mask = LayerMask.GetMask("Environment", "Orange Portal");
        } else if (portalColor == PortalColor.Orange) {
            mask = LayerMask.GetMask("Environment", "Blue Portal");
        }
        else {
            Debug.Log("New portal has no color.");
            mask = LayerMask.GetMask("Environment");
        }

        // Configure filter's properties
        // filter.minDepth = 4; // Change from 4f -> -4.1f?
        // filter.maxDepth = 4; // Change from 4f -> 4.1f?
        // filter.useDepth = true;
        // filter.useTriggers = true;

        // int mainBCOverlapCount = boxCol.OverlapCollider(filter, colliderList);

        Collider2D tfOverlapCol = Physics2D.OverlapPoint(transform.TransformPoint(BoundsCorners.topFrontPoint), mask);
        Collider2D bfOverlapCol = Physics2D.OverlapPoint(transform.TransformPoint(BoundsCorners.bottomFrontPoint), mask);
        bool isTbOverlap = _surfaceObjectCol.OverlapPoint(transform.TransformPoint(BoundsCorners.topBackPoint));
        bool isBbOverlap = _surfaceObjectCol.OverlapPoint(transform.TransformPoint(BoundsCorners.bottomBackPoint));
        
        // String debugMessage = "[" + mainBCOverlapCount + "]";
        // if (tfOverlapCol) {
        //     debugMessage += tfOverlapCol.name + ", "; 
        // }
        // else {
        //     debugMessage += "null, ";
        // }
        // if (bfOverlapCol) {
        //     debugMessage += bfOverlapCol.name + ", "; 
        // }
        // else {
        //     debugMessage += "null";
        // }
        // debugMessage += ", " + isTbOverlap + ", " + isBbOverlap;
        // Debug.Log(debugMessage);
        //Debug.Log("[" + mainBCOverlapCount + "]" + tfOverlapCol.name + ", " + bfOverlapCol.name + ", " + isTbOverlap + ", " + isBbOverlap);

        int moveDirection = 0;
        int collisionMoves = 0;

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

        boxCol.enabled = false;
        
        if (_portalPartner && partnerBoxCol) {
            partnerBoxCol.enabled = false;
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
            new Vector3(0.3f, 0.2f, 1));

        // Restore the original Gizmos matrix
        Gizmos.matrix = originalMatrix;
        
        //Gizmos.DrawLine(_objectPos, _objectPos + (transform.right * gizmoLength));
    }
}
