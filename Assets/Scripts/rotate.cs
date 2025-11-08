using UnityEngine;

public class rotate : MonoBehaviour
{
    public float rotateSpeed = 450f;
    public float moveDirection = -1f; // 1 dla prawo, -1 dla lewo (domyœlnie zaczyna w prawo)
                                      // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 0, moveDirection * rotateSpeed * Time.deltaTime);
    }
}
