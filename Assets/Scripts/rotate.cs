using UnityEngine;

public class rotate : MonoBehaviour
{
    public float rotateSpeed = 450f;
    public float moveDirection = -1f; 

    void Update()
    {
        transform.Rotate(0, 0, moveDirection * rotateSpeed * Time.deltaTime);
    }
}
