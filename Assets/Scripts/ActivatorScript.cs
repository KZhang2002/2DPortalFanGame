using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ActivatorScript : MonoBehaviour
{
    [SerializeField] UnityEvent onFirstEnter = default, onLastExit = default;
    List<Collider2D> colliders = new List<Collider2D>();

    private void Awake() {
        enabled = false;
    }

    private void FixedUpdate() {
        for (int i = 0; i < colliders.Count; i++) {
            Collider2D collider = colliders[i];
            if (!collider || !collider.gameObject.activeInHierarchy) {
                colliders.RemoveAt(i--);
                if (colliders.Count == 0) {
                    onLastExit.Invoke();
                    enabled = false;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Physics Object") || other.CompareTag("Player")) {
            if (colliders.Count == 0) {
                onFirstEnter.Invoke();
                enabled = true;
            }
            colliders.Add(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Physics Object") || other.CompareTag("Player")) {
            if (colliders.Remove(other) && colliders.Count == 0) {
                onLastExit.Invoke();
                enabled = false;
            }
        }
    }
}
