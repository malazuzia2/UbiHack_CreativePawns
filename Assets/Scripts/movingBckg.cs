using UnityEngine;

public class movingBckg : MonoBehaviour
{
    public float scrollSpeed = 450f;
    public float leftBoundary = -13f; // Lewa granica, do której obiekt siê przesuwa
    public float rightBoundary = 13f;  // Prawa granica, do której obiekt siê przesuwa

    public int moveDirection = -1; // 1 dla prawo, -1 dla lewo (domyœlnie zaczyna w prawo)

    void Update()
    { 
        transform.Translate(Vector3.right * moveDirection * scrollSpeed * Time.deltaTime);
         
        if (transform.localPosition.x < leftBoundary)
        {
            moveDirection = 1; // Zmieñ kierunek na prawo
        }
        // SprawdŸ, czy obiekt przekroczy³ praw¹ granicê
        else if (transform.localPosition.x > rightBoundary)
        {
            moveDirection = -1; // Zmieñ kierunek na lewo
        }
    }
}