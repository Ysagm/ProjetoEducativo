using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class GameController : MonoBehaviour
{
    // Our different colors that we will use
    private Color colorCorrect = new Color(0.6f, 0.8509804f, 0.5490196f);
    private Color colorIncorrectPlace = new Color(1f, 0.8235294f, 0.4f);
    private Color colorUnused = new Color(0.572549f, 0.572549f, 0.572549f);
    private Color colorReward = new Color(0.596078f, 0.901960f, 0.992156f);

    // List of starting x positions for the wordboxes
    private float[] wordRowStartIngXPositions = new float[5];

    // Reference to grid layout group
    public GridLayoutGroup gridLayoutGroup;

    // The sprite that will be used when a box "cleared"
    public Sprite clearedWordBoxSprite;

    // Reference to the player controller script
    public PlayerController playerController;

    //Control popup
    public GameObject popup;

    private Coroutine popupRoutine;

    public Button popupButton;

    // Curve for animating the wordboxes
    public AnimationCurve wordBoxInteractionCurve;

    // Current wordbox that we're inputting in
    private int currentWordBox;

    // The current row that we're currently at
    private int currentRow;

    // Amount of rows of wordboxes
    private int amountOfRows = 5;

    // How many characters are there per row
    private int charactersPerRowCount = 5;

    public string correctWord;

    // All wordboxes
    public List<Transform> wordBoxes = new List<Transform>();

    // List with all the words
    private List<string> dictionary = new List<string>();

    // List with words that can be chosen as correct words
    private List<string> guessingWords = new List<string>();
    private int savedVariable;
    //Armazenar palavras corretas
    public char[] correctLetters = new char[5];

    // Start is called before the first frame update
    void Start()
    {
        AddsManager.Instance.ShowInterstitial();
         // get the value stored in PlayerPrefs and assign it to the savedVariable variable
        if (PlayerPrefs.HasKey("dropdownID"))
        {
            savedVariable = PlayerPrefs.GetInt("dropdownID");
        }
        else
        {
            // if the key "dropdownID" doesn't exist in PlayerPrefs, set the default value to 0
            savedVariable = 0;
        }

        // populate the dictionaries based on the selected language
        if (savedVariable == 0)
        {
            // Populate the dictionary
            AddWordsToList("dictionaryPT", dictionary);

            // Populate the guessing words
            AddWordsToList("wordlist", guessingWords);
        }
        else if (savedVariable == 1)
        {
            AddWordsToList("dictionary", dictionary);

            // Populate the guessing words
            AddWordsToList("wordlistEn", guessingWords);
        }

        // Choose a random correct word
        correctWord = GetRandomWord();

        // Save the x-positions of the wordboxes to a list, so we can reuse it for resetting later
        SetRowStartPositions();
    }
    public void ShowRewarded()
    {
        AddsManager.Instance.ShowRewarded();
    }

    void SetRowStartPositions()
    {
        // Force the grid layout group to set it's positions
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridLayoutGroup.GetComponent<RectTransform>());

        // Save all x positions to a list
        for (int i = 0; i < 5; i++)
        {
            wordRowStartIngXPositions[i] = (wordBoxes[i].localPosition.x);
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Restarted scene");

    }

    void AddWordsToList(string path, List<string> listOfWords)
    {
        //só pode carregar arquivos .txt, não pode colocar o .txt no nome para o Resources.Load
        TextAsset mytxtData = (TextAsset)Resources.Load(path);
        string text = mytxtData.text;

/*
        // Read the text from the file
        StreamReader reader = new StreamReader(path);
        string text = reader.ReadToEnd();
*/
        // Output the text to the console
        //Debug.Log(text);

        // Separate them for each ',' character
        char[] separator = { ',' };
        string[] singleWords = text.Split(separator);

        // Add everyone of them to the list provided as a variable
        foreach (string newWord in singleWords)
        {
            listOfWords.Add(newWord);
        }

        // Close the reader
       // reader.Close();
    }

    string GetRandomWord()
    {
        string randomWord = guessingWords[Random.Range(0, guessingWords.Count)];
        Debug.Log(randomWord);
        return randomWord;
    }

    public void AddLetterToWordBox(string letter)
    {
        if (currentRow > amountOfRows)
        {
            Debug.Log("No more rows available");
            return;
        }

        int currentlySelectedWordbox = (currentRow * charactersPerRowCount) + currentWordBox;

        if (wordBoxes[(currentRow * charactersPerRowCount) + currentWordBox].GetChild(0).GetComponent<Text>().text == "")
        {
            wordBoxes[(currentRow * charactersPerRowCount) + currentWordBox].GetChild(0).GetComponent<Text>().text = letter;
            AnimateWordBox(wordBoxes[currentlySelectedWordbox]);
        }

        if (currentlySelectedWordbox < (currentRow * charactersPerRowCount) + 4)
        {
            currentWordBox++;
        }
    }

    public void RemoveLetterFromWordBox()
    {
        if (currentRow > amountOfRows)
        {
            Debug.Log("No more rows available");
            return;
        }

        int currentlySelectedWordbox = (currentRow * charactersPerRowCount) + currentWordBox;

        // If the text in the current wordbox is empty, go back a step and clear the one that comes after
        if (wordBoxes[currentlySelectedWordbox].GetChild(0).GetComponent<Text>().text == "")
        {
            if (currentlySelectedWordbox > ((currentRow * charactersPerRowCount)))
            {
                // Step back
                currentWordBox--;
            }
            // Update the variable
            currentlySelectedWordbox = (currentRow * charactersPerRowCount) + currentWordBox;

            wordBoxes[currentlySelectedWordbox].GetChild(0).GetComponent<Text>().text = "";
        }

        else
        {
            // If it wasn't empty, we clear the one selected instead
            wordBoxes[currentlySelectedWordbox].GetChild(0).GetComponent<Text>().text = "";
        }
        AnimateWordBox(wordBoxes[currentlySelectedWordbox]);
    }

    public void SubmitWord()
    {
        // The players guess
        string guess = "";

        for (int i = (currentRow * charactersPerRowCount); i < (currentRow * charactersPerRowCount) + currentWordBox + 1; i++)
        {
            // Add each letter to the players guess
            guess += wordBoxes[i].GetChild(0).GetComponent<Text>().text;
        }

        // Return if the answer did not contain exactly 5 letters
        if (guess.Length != 5)
        {
            Debug.Log("Answer too short, must be 5 letters");

            popupButton.gameObject.SetActive(false);
            // Let the player know that the submitted word is too short
            ShowPopup("Answer too short, must be 5 letters", 2f, false);

            // Wiggle word row
            StartCoroutine(AnimateWordRow());
            return;
        }

        // All words are in lowercase, so let's convert the guess to that as well
        guess = guess.ToLower();

        // Check if the word exists in the dictionary 
        bool wordExists = false;
        foreach (var word in dictionary)
        {
            if (guess == word)
            {
                wordExists = true;
                break;
            }
        }
        // If it didn't exist in the dictionary, does it exist in the other list
        if (wordExists == false)
        {
            foreach (var word in guessingWords)
            {
                if (guess == word)
                {
                    wordExists = true;
                    break;
                }
            }
        }
        if (wordExists == false)
        {
            // Let the player know that the submitted word is too short
            ShowPopup("Word does not exist in dictionary!", 2f, false);

            popupButton.gameObject.SetActive(false);
            // Wiggle word row
            StartCoroutine(AnimateWordRow());
            return;
        }

        // Output the guess to the console
        Debug.Log("Player guess:" + guess);

        StartCoroutine(CheckWord(guess));

    }

    IEnumerator CheckWord(string guess)
    {
        char[] playerGuessArray = guess.ToCharArray();
        string tempPlayerGuess = guess;
        char[] correctWordArray = correctWord.ToCharArray();
        string tempCorrectWord = correctWord;

        // Swap correct characters with '0'
        for (int i = 0; i < 5; i++)
        {
            if (playerGuessArray[i] == correctWordArray[i])
            {
                // Correct place
                armazenarAcertos(playerGuessArray[i]);
                playerGuessArray[i] = '0';
                correctWordArray[i] = '0';
            }
        }

        // Update the information
        tempPlayerGuess = "";
        tempCorrectWord = "";
        for (int i = 0; i < 5; i++)
        {
            tempPlayerGuess += playerGuessArray[i];
            tempCorrectWord += correctWordArray[i];
        }

        // Check for characters in wrong place, but correct letter
        for (int i = 0; i < 5; i++)
        {
            if (tempCorrectWord.Contains(playerGuessArray[i].ToString()) && playerGuessArray[i] != '0')
            {
                char playerCharacter = playerGuessArray[i];
                armazenarAcertos(playerGuessArray[i]);
                playerGuessArray[i] = '1';
                tempPlayerGuess = "";
                for (int j = 0; j < 5; j++)
                {
                    tempPlayerGuess += playerGuessArray[j];
                }

                // We need to update the correct word string
                // This is so that we only check for the correct amount of characters.
                int index = tempCorrectWord.IndexOf(playerCharacter, 0);
                correctWordArray[index] = '.';
                tempCorrectWord = "";
                for (int j = 0; j < 5; j++)
                {
                    tempCorrectWord += correctWordArray[j];
                }
            }
        }

        // Set the fallback color to gray
        Color newColor = colorUnused;

        // Go through the players answer and color each button and wordbox accordingly
        for (int i = 0; i < 5; i++)
        {

            if (tempPlayerGuess[i] == '0')
            {
                // Correct placement
                newColor = colorCorrect;
            }
            else if (tempPlayerGuess[i] == '1')
            {
                // Correct character, wrong placement
                newColor = colorIncorrectPlace;
            }
            else
            {
                // Character not used
                newColor = colorUnused;
            }

            // Reference variable
            Image currentWordboxImage = wordBoxes[i + (currentRow * charactersPerRowCount)].GetComponent<Image>();

            // Our timer
            float timer = 0f;

            // Duration of the animation
            float duration = 0.15f;


            // Loop for the duration
            while (timer <= duration)
            {
                // Value will go from 0 to 1
                float value = timer / duration;

                // Interpolate linearly from a scale of (1, 1, 1) to a scale of (0, 0, 0)
                currentWordboxImage.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, value);

                // Increase timer
                timer += Time.deltaTime;
                yield return null;
            }

            // Set the scale again if we overshoot
            currentWordboxImage.transform.localScale = Vector3.zero;

            // Change the sprite
            currentWordboxImage.sprite = clearedWordBoxSprite;

            // Set the color of the wordbox to the "new color"
            currentWordboxImage.color = newColor;

            // Reset the timer
            timer = 0f;

            // Same loop as before, but in reverse from from a scale of (0, 0, 0) to a scale of (1, 1, 1)
            while (timer <= duration)
            {
                // Value will go from 0 to 1
                float value = timer / duration;

                // Interpolate linearly from a scale of (0, 0, 0) to a scale of (1, 1, 1)
                currentWordboxImage.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, value);

                // Increase timer
                timer += Time.deltaTime;
                yield return null;
            }

            // Set the scale again if we overshoot
            currentWordboxImage.transform.localScale = Vector3.one;

            // Set the color of the keyboard character to the "new color", only if it's "better" than the previous one
            // Saving a variable for the current keyboard image
            Image keyboardImage = playerController.GetKeyboardImage(guess[i].ToString());

            // Always possible to set the correct placement color
            if (newColor == colorCorrect)
            {
                keyboardImage.color = newColor;
            }

            // Only set the colorIncorrectPlace if it's not the colorCorrect
            if (newColor == colorIncorrectPlace && keyboardImage.color != colorCorrect)
            {
                keyboardImage.color = newColor;
            }

            // Only set the unused color if it's not colorIncorrectPlace and colorCorrect
            if (newColor == colorUnused && keyboardImage.color != colorCorrect && keyboardImage.color != colorIncorrectPlace)
            {
                keyboardImage.color = newColor;
            }

        }

        // If the guess was correct, output that the player has won into the console
        if (guess == correctWord)
        {
            // Let the player know that they won!
            // And show what score they got
            // This popup stays forever as well
            ShowPopup("You win!", 0f, true);
            popupButton.gameObject.SetActive(true);

        }
        else
        {
            // If the guess was incorrect, go to the next row
            Debug.Log("Wrong, guess again!");
            // Restart at the leftmost character
            currentWordBox = 0;
            currentRow++;
        }

        if (currentRow > amountOfRows)
        {
            Debug.Log("No more rows available");
            // Let the player know that they lost, and what the correct word was
            // this popup does not disappear
            ShowPopup("You Lost!\n" + "Correct word was:" + correctWord, 0f, true);
            popupButton.gameObject.SetActive(true);
        }
    }
    public void armazenarAcertos(char letter)
    {
        bool adicionou = false;
        for (int i = 0; i < correctLetters.Length; i++)
        {
            if (correctLetters[i] == 0 && !adicionou)
            {
                correctLetters[i] = letter;
                adicionou = true;
            }
        }
    }
    public void rewardLetter()
    {
        bool tem = false;
        bool pintou = false;
        for (int u = 0; u < correctLetters.Length; u++)
        {
            Debug.Log(correctLetters[u]);
        }
        foreach (char i in correctWord)
        {
            tem = false;
            for (int j = 0; j < correctLetters.Length; j++)
            {
                if (i == correctLetters[j])
                {
                    tem = true;
                }
            }
            if (!tem && !pintou)
            {
                Image keyboardImage = playerController.GetKeyboardImage(i.ToString());
                keyboardImage.color = colorReward;
                pintou = true;
            }
        }
    }
    void ShowPopup(string message, float duration, bool stayForever)
    {
        // If a popup routine exists, we should stop that first,
        // this makes sure that not 2 coroutines can run at the same time.
        // Since we are using the same popup for every message, we only want one of these coroutines to run at any time
        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
        }
        popupRoutine = StartCoroutine(ShowPopupRoutine(message, duration, stayForever));
    }

    IEnumerator ShowPopupRoutine(string message, float duration, bool stayForever = false)
    {
        // Set the message of the popup
        popup.transform.GetChild(0).GetComponent<Text>().text = message;

        // Activate the popup
        yield return PopupVisualRoutine(false);
        // If it should stay forever or not
        if (stayForever)
        {
            while (true)
            {
                yield return null;
            }
        }
        // Wait for the duration time
        yield return new WaitForSeconds(duration);

        // Deactivate the popup
        yield return PopupVisualRoutine(true);
    }

    void AnimateWordBox(Transform wordboxToAnimate)
    {
        StartCoroutine(AnimateWordboxRoutine(wordboxToAnimate));
    }

    IEnumerator AnimateWordboxRoutine(Transform wordboxToAnimate)
    {
        // Our timer
        float timer = 0f;

        // Duration of the animation
        float duration = 0.15f;

        //Set up startscale and end-scale of the wordbox
        Vector3 startScale = Vector3.one;

        // End-scale is just a little bit bigger than the original scale
        Vector3 scaledUp = Vector3.one * 1.2f;

        // Set the wordbox-scale to the starting scale, in case we're entering in the middle of another transition
        wordboxToAnimate.localScale = Vector3.one;

        // Loop for the time of the duration
        while (timer <= duration)
        {
            // This will go from 0 to 1 during the time of the duration
            float value = timer / duration;

            // LerpUnclamped will return a value above 1 and below 0, regular Lerp will clamp the value at 1 and 0
            // To have more freedom when animating, LerpUnclamped can be used instead
            wordboxToAnimate.localScale = Vector3.LerpUnclamped(startScale, scaledUp, wordBoxInteractionCurve.Evaluate(value));

            // Increase the timer by the delta time
            timer += Time.deltaTime;
            yield return null;
        }

        // Since we're checking if the timer is smaller and/or equals to the duration in the loop above,
        // the value might go above 1 which would give the wordbox a scale that is not equals to the desired scale.
        // To prevent slightly scaled wordboxes, we set the scale of the wordbox to the startscale
        wordboxToAnimate.localScale = startScale;
    }

    IEnumerator AnimateWordRow()
    {
        // Our timer
        float timer = 0f;

        // Duration of the animation
        float duration = 0.5f;

        // Save the starting positions of the current wordRow boxes to a list
        // Here we will use the saved x-positions that we collected on start
        List<Vector3> wordRowStartIngPositions = new List<Vector3>();
        for (int i = 0; i < 5; i++)
        {
            // Reference to wordBox
            Vector3 currentWordBoxLocalPosition = wordBoxes[i + (currentRow * 5)].localPosition;
            // Every wordbox in the same column share the same X value
            // So all 5 wordboxes per row will have the same x-starting values as the 5 word boxes above
            wordRowStartIngPositions.Add(new Vector3(wordRowStartIngXPositions[i], currentWordBoxLocalPosition.y, currentWordBoxLocalPosition.z));
        }

        // Now we will reset the wordboxes to the saves starting positions
        // This is to handle this coroutine being triggered on top if itself
        for (int i = 0; i < 5; i++)
        {
            Transform currentWordBox = wordBoxes[i + (currentRow * 5)];
            // Reset the position
            currentWordBox.localPosition = wordRowStartIngPositions[i];
        }

        // Set up our variables
        float wiggleX = 0f;

        // How fast should the wiggle should be
        float wiggleSpeed = 40f;

        // How far the wiggle should be from the center
        float wiggleRadius = 8f;

        // Run loop
        while (timer <= duration)
        {
            // By using Cos we will get a value that goes from -1 to 1, we multiply this by wiggleSpeed to "wiggle" the box.
            // We then let this value go towards 0 at the end of the duration.
            // This will wiggle the object and the amplitude of the wiggle will decrease until it hits 0 at the end of the duration
            wiggleX = Mathf.Lerp(Mathf.Cos(timer * wiggleSpeed) * wiggleRadius, 0f, timer / duration);

            // Set the position of the word boxes to the startingPosition that they had + the wiggleAmount
            for (int i = 0; i < 5; i++)
            {
                wordBoxes[i + (currentRow * 5)].localPosition = wordRowStartIngPositions[i] + new Vector3(wiggleX, 0, 0);
            }

            // Increase the timer by the delta time
            timer += Time.deltaTime;

            yield return null;
        }

        // Reset the positions to the starting positions
        for (int i = 0; i < 5; i++)
        {
            wordBoxes[i + (currentRow * 5)].localPosition = wordRowStartIngPositions[i];
        }

    }

    IEnumerator PopupVisualRoutine(bool hide)
    {
        // Our timer
        float timer = 0f;

        // Duration of the animation
        float duration = 0.2f;

        // Reference to the popups transform
        Transform popupTransform = popup.transform;

        // Set the start scale
        Vector3 startScale = Vector3.zero;

        // Set the end scale
        Vector3 endScale = Vector3.one;

        // If we're hiding the popup, we will swap the startscale and endscale variables
        if (hide)
        {
            (startScale, endScale) = (endScale, startScale);
        }
        // Set the popups scale to 0
        popupTransform.localScale = startScale;

        // Turn on the popup gameobject
        popup.SetActive(true);

        // First we lerp the scale from 0 to 1
        while (timer <= duration)
        {
            // This is just to make the variable value a bit cleaner
            float t = timer / duration;

            // This will create a smoothstep curve
            // It will ease in, and then ease out
            float value = t * t * (3f - (2f * t));

            // Lerp the scale from (0,0,0) to (1,1,1)
            popupTransform.localScale = Vector3.Lerp(startScale, endScale, value);

            // Increase the timer by the delta time
            timer += Time.deltaTime;

            yield return null;
        }
        // Set the scale to (1, 1, 1) in case we overshoot
        popupTransform.localScale = endScale;

        if (hide)
        {
            popup.SetActive(false);
        }

    }
}