using UnityEngine;

public class TrainCar : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        // Grab the SpriteRenderer so we can change the car's color
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color; // Remember its default color (Red)
    }

    // Unity automatically calls this when a mouse clicks on a 2D Collider!
    void OnMouseDown()
    {
        // Find the LevelManager and tell it THIS specific car was clicked
        FindObjectOfType<LevelManager>().CarClicked(this);
    }

    // Called by the LevelManager to turn the car yellow when selected
    public void Highlight(bool turnOn)
    {
        if (turnOn)
        {
            spriteRenderer.color = Color.yellow;
        }
        else
        {
            spriteRenderer.color = originalColor;
        }
    }
}