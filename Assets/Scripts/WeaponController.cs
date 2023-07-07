using System.Collections;
using System.Collections.Generic;
using TarodevController;
using UnityEngine;

public class WeaponController : MonoBehaviour {
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private GameObject objectBlue;
    [SerializeField] private GameObject objectOrange;
    [SerializeField] private CapsuleCollider2D standingCollider;
    [SerializeField] private CapsuleCollider2D crouchingCollider;
    private PlayerController _pc;
    private Rigidbody2D _rb;

    // Start is called before the first frame update
    void Awake() {
        _pc = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody2D>();
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
        Vector3 mousePosition = Input.mousePosition;
        Vector3 worldMousePosition =
            Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Camera.main.nearClipPlane));

        Vector2 direction = worldMousePosition - transform.position;
        direction.Normalize();

        Vector3 playerCenter;

        if (_pc.Crouching) {
            playerCenter = _rb.position + standingCollider.offset;
        }
        else {
            playerCenter = _rb.position + crouchingCollider.offset;
        }

        RaycastHit2D hit = Physics2D.Raycast(playerCenter, direction, raycastDistance);
        Debug.DrawRay(playerCenter, direction * 100f, Color.green, 1);

        if (hit.collider != null) {
            Vector2 surfaceNormal = hit.normal;
            float zValue = Mathf.Atan2(surfaceNormal.y, surfaceNormal.x) * Mathf.Rad2Deg;
            float yValue = 0;

            // Adjusts rotation of portal so bottom of portal always 
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
            // Adjusts rotation of portal so bottom of portal 
            // faces the player when placed on ceiling or floor
            else if (zValue is -90f or 90f) {
                // If this code is pulled out of the player, change transform.position.x
                // to the transform of the object specifically
                if (hit.point.x < transform.position.x) {
                    yValue = 180f;
                }
            }

            Quaternion rotation =
                Quaternion.Euler(0f, yValue, zValue);

            // Handle the hit result, e.g., apply damage, trigger effects, etc.
            Instantiate(objectToSpawn, hit.point, rotation);

            Debug.Log("Raycast hit: " + hit.collider.gameObject.name);
        }
    }
}