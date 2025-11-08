using UnityEngine;

public class movingBckg : MonoBehaviour
{
    public float scrollSpeed = 450f;
    public float leftBoundary = -13f; 
    public float rightBoundary = 13f;  

    public int moveDirection = -1; 

    void Update()
    { 
        transform.Translate(Vector3.right * moveDirection * scrollSpeed * Time.deltaTime);
         
        if (transform.localPosition.x < leftBoundary)
        {
            moveDirection = 1;
        }
        else if (transform.localPosition.x > rightBoundary)
        {
            moveDirection = -1;
        }
    }
}