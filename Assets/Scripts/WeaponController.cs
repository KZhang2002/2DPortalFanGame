using System.Collections;
using System.Collections.Generic;
using TarodevController;
using UnityEngine;

public class WeaponController : MonoBehaviour {
    [SerializeField] private float raycastDistance = 50f;
    [SerializeField] private GameObject objectBlue;
    [SerializeField] private GameObject objectOrange;
    [SerializeField] private CapsuleCollider2D standingCollider;
    [SerializeField] private CapsuleCollider2D crouchingCollider;
    private PlayerController _pc;
    private Rigidbody2D _rb;
    public RaycastHit2D Hit { get; private set; }
    public GameObject LastObjectSpawned { get; private set; }
    private Camera _mainCamera;
    

    // Start is called before the first frame update
    void Awake() {
        _pc = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetButtonDown("Fire1")) {
            Shoot(objectBlue);
        }
        else if (Input.GetButtonDown("Fire2")) {
            Shoot(objectOrange);
        }
    }

    void Shoot(GameObject objectToSpawn) {
        // Shooting mechanics
        Vector3 mousePosition = Input.mousePosition;
        Vector3 worldMousePosition = _mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, _mainCamera.nearClipPlane));
        LayerMask mask = LayerMask.GetMask("Environment");
        Vector3 playerCenter;

        if (_pc.Crouching) {
            playerCenter = _rb.position + standingCollider.offset;
        }
        else {
            playerCenter = _rb.position + crouchingCollider.offset;
        }
        
        Vector2 direction = worldMousePosition - playerCenter;
        direction.Normalize();
        

        Hit = Physics2D.Raycast(playerCenter, direction, raycastDistance, mask);
        Debug.DrawRay(playerCenter, direction * 50f, Color.green, 1);

        if (Hit.collider != null) {
            // Handle the hit result, e.g., apply damage, trigger effects, etc.
            LastObjectSpawned = Instantiate(objectToSpawn, Hit.point, Quaternion.identity);
            // Debug.Log(LastObjectSpawned.name);
            LastObjectSpawned.GetComponent<PortalScript>().SetRaycastHit(Hit);
        }
    }
}