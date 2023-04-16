using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using KModkit;

public class forgetleScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo BombInfo;
    public KMBossModule BossModule;
    public TextMesh[] textDisplays;
    public KMSelectable[] keyboardButtons;
    public Material[] cellColors;
    public Renderer[] cells;
    public AudioClip[] sounds;
    public KMSelectable ModuleSelectable;

    private char[] wordArr = {' ', ' ', ' ', ' ', ' '};
    private string[] wordList;
    private string[] submitList;

    private static int maxStages;

    private int numCurrentStage;
    private int numStagesLeft;
    private int numStagesOnBomb;
    private int numLettersInputted = 0;
    private int tempSolved = 0;
    private String initialPaths;
    private String possiblePaths; 
    private String[] ignoredModules = null;
    private String[] words;
    private String[] colors;
    private String[][] data;
    private StringBuilder sb = new StringBuilder();

    private static Dictionary<KeyCode, String> keyboardInputs = new Dictionary<KeyCode, String>
    {
        {KeyCode.Q, "Q"}, {KeyCode.W, "W"}, {KeyCode.E, "E"}, {KeyCode.R, "R"},
        {KeyCode.T, "T"}, {KeyCode.Y, "Y"}, {KeyCode.U, "U"}, {KeyCode.I, "I"},
        {KeyCode.O, "O"}, {KeyCode.P, "P"}, {KeyCode.A, "A"}, {KeyCode.S, "S"},
        {KeyCode.D, "D"}, {KeyCode.F, "F"}, {KeyCode.G, "G"}, {KeyCode.H, "H"},
        {KeyCode.J, "J"}, {KeyCode.K, "K"}, {KeyCode.L, "L"}, {KeyCode.Z, "Z"},
        {KeyCode.X, "X"}, {KeyCode.C, "C"}, {KeyCode.V, "V"}, {KeyCode.B, "B"},
        {KeyCode.N, "N"}, {KeyCode.M, "M"}
    };

    static int moduleIDCounter = 1;
    int ModuleID = 0;
    private bool moduleSolved = false;
    private bool readyNullSolve = false;
    private bool readySubmitStages = false;
    private bool focused = false;

    private bool displayRewound = true;

    // Boolean switch to toggle between debug mode and play mode. Only for developers' use.
    private bool debugMode = false;
    
    void Awake()
    {
        maxStages = wordleDictionary.GetLength() - 1;
        if (ignoredModules == null){
            ignoredModules = BossModule.GetIgnoredModules("Forgetle", new string[] {
                "8",
                "14",
                "+",
                "A>N<D",
                "Apple Pen",
                "Bamboozling Time Keeper",
                "Bitwise Oblivion",
                "Black Arrows",
                "Brainf---",
                "Busy Beaver",
                "Castor",
                "Concentration",
                "Cube Synchronization",
                "Damocles Lumber",
                "Don't Touch Anything",
                "Doomsday Button",
                "Duck Konundrum",
                "Floor Lights",
                "Forget Any Color",
                "Forget Enigma",
                "Forget Everything",
                "Forget Fractal",
                "Forget It Not",
                "Forget Maze Not",
                "Forget Me Later",
                "Forget Me Maybe",
                "Forget Me Not",
                "Forget Our Voices",
                "Forget Perspective",
                "Forget The Colors",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Forgetle",
                "Gemory",
                "HyperForget",
                "Iconic",
                "ID Exchange",
                "In Order",
                "Keypad Directionality",
                "Kugelblitz",
                "OmegaDestroyer",
                "OmegaForget",
                "OMISSION",
                "Organization",
                "Out of Time",
                "Password Destroyer",
                "Perspective Stacking",
                "Piano Paradox",
                "Pineapple Pen",
                "Pointer Pointer",
                "Pollux",
                "Purgatory",
                "Queen's War",
                "Red Light Green Light",
                "Remember Simple",
                "Remembern't Simple",
                "Reporting Anomalies",
                "RPS Judging",
                "Security Council",
                "Shoddy Chess",
                "Simon Forgets",
                "Simon's Stages",
                "Slight Gibberish Twist",
                "Soulscream",
                "Soulsong",
                "Souvenir",
                "Tallordered Keys",
                "Tetrahedron",
                "The Board Walk",
                "The Grand Prix",
                "The Nobody's Code",
                "The Time Keeper",
                "The Troll",
                "The Twin",
                "The Very Annoying Button",
                "Timing is Everything",
                "Top 10 Numbers",
                "Turn The Key",
                "Twister",
                "Übermodule",
                "Ultimate Custom Night",
                "Whiteout",
                "X",
                "Y",
                "Zener Cards"
            }); 
        }
        ModuleSelectable.OnInteract += delegate () { focused = true; return true; };
        ModuleSelectable.OnDefocus += delegate () { focused = false; };

        foreach (KMSelectable action in keyboardButtons)
        {
            action.OnInteract += delegate () { ActionPress(action); return false; };
        }  

        ModuleID = moduleIDCounter++;
        
    }

    void Start()
    {
        if (!debugMode){
            numStagesOnBomb = BombInfo.GetSolvableModuleNames().Where(x => !ignoredModules.Contains(x)).Count();
        } else {
            numStagesOnBomb = 6;
        }
    
        if (numStagesOnBomb < 2){
            // TestListGeneration(wordleDictionary.GetLength() - 1);
            SetUpNullVictory();
        } else {
            words = new String[numStagesOnBomb];
            colors = new String[numStagesOnBomb - 1];
            data = new String[][] {words, colors};
            wordleDictionary.GenerateSingleStage(data, 0);
            submitList = new String[numStagesOnBomb];
            submitList[0] = data[0][0];
            StartCoroutine(RevealWord(submitList[0].ToUpper(), "11111"));
            Debug.LogFormat("[Forgetle #{0}] Initial word is '{1}'", ModuleID, data[0][0]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!moduleSolved && !readyNullSolve){
            if (displayRewound){
                textDisplays[5].text = "" + (numCurrentStage);
            }
            if (!readySubmitStages){
                if (!debugMode){
                    tempSolved = BombInfo.GetSolvedModuleNames().Where(x => !ignoredModules.Contains(x)).Count();
                }                
                if (numCurrentStage != tempSolved){
                    numCurrentStage = tempSolved;
                    if (numCurrentStage >= numStagesOnBomb || numCurrentStage >= maxStages){
                        initialPaths = ReturnPossiblePaths(1);
                        possiblePaths = initialPaths;
                        Debug.LogFormat("[Forgetle #{0}] Stage 1 Possible Words: {1}", ModuleID, initialPaths);
                        StartCoroutine(RevealWord("_____", "11111"));
                        readySubmitStages = true;
                        int tempCurrentStage = numCurrentStage;
                        numCurrentStage = 1;
                        displayRewound = false;
                        StartCoroutine(RewindDisplay(tempCurrentStage));
                    } else {
                        textDisplays[5].fontSize = returnFontSize(numCurrentStage);
                        wordleDictionary.GenerateSingleStage(data, numCurrentStage);
                        Debug.LogFormat("[Forgetle #{0}] Stage {1} Colors - {2} [pregenerated word: '{3}']", ModuleID, numCurrentStage, GetColorDisplays(data[1][numCurrentStage - 1]), data[0][numCurrentStage]);
                        StartCoroutine(RevealWord(GetColorDisplays(data[1][numCurrentStage - 1]), data[1][numCurrentStage - 1]));
                    }
                }

            // Keyboard functionality
            } else if (focused){
                foreach (KeyValuePair<KeyCode, String> pair in keyboardInputs){
                    if (Input.GetKeyDown(pair.Key) && numLettersInputted < 5){
                        if (readySubmitStages)
                        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
                        SubmitLetter(pair.Value);
                    }
                }
                if (Input.GetKeyDown(KeyCode.Backspace) && numLettersInputted > 0){
                    GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
                    RemoveLetter();
                } else if (Input.GetKeyDown(KeyCode.Return) && numLettersInputted == 5){
                    GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
                    SubmitWord(numCurrentStage);
                    ModuleSelectable.AddInteractionPunch();
                }
            }
        }
    }

    void ActionPress(KMSelectable action)
    {
        bool soundAndPunch = false;
        if (!moduleSolved){
            if (readySubmitStages){
                SetColors("11111");
            }
            String name = action.name;
            if (!readyNullSolve && name == "backspace"){
                if (readySubmitStages){
                    if (numLettersInputted > 0) {
                        soundAndPunch = true;
                        RemoveLetter();
                    }
                } else {
                    soundAndPunch = true;
                    Debug.LogFormat("[Forgetle #{0}] Striked for touching keyboard before solving all necessary modules!", ModuleID); 
                    GetComponent<KMBombModule>().HandleStrike();
                }
            } else if (name == "submit"){
                if (debugMode && !readySubmitStages){
                    tempSolved++;
                } else {
                    if (readyNullSolve){
                        soundAndPunch = true;
                        Debug.LogFormat("[Forgetle #{0}] Submit has been pressed on a null state.", ModuleID); 
                        SolveModule();
                    } else if (readySubmitStages){
                        if (numLettersInputted == 5){
                            soundAndPunch = true;
                            SubmitWord(numCurrentStage);
                        }
                    } else {
                        soundAndPunch = true;
                        Debug.LogFormat("[Forgetle #{0}] Striked for touching keyboard before solving all necessary modules!", ModuleID); 
                        GetComponent<KMBombModule>().HandleStrike();
                    }
                }
            } else if (!readyNullSolve && numLettersInputted < 5){
                soundAndPunch = true;
                if (readySubmitStages){
                    SubmitLetter(action.name);
                } else {
                Debug.LogFormat("[Forgetle #{0}] Striked for touching keyboard before solving all necessary modules!", ModuleID); 
                GetComponent<KMBombModule>().HandleStrike();
                }
            }
        }
        if (soundAndPunch) {
            GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            action.AddInteractionPunch();
        }
    }

    // Returns a "list" of all possible words that follow the rules based on the previous stage
    // Compares pregenerated list entries to user entries to ensure they all have the same similarity
    private String ReturnPossiblePaths(int stageNumber){
        sb.Length = 0;
        bool moreThanOne = false;
        int length = wordleDictionary.GetLength();
        sb.Append("{");
        for (int i = 0; i < length; i++){
            String wordleWord = wordleDictionary.GetWord(i);
            if (wordleDictionary.CalculateExactSimilarity(submitList[stageNumber - 1], wordleWord) == wordleDictionary.CalculateExactSimilarity(data[0][stageNumber - 1], data[0][stageNumber])){
                if (moreThanOne){
                    sb.Append(", ");
                }
                sb.Append(wordleWord);
                moreThanOne = true;
            }
        }
        sb.Append("}");
        return sb.ToString();
    }

    // Removes letter from current word submission
    private void RemoveLetter(){
        SetColors("11111");
        numLettersInputted--;
        textDisplays[numLettersInputted].text = "_";
        wordArr[numLettersInputted] = ' ';
    }

    // Adds letter to current word submission
    private void SubmitLetter(String letter){
        SetColors("11111");
        textDisplays[numLettersInputted].text = letter;
        wordArr[numLettersInputted] = char.Parse(letter);
        numLettersInputted++;
    }

    // Submits word based on given stage
    private bool SubmitWord(int currentStage){
        String result = new String(wordArr).ToLower();
        if (wordleDictionary.HasWord(result)){
            if (possiblePaths.IndexOf(result) != -1 && !(submitList.Contains(result))){
                Debug.LogFormat("[Forgetle #{0}] Submitted '{1}' on stage {2}", ModuleID, result, currentStage); 
                numCurrentStage++;
                numLettersInputted = 0;
                StartCoroutine(RevealWord("_____", "11111"));
                submitList[currentStage] = result;
                if (numCurrentStage == numStagesOnBomb || numCurrentStage >= maxStages){
                    SolveModule();
                } else {
                    possiblePaths = ReturnPossiblePaths(numCurrentStage);
                    if (possiblePaths == "{}"){
                        Debug.LogFormat("[Forgetle #{0}] There are no words in the word bank that would match the next stage. Resetting to stage 1...", ModuleID);
                        for(int i = 1; i <= currentStage; i++){
                            submitList[i] = null;
                        }
                        possiblePaths = initialPaths;
                        numCurrentStage = 1;
                        StartCoroutine(RevealWord("_____", "00000"));
                        numLettersInputted = 0;
                        Debug.LogFormat("[Forgetle #{0}] Stage 1 Possible Words: {1}", ModuleID, initialPaths);
                        return false;
                    }
                    Debug.LogFormat("[Forgetle #{0}] Stage {1} Possible Words: {2}", ModuleID, numCurrentStage, possiblePaths);
                }
                return true;
            } else {
                SetColors(data[1][currentStage - 1]);
                Debug.LogFormat("[Forgetle #{0}] Struck for breaking the rules with '{1}'!", ModuleID, result);
                GetComponent<KMBombModule>().HandleStrike();
                return false;
            }
        } else {
            SetColors("00000");
            return false;
        }   
    }

    // Prepares module for solving if there are insufficient amount of stages
    private void SetUpNullVictory(){
        textDisplays[5].text = "!";
        StartCoroutine(RevealWord("!!!!!", "11111"));
        readyNullSolve = true;
    }

    // Instantly sets colors of displays ("11111" -> all gray)
    private void SetColors(String displayColors){
        for (int i = 0; i < 5; i++){
            int color = Int32.Parse(displayColors.Substring(i, 1));
            cells[i].material = cellColors[color];
        }
    }

    // Gets the readable version of the colors ("11111" -> "BBBBB")
    private String GetColorDisplays(String colorNumbers){
        String result = "";
        String[] colorArr = {null, "B", "Y", "G"};
        for (int i = 0; i < 5; i++){
            int color = Int32.Parse(colorNumbers.Substring(i, 1));
            result += colorArr[color];
        }
        return result;
    }
    
    // Solves the module
    private void SolveModule(){
        moduleSolved = true;
        String msg = "";
        if (numStagesOnBomb == 70){
            msg = "NICE!";
        } else if (numStagesOnBomb > 60) {
            msg = "WOAH!";
        } else if (numStagesOnBomb < 7) {
            msg = "EZPZ!";
        } else {
            msg = "DONE!";
        }
        StartCoroutine(RevealWord(msg, "33333"));
        if (UnityEngine.Random.Range(0, 30) == 15){
            Audio.PlaySoundAtTransform(sounds[0].name, transform);
        }
        textDisplays[5].text = "!";
        Debug.LogFormat("[Forgetle #{0}] Module solved.", ModuleID);
        GetComponent<KMBombModule>().HandlePass();
    }

    // Rewinds display after intial stages finish
    IEnumerator RewindDisplay(int tempCurrentStage){
        float delayTime = (float) (0.5);
        while (tempCurrentStage > 1){
            if (tempCurrentStage > 12){
                tempCurrentStage -= (int)(tempCurrentStage / 6);
            } else {
                tempCurrentStage--;
            }
            
            textDisplays[5].text = ("" + tempCurrentStage);
            textDisplays[5].fontSize = returnFontSize(tempCurrentStage);
            yield return new WaitForSeconds(delayTime / tempCurrentStage);
        }
        textDisplays[5].text = ("1");
        displayRewound = true;
        StopCoroutine(RewindDisplay(tempCurrentStage));
    }

    // Method to reveal the next word and set of colors in a text-crawl style
    IEnumerator RevealWord(String desiredWord, String desiredColors){
        for (int i = 0; i < 5; i++){
            textDisplays[i].text = (desiredWord.Substring(i, 1));
            int color = Int32.Parse(desiredColors.Substring(i, 1));
            cells[i].material = cellColors[color];
            yield return new WaitForSeconds(0.02f);
        }
        StopCoroutine(RevealWord(desiredWord, desiredColors));
    }

    // Method to calculate the font size for the stage number text mesh
    private int returnFontSize(int stageNumber){
        int res = 140;
        if (stageNumber != 0){
            res = 140 - (((int) Math.Log(stageNumber, 10)) * 20);
        }
        return res; 
    }

    // private void TestListGeneration(int testValue){
    //     words = new String[testValue];
    //     colors = new String[testValue - 1];
    //     String[][] testData = new String[][] {words, colors};
    //     wordleDictionary.GenerateSingleStage(testData, 0);
    //     Debug.LogFormat("[Forgetle #{0}] Stage 0 Word - '{1}'", ModuleID, testData[0][0]);
    //     for (int i = 1; i < testValue; i++){
    //         wordleDictionary.GenerateSingleStage(testData, i);
    //         Debug.LogFormat("[Forgetle #{0}] Stage {1} Colors - {2} [pregenerated word: '{3}']", ModuleID, i, GetColorDisplays(testData[1][i - 1]), testData[0][i]);
    //     }
    // }
}
