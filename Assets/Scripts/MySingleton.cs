using UnityEngine;

public class MySingleton : MonoBehaviour
{
    private static MySingleton _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // Destroy the old instance
            Destroy(_instance.gameObject);
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}