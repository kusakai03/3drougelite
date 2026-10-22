using TMPro;
using UnityEngine;

public class FloatingDamage : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float lifeTime = 0.8f;
    [SerializeField] private float moveSpeed = 1.5f;

    private float timer;

    public void Setup(float damage, Color color)
    {
        text.text = damage > 0 ? Mathf.RoundToInt(damage).ToString() : "";
        text.color = color;
    }

    private void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;

        timer += Time.deltaTime;

        if (timer >= lifeTime)
            Destroy(gameObject);
    }
}