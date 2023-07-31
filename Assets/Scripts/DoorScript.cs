using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class DoorScript : MonoBehaviour {
    public bool IsOpen { get; private set; }

    [SerializeField] private Collider2D doorCol;
    [SerializeField] private SpriteRenderer sprite;

    private void Awake() {
        IsOpen = false;
    }

    public void OpenDoor() {
        IsOpen = true;
        doorCol.enabled = false;
        sprite.enabled = false;
    }

    public void CloseDoor() {
        IsOpen = false;
        doorCol.enabled = true;
        sprite.enabled = true;
    }
}
