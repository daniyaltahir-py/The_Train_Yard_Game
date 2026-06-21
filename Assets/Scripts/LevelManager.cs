using UnityEngine;
using TMPro; // Required to interact with TextMeshPro
using System.Collections; // Required for Coroutines!
using System.Collections.Generic; // Required to use Lists
using UnityEngine.SceneManagement; // Required to restart the scene!

// NEW: We are replacing the boolean with an Enum so we can have 3+ rules!
public enum SortAlgorithm
{
    BubbleSort,
    SelectionSort,
    InsertionSort,
    Quicksort // NEW: Added Quicksort for Level 4!
}

public class LevelManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject trainCarPrefab;

    [Header("Level Settings")]
    // This is the array representing our unsorted train for Level 1
    public int[] trainNumbers = { 4, 2, 5, 1, 3 }; 
    public float spacing = 3.5f; // UPDATED: Set to 3.5f for perfect visual spacing
    public int threeStarMoves = 5; // NEW: Max moves for 3 stars
    public int twoStarMoves = 8;   // NEW: Max moves for 2 stars
    
    [Header("Algorithm Rules")]
    public SortAlgorithm currentAlgorithm = SortAlgorithm.BubbleSort; // REPLACED requiresAdjacentSwap
    public int moveCount = 0; // Tracks the player's score

    [Header("UI Elements")]
    public TextMeshProUGUI moveCountText; // To show moves on screen
    public TextMeshProUGUI winText;       // To show the win message on screen
    public GameObject nextLevelButton;    // The button to click to go to the next level

    private TrainCar currentlySelectedCar = null; // Remembers which car we clicked first
    private List<TrainCar> spawnedCars = new List<TrainCar>(); // Track the cars in a list!
    
    private bool isSwapping = false; // Safety lock to prevent clicking during animations

    void Start()
    {
        // Hide the win text and next level button at the start of the level
        if (winText != null) winText.gameObject.SetActive(false);
        if (nextLevelButton != null) nextLevelButton.SetActive(false);
        
        UpdateMoveText();
        SpawnTrain();
    }

    void SpawnTrain()
    {
        // NEW: Dynamically calculate the starting X position so the train is always centered!
        float totalWidth = (trainNumbers.Length - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < trainNumbers.Length; i++)
        {
            // Calculate position so they spawn in a horizontal line
            Vector3 spawnPos = new Vector3(startX + (i * spacing), 0, 0);

            // Instantiate (spawn) a copy of the prefab into the game world
            GameObject newCar = Instantiate(trainCarPrefab, spawnPos, Quaternion.identity);

            // Add the spawned car to our tracking list
            TrainCar carScript = newCar.GetComponent<TrainCar>();
            spawnedCars.Add(carScript);

            // Find the TextMeshPro component on the spawned car and set its number
            TextMeshPro textComponent = newCar.GetComponentInChildren<TextMeshPro>();
            if (textComponent != null)
            {
                textComponent.text = trainNumbers[i].ToString();
            }
        }
    }

    // This method is called by the TrainCar script when a player clicks on it
    public void CarClicked(TrainCar clickedCar)
    {
        // Ignore clicks if an animation is currently playing!
        if (isSwapping) return; 

        if (currentlySelectedCar == null)
        {
            // First click: Select the car
            currentlySelectedCar = clickedCar;
            currentlySelectedCar.Highlight(true);
        }
        else if (currentlySelectedCar == clickedCar)
        {
            // Clicked the exact same car again: Deselect it
            currentlySelectedCar.Highlight(false);
            currentlySelectedCar = null;
        }
        else
        {
            // Determine if the swap is valid based on our current level's rules
            int index1 = spawnedCars.IndexOf(currentlySelectedCar);
            int index2 = spawnedCars.IndexOf(clickedCar);

            bool isValidMove = true;

            // If Bubble Sort is active, enforce the adjacency rule
            if (currentAlgorithm == SortAlgorithm.BubbleSort)
            {
                if (Mathf.Abs(index1 - index2) != 1)
                {
                    isValidMove = false;
                    Debug.LogWarning("BUBBLE SORT RULE: The crane is jammed! You can only swap adjacent cars!");
                }
            }

            // NEW: If Quicksort is active, enforce the directional partition rule!
            if (currentAlgorithm == SortAlgorithm.Quicksort)
            {
                // Figure out which car is physically on the left vs right
                int leftIndex = Mathf.Min(index1, index2);
                int rightIndex = Mathf.Max(index1, index2);
                
                int leftVal = int.Parse(spawnedCars[leftIndex].GetComponentInChildren<TextMeshPro>().text);
                int rightVal = int.Parse(spawnedCars[rightIndex].GetComponentInChildren<TextMeshPro>().text);
                
                // If the car on the left is ALREADY smaller than the car on the right, 
                // swapping them would put the larger number on the left. Block it!
                if (leftVal < rightVal)
                {
                    isValidMove = false;
                    Debug.LogWarning("QUICKSORT RULE: Invalid partition! You must move smaller numbers to the left and larger numbers to the right.");
                }
            }

            if (isValidMove)
            {
                // Increment move counter
                moveCount++;
                UpdateMoveText();
                
                // Log specific messaging depending on the algorithm
                if (currentAlgorithm == SortAlgorithm.Quicksort)
                {
                    Debug.Log("Valid Partition Swap! Total Moves: " + moveCount);
                }
                else
                {
                    Debug.Log("Valid Move! Total Moves: " + moveCount);
                }
                
                if (currentAlgorithm == SortAlgorithm.InsertionSort)
                {
                    // Insertion Sort shifts the array instead of a direct 1-to-1 swap
                    StartCoroutine(InsertRoutine(currentlySelectedCar, index1, index2));
                }
                else
                {
                    // Start the smooth swap animation for Bubble, Selection, and Quicksort!
                    StartCoroutine(SwapRoutine(currentlySelectedCar, clickedCar));
                }
            }
            
            // Deselect after initiating swap (or failing)
            currentlySelectedCar.Highlight(false);
            currentlySelectedCar = null;
        }
    }

    // A Coroutine that smoothly moves the cars over time
    private IEnumerator SwapRoutine(TrainCar car1, TrainCar car2)
    {
        isSwapping = true; // Lock controls

        Vector3 startPos1 = car1.transform.position;
        Vector3 startPos2 = car2.transform.position;

        float duration = 0.4f; // How long the animation takes (0.4 seconds)
        float elapsed = 0f;

        // Loop until the duration is reached
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Apply a smooth easing effect (starts slow, speeds up, ends slow)
            float smoothStep = t * t * (3f - 2f * t);

            // Move the cars towards their new targets
            car1.transform.position = Vector3.Lerp(startPos1, startPos2, smoothStep);
            car2.transform.position = Vector3.Lerp(startPos2, startPos1, smoothStep);

            yield return null; // Wait until the next frame before continuing the loop
        }

        // Snap perfectly into place at the end just in case
        car1.transform.position = startPos2;
        car2.transform.position = startPos1;

        // Swap their order in our tracking list
        int index1 = spawnedCars.IndexOf(car1);
        int index2 = spawnedCars.IndexOf(car2);
        
        spawnedCars[index1] = car2;
        spawnedCars[index2] = car1;

        isSwapping = false; // Unlock controls

        // Check if they won!
        CheckWinCondition();
    }

    // NEW: A Coroutine that INSERTS a car and SHIFTS the rest of the train!
    private IEnumerator InsertRoutine(TrainCar carToInsert, int fromIndex, int toIndex)
    {
        isSwapping = true; // Lock controls

        // 1. Update the logical list first
        spawnedCars.RemoveAt(fromIndex);
        spawnedCars.Insert(toIndex, carToInsert);

        // 2. Track where everyone started and where they need to go
        Vector3[] startPositions = new Vector3[spawnedCars.Count];
        Vector3[] targetPositions = new Vector3[spawnedCars.Count];

        // NEW: Dynamically calculate the starting X position for the animation too!
        float totalWidth = (spawnedCars.Count - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < spawnedCars.Count; i++)
        {
            startPositions[i] = spawnedCars[i].transform.position;
            targetPositions[i] = new Vector3(startX + (i * spacing), 0, 0); // Same math as SpawnTrain
        }

        // 3. Animate all cars simultaneously!
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothStep = t * t * (3f - 2f * t);

            for (int i = 0; i < spawnedCars.Count; i++)
            {
                spawnedCars[i].transform.position = Vector3.Lerp(startPositions[i], targetPositions[i], smoothStep);
            }

            yield return null;
        }

        // 4. Snap everything perfectly into place
        for (int i = 0; i < spawnedCars.Count; i++)
        {
            spawnedCars[i].transform.position = targetPositions[i];
        }

        isSwapping = false; // Unlock controls
        CheckWinCondition();
    }

    // New helper method to update the screen text
    private void UpdateMoveText()
    {
        if (moveCountText != null)
        {
            moveCountText.text = "Moves: " + moveCount;
        }
    }

    private void CheckWinCondition()
    {
        bool isSorted = true;
        
        // Loop through our list and check if every number is smaller than the next one
        for (int i = 0; i < spawnedCars.Count - 1; i++)
        {
            int currentNum = int.Parse(spawnedCars[i].GetComponentInChildren<TextMeshPro>().text);
            int nextNum = int.Parse(spawnedCars[i+1].GetComponentInChildren<TextMeshPro>().text);
            
            if (currentNum > nextNum)
            {
                isSorted = false;
                break;
            }
        }

        if (isSorted)
        {
            // FIX: Using text/asterisks instead of Unicode symbols to avoid missing font characters!
            string stars = "* _ _"; // Default to 1 star
            if (moveCount <= threeStarMoves) stars = "* * *";
            else if (moveCount <= twoStarMoves) stars = "* * _";

            Debug.Log("🎉 YOU WIN! Total moves taken: " + moveCount + " Rating: " + stars + " 🎉");
            
            // Show the win text on the screen!
            if (winText != null)
            {
                winText.text = "YOU WIN!\nTotal Moves: " + moveCount + "\nRating: <color=yellow>" + stars + "</color>";
                winText.gameObject.SetActive(true);
            }

            // Show the Next Level button!
            if (nextLevelButton != null)
            {
                nextLevelButton.SetActive(true);
            }
        }
    }

    // This method will be triggered by our Restart Button
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // NEW: This method will be triggered by our Next Level Button
    public void LoadNextLevel()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        
        // Safety check to make sure the next level exists in the Build Settings!
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("You beat the whole game!");
            // If they beat the last level, return to the Main Menu!
            SceneManager.LoadScene("MainMenu");
        }
    }
}