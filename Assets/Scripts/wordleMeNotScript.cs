using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using KModkit;

public class wordleMeNotScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo BombInfo;
    public KMBossModule BossModule;
    public TextMesh[] textDisplays;
    public KMSelectable[] keyboardButtons;
    public Material[] cellColors;
    public Renderer[] cells;
    public AudioClip[] sounds;
    public Color[] fontColors;
    public KMSelectable ModuleSelectable;

    private char[] wordArr = {' ', ' ', ' ', ' ', ' '};
    private string[] wordList;
    private string[] submitList;

    private int numCurrentStage;
    private int numStagesLeft;
    private int numStagesOnBomb;
    private int numLettersInputted = 0;
    private string[] ignoredModules = null;
    private string[][] data;
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

    void Awake()
    {
        if (ignoredModules == null){
            ignoredModules = BossModule.GetIgnoredModules("Wordle Me Not", new string[] {
                "14",
                "42",
                "501",
                "A>N<D",
                "Bamboozling Time Keeper",
                "Black Arrows",
                "Brainf---",
                "Busy Beaver",
                "Cube Synchronization",
                "Don't Touch Anything",
                "Floor Lights",
                "Forget Any Color",
                "Forget Enigma",
                "Forget Everything",
                "Forget Infinity",
                "Forget It Not",
                "Forget Maze Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Perspective",
                "Forget The Colors",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Iconic",
                "Keypad Directionality",
                "Kugelblitz",
                "Multitask",
                "OmegaDestroyer",
                "OmegaForest",
                "Organization",
                "Password Destroyer",
                "Purgatory",
                "RPS Judging",
                "Security Council",
                "Shoddy Chess",
                "Simon Forgets",
                "Simon's Stages",
                "Souvenir",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "The Twin",
                "Übermodule",
                "Ultimate Custom Night",
                "The Very Annoying Button",
                "Whiteout",
                "Wordle Me Not",
                "X",
                "Y"
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
        numStagesOnBomb = BombInfo.GetSolvableModuleNames().Where(x => !ignoredModules.Contains(x)).Count();
        if (numStagesOnBomb < 2){
            data = wordleDictionary.GenerateListOfWordsAndColors(20, ModuleID);
            SetUpNullVictory();
        } else {
            textDisplays[5].text = "0";
            data = wordleDictionary.GenerateListOfWordsAndColors(numStagesOnBomb, ModuleID);
            submitList = new String[numStagesOnBomb];
            submitList[0] = data[0][0];
            SetDisplay(data[0][0].ToUpper());
            Debug.LogFormat("[Wordle Me Not #{0}] Initial word is '{1}'", ModuleID, data[0][0]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!moduleSolved){
            textDisplays[5].text = "" + (numCurrentStage);
            if (!readySubmitStages){
                int tempSolved = BombInfo.GetSolvedModuleNames().Where(x => !ignoredModules.Contains(x)).Count();
                if (numCurrentStage != tempSolved){
                    numCurrentStage = tempSolved;
                    if (numCurrentStage >= numStagesOnBomb){
                        Debug.LogFormat("[Wordle Me Not #{0}] Stage 1 Possible Words: {1}", ModuleID, ReturnPossiblePaths(1));
                        SetColors("11111");
                        SetDisplay("_____");
                        readySubmitStages = true;
                        numCurrentStage = 1;
                    } else {
                        Debug.LogFormat("[Wordle Me Not #{0}] Stage {1} Colors - {2} [pregenerated word: '{3}']", ModuleID, numCurrentStage, GetColorDisplays(data[1][numCurrentStage - 1]), data[0][numCurrentStage]);
                        SetColorDisplays(data[1][numCurrentStage - 1]);
                    }
                }
            } else if (focused){
                foreach (KeyValuePair<KeyCode, String> pair in keyboardInputs){
                    if (Input.GetKeyDown(pair.Key) && numLettersInputted < 5){
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
                    Debug.LogFormat("[Wordle Me Not #{0}] Striked for touching keyboard before solving all necessary modules!", ModuleID); 
                    GetComponent<KMBombModule>().HandleStrike();
                }
            } else if (name == "submit"){
                if (readyNullSolve){
                    soundAndPunch = true;
                    Debug.LogFormat("[Wordle Me Not #{0}] Submit has been pressed on a null state. Module solved.", ModuleID); 
                    SolveModule();
                } else if (readySubmitStages){
                    if (numLettersInputted == 5){
                        soundAndPunch = true;
                        SubmitWord(numCurrentStage);
                    }
                } else {
                    soundAndPunch = true;
                    Debug.LogFormat("[Wordle Me Not #{0}] Striked for touching keyboard before solving all necessary modules!", ModuleID); 
                    GetComponent<KMBombModule>().HandleStrike();
                }
            } else if (!readyNullSolve && numLettersInputted < 5){
                soundAndPunch = true;
                if (readySubmitStages){
                    SubmitLetter(action.name);
                } else {
                   Debug.LogFormat("[Wordle Me Not #{0}] Striked for touching keyboard before solving all necessary modules!", ModuleID); 
                   GetComponent<KMBombModule>().HandleStrike();
                }
            }
        }
        if (soundAndPunch) {
            GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            action.AddInteractionPunch();
        }
    }

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

    private void RemoveLetter(){
        SetColors("11111");
        numLettersInputted--;
        textDisplays[numLettersInputted].text = "_";
        wordArr[numLettersInputted] = ' ';
    }
    private void SubmitLetter(String letter){
        SetColors("11111");
        textDisplays[numLettersInputted].text = letter;
        wordArr[numLettersInputted] = char.Parse(letter);
        numLettersInputted++;
    }
    private bool SubmitWord(int currentStage){
        String result = new String(wordArr).ToLower();
        if (wordleDictionary.HasWord(result)){
            if (wordleDictionary.CalculateExactSimilarity(data[0][currentStage - 1], data[0][currentStage]) 
                == wordleDictionary.CalculateExactSimilarity(submitList[currentStage - 1], result) && !(submitList.Contains(result))){
                Debug.LogFormat("[Wordle Me Not #{0}] Submitted '{1}' on stage {2}", ModuleID, result, currentStage); 
                numCurrentStage++;
                numLettersInputted = 0;
                SetDisplay("_____");
                submitList[currentStage] = result;
                if (numCurrentStage == numStagesOnBomb){
                    SolveModule();
                } else {
                    String possiblePaths = ReturnPossiblePaths(numCurrentStage);
                    if (possiblePaths == "{}"){
                        Debug.LogFormat("[Wordle Me Not #{0}] There are no words in the word bank that would match the next stage. Resetting to stage 1...", ModuleID);
                        for(int i = 1; i <= numCurrentStage; i++){
                            submitList[i] = null;
                        }
                        numCurrentStage = 1;
                        SetColors("00000");
                        numLettersInputted = 0;
                        Debug.LogFormat("[Wordle Me Not #{0}] Stage 1 Possible Words: {2}", ModuleID, 1, ReturnPossiblePaths(1));
                        return false;
                    }
                    Debug.LogFormat("[Wordle Me Not #{0}] Stage {1} Possible Words: {2}", ModuleID, numCurrentStage, possiblePaths);
                }
                return true;
            } else {
                SetColors(data[1][currentStage - 1]);
                Debug.LogFormat("[Wordle Me Not #{0}] Struck for breaking the rules with '{1}'!", ModuleID, result);
                GetComponent<KMBombModule>().HandleStrike();
                return false;
            }
        } else {
            SetColors("00000");
            return false;
        }   
    }

    private void SetUpNullVictory(){
        textDisplays[5].text = "!";
        SetDisplay("!!!!!");
        readyNullSolve = true;
    }
    private void SetColors(String displayColors){
        for (int i = 0; i < 5; i++){
            int color = Int32.Parse(displayColors.Substring(i, 1));
            cells[i].material = cellColors[color];
        }
    }
    private void SetDisplay(String displayWord){
        numLettersInputted = 0;
        for (int i = 0; i < 5; i++){
            textDisplays[i].text = displayWord.Substring(i, 1);
        }
    }
    private void SetColorDisplays(String colors){
        SetColors(colors);
        String[] colorArr = {null, "B", "Y", "G"};
        for (int i = 0; i < 5; i++){
            int color = Int32.Parse(colors.Substring(i, 1));
            textDisplays[i].text = colorArr[color];
        }
    }
    private String GetColorDisplays(String colorNumbers){
        String result = "";
        String[] colorArr = {null, "B", "Y", "G"};
        for (int i = 0; i < 5; i++){
            int color = Int32.Parse(colorNumbers.Substring(i, 1));
            result += colorArr[color];
        }
        return result;
    }
    
    private void SolveModule(){
        moduleSolved = true;
        SetColors("33333");
        SetDisplay("DONE!");
        textDisplays[5].text = "!";
        Debug.LogFormat("[Wordle Me Not #{0}] Module solved.", ModuleID);
        GetComponent<KMBombModule>().HandlePass();
    }
}
