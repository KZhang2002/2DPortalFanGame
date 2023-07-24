using System.Collections;
using System.Collections.Generic;
using TarodevController;
using UnityEditor;
using UnityEngine;

public class WeaponController : MonoBehaviour {
    [SerializeField] private float shootRayDistance = 50f;
    [SerializeField] private float interactRayDistance = 2f;
    [SerializeField] private GameObject objectBlue;
    [SerializeField] private GameObject objectOrange;
    [SerializeField] private CapsuleCollider2D standingCollider;
    [SerializeField] private CapsuleCollider2D crouchingCollider;
    private PlayerController _pc;
    private Rigidbody2D _rb;
    private GameObject _heldObj;
    private Rigidbody2D _heldObjRb;
    [SerializeField] private float heldObjMoveSpeed = 1000f;
    [SerializeField] private float heldObjRotationSpeed = 1000f;
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
    
    // Start is called before the first frame update
    void Awake() {
        _pc = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update() {
        MousePosition = Input.mousePosition;
        WorldMousePosition = _mainCamera.ScreenToWorldPoint(new Vector2(MousePosition.x, MousePosition.y));
        PlayerCenter = GetPlayerCollider().bounds.center;
        Direction = (WorldMousePosition - PlayerCenter).normalized;

        if (_heldObj) {
            HeldObjInteraction();
        }
        
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

    Collider2D GetPlayerCollider() {
        if (_pc.Crouching) {
            return crouchingCollider;
        }
        else {
            return standingCollider;
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
    }

    void Interact(RaycastHit2D ray) {
        GameObject obj = ray.collider.gameObject;

        if (_heldObj) {
            Debug.Log("Dropped " + _heldObj);
            
            _heldObj = null;
            _heldObjRb = null;
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

    void HeldObjInteraction() {
        Vector2 currentPosition = _heldObjRb.transform.position;
        Vector2 moveToPos = GetPlayerCollider().ClosestPoint(currentPosition) + Direction * 1.5f;
        Vector2 newPosition = Vector2.MoveTowards(currentPosition, moveToPos, heldObjMoveSpeed * Time.deltaTime);
        Quaternion newRotation = Quaternion.RotateTowards( Quaternion.Euler(0f, 0f, _heldObjRb.rotation), 
            Quaternion.identity, heldObjRotationSpeed * Time.deltaTime);
        
        _heldObjRb.MovePosition(newPosition);
        _heldObjRb.MoveRotation(newRotation);
    }
}