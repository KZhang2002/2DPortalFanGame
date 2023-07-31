using System;
using System.Collections;
using System.Collections.Generic;
using TarodevController;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class WeaponController : MonoBehaviour {
    // Shooting config
    [SerializeField] private float shootRayDistance = 50f;
    [SerializeField] private float interactRayDistance = 2f;
    
    // References
    [SerializeField] private GameObject objectBlue;
    [SerializeField] private GameObject objectOrange;
    [SerializeField] private CapsuleCollider2D standingCollider;
    [SerializeField] private CapsuleCollider2D crouchingCollider;
    private PlayerController _pc;
    private Rigidbody2D _rb;
    private GameObject _heldObj;
    private Rigidbody2D _heldObjRb;
    private PortalManager _pm;
    
    // Held object config
    [SerializeField] private float heldObjMoveSpeed = 1000f;
    [SerializeField] private float heldObjRotationSpeed = 1000f;
    [SerializeField] private float heldObjDistance = 1.5f;
    [SerializeField] private float heldObjMargin = 0.5f;
    [SerializeField] private float objTeleportThreshold = 1f;
    
    // Held object data
    private Vector3? _heldObjPreviousPos;
    private Vector3 _playerPreviousPos;
    private bool _heldObjHasTeleported = false;
    private bool _playerHasTeleported = false;
    
    public RaycastHit2D Hit { get; private set; }
    public GameObject LastObjectSpawned { get; private set; }
    private Camera _mainCamera;
    public Vector2 MousePosition { get; private set; }
    public Vector2 WorldMousePosition { get; private set; }
    public Vector2 Direction { get; private set; }
    public Vector2 PlayerCenter { get; private set; }
    [SerializeField] private LayerMask shootMask;
    [SerializeField] private LayerMask interactMask;
    

    enum ShootType {
        spawn,
        interact,
        damage
    }
    
    void Awake() {
        _pc = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main;
        _pm = GameObject.FindWithTag("PortalManager").GetComponent<PortalManager>();
        _playerPreviousPos = transform.position;
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetButtonDown("Fire1")) {
            Shoot(ShootType.spawn, objectBlue);
        }
        else if (Input.GetButtonDown("Fire2")) {
            Shoot(ShootType.spawn, objectOrange);
        }
        else if (Input.GetButtonDown("Interact")) {
            Shoot(ShootType.interact);
        }
    }

    private void FixedUpdate() {
        MousePosition = Input.mousePosition;
        WorldMousePosition = _mainCamera.ScreenToWorldPoint(new Vector2(MousePosition.x, MousePosition.y));
        PlayerCenter = GetPlayerCenter();
        Direction = (WorldMousePosition - PlayerCenter).normalized;

        CheckForTeleports();
        
        if (_heldObj) {
            _heldObjPreviousPos ??= _heldObj.transform.position;
            HeldObjInteraction();
        }
    }

    private void LateUpdate() {
        
    }

    void CheckForTeleports() {
        float playerDisplacement = Vector3.Distance(transform.position, _playerPreviousPos);
        Debug.DrawRay(transform.position, Vector3.up, Color.red);
        Debug.DrawRay(_playerPreviousPos, Vector3.up, Color.green);
        
        // if (playerDisplacement > 0.01f) {
        //     Debug.Log($"player displacement: {playerDisplacement}");
        // }
        
        if (playerDisplacement > objTeleportThreshold) {
            Debug.Log($"Player has been teleported with a displacement of {playerDisplacement}");
            _playerHasTeleported = !_playerHasTeleported;
            Debug.Log($"_playerHasTeleported: {_playerHasTeleported}, _heldObjHasTeleported: {_heldObjHasTeleported}");
            // Debug.Break();
        }

        if (_heldObjPreviousPos != null) {
            float objDisplacement = Vector3.Distance(_heldObj.transform.position, _heldObjPreviousPos.Value);

            // if (displacement > 0.01f) {
            //     Debug.Log($"heldObj displacement: {displacement}");
            // }

            if (objDisplacement > objTeleportThreshold) {
                Debug.Log($"Held object has been teleported with a displacement of {objDisplacement}");
                _heldObjHasTeleported = !_heldObjHasTeleported;
                Debug.Log($"_playerHasTeleported: {_playerHasTeleported}, _heldObjHasTeleported: {_heldObjHasTeleported}");
                // Debug.Break();
            }
            
            _heldObjPreviousPos = _heldObj.transform.position;
        }
        
        if (_heldObjHasTeleported && _playerHasTeleported) {
            _heldObjHasTeleported = false;
            _playerHasTeleported = false;
            Debug.Log($"_playerHasTeleported: {_playerHasTeleported}, _heldObjHasTeleported: {_heldObjHasTeleported}");
            
            //DropHeldObject();
        }

        _playerPreviousPos = transform.position;
    }

    Vector2 FindIntersectionPoint(Vector2 _p1, Vector2 _d1, Vector2 _p2, Vector2 _d2)
    {
        // Check if the rays are parallel using the determinant of their direction vectors
        float det = _d1.x * _d2.y - _d1.y * _d2.x;

        if (Mathf.Approximately(det, 0f))
        {
            // The rays are parallel or collinear, no unique intersection point
            return Vector2.zero;
        }

        // Solve for the parameters t1 and t2
        float t1 = (_d2.x * (_p1.y - _p2.y) - _d2.y * (_p1.x - _p2.x)) / det;
        float t2 = (_d1.x * (_p1.y - _p2.y) - _d1.y * (_p1.x - _p2.x)) / det;

        // Check if the intersection point lies within both rays (t >= 0)
        if (t1 >= 0f && t2 >= 0f)
        {
            // Calculate the intersection point
            Vector2 intersection = _p1 + t1 * _d1;
            return intersection;
        }

        // The intersection point does not lie within both rays
        return Vector2.zero;
    }

    Collider2D GetPlayerCollider() {
        if (_pc.Crouching) {
            return crouchingCollider;
        }
        else {
            return standingCollider;
        }
    }
    
    Vector3 GetPlayerCenter() {
        if (_pc.Crouching) {
            return crouchingCollider.bounds.center;
        }
        else {
            return standingCollider.bounds.center;
        }
    }

    void Shoot(ShootType shootType, GameObject objectToSpawn = null) {
        // Shooting mechanics
        LayerMask mask = 0;
        float raycastDistance = 0f;

        if (shootType == ShootType.spawn) {
            raycastDistance = shootRayDistance;
            mask = shootMask;
        }
        else if (shootType == ShootType.interact) {
            raycastDistance = interactRayDistance;
            mask = interactMask;
        }

        Hit = Physics2D.Raycast(PlayerCenter, Direction, raycastDistance, mask);
        Debug.DrawRay(PlayerCenter, Direction * raycastDistance, Color.green, 1);

        if (Hit.collider != null) {
            if (shootType == ShootType.spawn && objectToSpawn) {
                // Handle the hit result, e.g., apply damage, trigger effects, etc.
                LastObjectSpawned = Instantiate(objectToSpawn, Hit.point, Quaternion.identity);
                // Debug.Log(LastObjectSpawned.name);
                LastObjectSpawned.GetComponent<PortalScript>().SetRaycastHit(Hit);
            } 
            else if (shootType == ShootType.interact) {
                Interact(Hit);
            }
        }
        else if (_heldObj) {
            DropHeldObject();
        }
    }

    void Interact(RaycastHit2D ray) {
        GameObject obj = ray.collider.gameObject;

        if (_heldObj) {
            DropHeldObject();
            return;
        }
        
        if (obj.CompareTag("Physics Object")) {
            _heldObj = obj;
            _heldObjRb = obj.GetComponent<Rigidbody2D>();
            
            Debug.Log("Interacted with " + _heldObj);
            return;
        }
        else if (obj.CompareTag("Interactable")) {
            
        }
    }

    void DropHeldObject() {
        Debug.Log("Dropped " + _heldObj);
            
        _heldObj = null;
        _heldObjRb = null;
        _heldObjPreviousPos = null;
        _heldObjHasTeleported = false;
        _playerHasTeleported = false;
    }

    //todo: optimize / add los checks for held objects
    void HeldObjInteraction() {
        Vector2 colEdgePoint = GetPlayerCollider().ClosestPoint(PlayerCenter + Direction);
        Vector2 currentPosition = _heldObjRb.transform.position;
        Vector2 moveToPos = colEdgePoint + Direction * heldObjDistance;
        float fullHoldDistance = Vector2.Distance(colEdgePoint, PlayerCenter) + (Direction * heldObjDistance).magnitude;
        _heldObjPreviousPos ??= _heldObj.transform.position;
        
        Debug.DrawLine(GetPlayerCollider().ClosestPoint(PlayerCenter + Direction), PlayerCenter, Color.cyan, 0f);
        //Debug.DrawRay(PlayerCenter + Direction, Vector3.up, Color.cyan);

        //Holding object through a portal interaction
        if ((_pm.BluePortal && _pm.OrangePortal) && (_heldObjHasTeleported != _playerHasTeleported)) {
            LayerMask hitMask = LayerMask.GetMask("Orange Portal", "Blue Portal");
            RaycastHit2D hit = Physics2D.Raycast(PlayerCenter, Direction, heldObjDistance, hitMask);
            Collider2D hitCollider = hit ? hit.collider : Physics2D.OverlapPoint(PlayerCenter, hitMask);
            
            if (hitCollider && hitCollider.gameObject.CompareTag("Portal")) {
                PortalScript portalScript = hitCollider.gameObject.GetComponent<PortalScript>();
                GameObject entrancePortal;
                GameObject exitPortal;

                if (portalScript.GetColor() == PortalColor.Blue) {
                    entrancePortal = _pm.BluePortal;
                    exitPortal = _pm.OrangePortal;
                }
                else {
                    entrancePortal = _pm.OrangePortal;
                    exitPortal = _pm.BluePortal;
                }

                // Ensure the directions are normalized (unit vectors).
                Vector3 aimRayDirection = Direction;
                Vector3 portalThresholdRayDirection = entrancePortal.transform.up;
                Vector3 aimOrigin = PlayerCenter;
                Vector3 portalRayOrigin = entrancePortal.transform.TransformPoint(new Vector3(0f, -0.5f, 0f));

                // Calculates intersection point of player's aim and entrance portal's threshold
                Vector3 intersectionPoint =
                    FindIntersectionPoint(aimOrigin, aimRayDirection, portalRayOrigin, portalThresholdRayDirection);

                Debug.DrawRay(aimOrigin, aimRayDirection * fullHoldDistance, Color.red);
                Debug.DrawRay(portalRayOrigin, portalThresholdRayDirection * 10f, Color.red);
                Debug.DrawRay(intersectionPoint, Vector3.right * 1f, Color.red);
                
                // Debug.Log($"player to portal dist: {Vector2.Distance(intersectionPoint, PlayerCenter)}");
                // Debug.Log($"fullHoldDist: {fullHoldDistance}");

                Vector3 playerToPortal = (hitCollider.transform.position - transform.position).normalized;
                float aimAngleToPortal = Vector3.Dot(Direction, playerToPortal);
                Debug.Log($"hitCollider: {hitCollider.gameObject.name}");

                if (aimAngleToPortal < 0f || !hitCollider.OverlapPoint(intersectionPoint)) {
                    DropHeldObject();
                    return;
                }
                
                if (
                    intersectionPoint != Vector3.zero &&
                    Vector2.Distance(intersectionPoint, PlayerCenter) < fullHoldDistance
                    ) 
                {
                    Vector3 offset = intersectionPoint - entrancePortal.transform.position;
                    Vector3 offsetInWorldSpace = entrancePortal.transform.InverseTransformVector(offset);
                    Vector3 offsetInExitLocalSpace = exitPortal.transform.TransformVector(offsetInWorldSpace);
                    Vector3 newPos = exitPortal.transform.position + offsetInExitLocalSpace;

                    //Move Direction from world space to local space (entrance) to local space (exit)
                    Vector3 newDirection = entrancePortal.transform.InverseTransformDirection(Direction);
                    newDirection = exitPortal.transform.TransformDirection(newDirection);

                    //Rotates the direction vector 180 degrees around the exit portals up axis
                    newDirection = Quaternion.AngleAxis(180f, exitPortal.transform.up) * newDirection;

                    float firstHalfDistance = Vector2.Distance(aimOrigin, intersectionPoint);
                    float secondHalfDistance = fullHoldDistance - firstHalfDistance;
                    
                    //Debug.Log($"fullHoldDist: {fullHoldDistance}, firstHalf: {firstHalfDistance}, secondHalf: {secondHalfDistance}");

                    Debug.DrawRay(aimOrigin, aimRayDirection * firstHalfDistance, Color.magenta, 0f);
                    Debug.DrawRay(newPos, exitPortal.transform.up, Color.cyan, 0f);
                    Debug.DrawRay(newPos, newDirection * secondHalfDistance, Color.magenta, 0f);
                    
                    moveToPos = new Ray2D(newPos, newDirection).GetPoint(secondHalfDistance);
                }
                else {
                    Vector3 offset = intersectionPoint - entrancePortal.transform.position;
                    Vector3 offsetInWorldSpace = entrancePortal.transform.InverseTransformVector(offset);
                    Vector3 offsetInExitLocalSpace = exitPortal.transform.TransformVector(offsetInWorldSpace) - exitPortal.transform.right;
                    Vector3 pointBehindExit = exitPortal.transform.position + offsetInExitLocalSpace;

                    moveToPos = pointBehindExit;
                }
            }
            else {
                _heldObjHasTeleported = false;
                _playerHasTeleported = false;
                // Debug.Log($"_playerHasTeleported: {_playerHasTeleported}, _heldObjHasTeleported: {_heldObjHasTeleported}");
                
                // DropHeldObject();
            }
        }
        else {
            Debug.DrawRay(moveToPos, Vector3.up, Color.cyan);
            if (Vector2.Distance(_heldObj.transform.position, moveToPos) > heldObjMargin) {
                DropHeldObject();
                return;
            }
        }

        Debug.DrawRay(moveToPos, Vector3.up, Color.cyan);
        // Debug.Log($"heldObjMoveSpeed: {heldObjMoveSpeed}, playerSpeed: {_pc.Speed.magnitude}");
        
        Vector3 newPosition = Vector3.MoveTowards(
            currentPosition, moveToPos, Mathf.Max(heldObjMoveSpeed, _pc.Speed.magnitude * 2f) * Time.deltaTime);
        Quaternion newRotation = Quaternion.RotateTowards(
            Quaternion.Euler(0f, 0f, _heldObjRb.rotation), Quaternion.identity, heldObjRotationSpeed * Time.deltaTime);
        
        _heldObjRb.MovePosition(newPosition);
        _heldObjRb.MoveRotation(newRotation);
    }
}