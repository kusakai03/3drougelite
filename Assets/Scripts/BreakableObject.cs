using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [SerializeField] private GameObject brokenPrefab;

    public void Break()
    {
        if (brokenPrefab != null)
        {
            GameObject broken = Instantiate(brokenPrefab, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }
}