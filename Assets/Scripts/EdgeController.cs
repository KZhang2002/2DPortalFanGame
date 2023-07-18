using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EdgeController : MonoBehaviour {
    [SerializeField] private EdgeCollider2D topEdge;
    [SerializeField] private EdgeCollider2D bottomEdge;
    
    // Start is called before the first frame update
    void Start() {
        DisableEdges();
    }

    public void DisableEdges() {
        topEdge.enabled = false;
        bottomEdge.enabled = false;
    }
    
    public void EnableEdges() {
        topEdge.enabled = true;
        bottomEdge.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
