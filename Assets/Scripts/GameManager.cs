using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    //Singleton
    public static GameManager gameManager;

    //Lista de codigos de teclas detectables
    private readonly KeyCode[] keyCodes = Enum.GetValues(typeof(KeyCode)) 
        .Cast<KeyCode>().Where(k => ((int)k < (int)KeyCode.Mouse0)).ToArray();


    // MUROS INVISIBLES:
    public RectTransform canvasUI;
    public GameObject wallPrefab;
    public float wallHeight = 5;
    public float wallThickness = 2;
    public float wallExtraLength = 1;

    
    // TECLA:

    //Objeto de la tecla
    private GameObject keyGO;
    //Controller de la tecla
    private KeyController keyContr;
    //¿La tecla esta pulsada?
    private bool keyPressed = false;
    private bool keyDead = false;
    private string specialKeyLabel = "";
    private string specialKeyOutput = "";

    
    // ELECCION DE TECLA:

    public TMP_InputField keyInputText;
    public KeyCode chosenKey;


    // CREACION DE LETRAS EN PANTALLA:

    //Prefab de letra que rebota
    public GameObject letterPrefab;
    private LetterController letterPrefabController;
    //Posicion inicial de aparición de las letras
    private Vector3 spawnPoint;
    //Offset de distancia entre las letras que aparecen en la misma pulsacion de la tecla
    public float spawnDistOffset = 0.5f;
    //Tiempo offset de aparicion entre letras al manter presionado el boton
    public float spawnTimeOffset = 0.2f;
    //Fuerza de impulso inicial de las letras
    public float letterReleaseForce = 50;
    //Escalar la altura respecto al suelo de las letras (en la direccion del rayo desde la camara)
    public float letterHeightFactor = 3f;
    //Direccion en la que hay que hacer el offset cuando se escriben varias letras en la pulsacion
    private Vector3 offsetDirection;
    //Elemento de la UI de spawn point de letras
    public RectTransform screenTextInput;
    //Radio del collider de las letras
    private float letterColliderRadius;
    //Lista de letras que se estan escribiendo antes de ser lanzadas
    private List<LetterController> rowLetters;
    //Capas con la que colisionarán los rayos trazados desde la camara (capa 9 "seeThrough" del suelo en este caso)
    private int rayLayerMask = 1 << 9;


    // SOBRECARGA:

    //Número máximo de letras que se escriben en el input de la pantalla
    public int maxInputLetters = 12;
    //Letras que dura como máximo la sobrecarga
    public int overloadLetters = 10;
    private int currentOverload = 0;
    public float overloadShakeMag = 0.1f;
    public Image inputBackground;
    public Material normalMat, chargedMat;
    public Renderer keyRenderer;


    // ONDA EXPANSIVA:
    public GameObject shockWaveParts;
    public int waveCoolDown = 20;
    public int currentCoolDownLetters = 20;
    private bool waveCharged = false;
    public bool waveAvailable = true;
    public Color ledOn, ledOff;
    public GameObject ledPrefab, waveLedPrefab;
    public Image waveIndicator;
    public Transform ledParent;
    private Image[] chargeLeds;


    // CURSOR:
    public Transform cursor;
    private Animator cursorAnimator;
    private Vector3 initCursorPos;


    // TRANSICION
    public Animator transitionAnimator;
    private bool waitingDialog = false;
    private bool lastDialog = false;


    // PUNTUACION Y DERROTA
    private int letterCount = 0;
    public TextMeshProUGUI letterCountText;
    public GameObject lifeCountUI, letterCountUI;
    public GameObject keyExplosionParts;


    // TUTORIAL

    public bool tutorialActived = false;
    [HideInInspector] public  bool overloadWarning = false, healthWarning = false;
    public bool needHoldKey = false;
    public bool needReleaseKey = false;
    public float holdTime = 1f;
    private float currentHoldTime = 0;
    public TutorialStep tutorialStep = TutorialStep.WAIT_START;
    public enum TutorialStep {
        WAIT_START = 0,
        OBJECTIVE = 1,
        WAIT_BOMB = 4,
        BOMB = 5,
        WAIT_COOLDOWN = 6,
        COOLDOWN = 7,
        FINISH = 8
    };

    //ESTADOS DEL JUEGO
    public bool inGame = false;
    public bool detectingKey = true;


    void Awake()
    {
        //Patron singleton
        if(gameManager == null)
        {
            gameManager = this;
            //dontdestroyonload
        }
        else
        {
            Destroy(this);
        }

        //Asignar variable estática de impulso de salida de cada letra (ofrecer interfaz en el editor a esta variable)
        LetterController.releaseForceMag = letterReleaseForce;
    }

    void Start()
    {
        //Recibimos avisos de que la accion de que se han acabado los dialogos
        DialogManager.OnLastPulsation += DialogFinished;

        letterCount = 0;

        keyGO = GameObject.FindGameObjectWithTag("Key");
        keyContr = keyGO.GetComponent<KeyController>();
        specialKeyLabel = "";

        letterPrefabController = letterPrefab.GetComponent<LetterController>();
        letterColliderRadius = letterPrefab.GetComponent<SphereCollider>().radius;

        //Trazamos un rayo desde la posicion del elemento de input de la UI hacia el floor utilizando una máscara para nuestro rayo
        Ray letterRay = RectTransformUtility.ScreenPointToRay(Camera.main, GetScreenCoordinates(screenTextInput).center);
        RaycastHit hit;
        Physics.Raycast(letterRay, out hit, Mathf.Infinity, rayLayerMask);
        spawnPoint = hit.point - (letterRay.direction.normalized * letterColliderRadius*letterHeightFactor);

        //Calculamos el vector en espacio mundo para desplazar las nuevas letras creadas en una pulsacion
        offsetDirection = (Quaternion.AngleAxis(Camera.main.transform.rotation.eulerAngles.y, Vector3.up) * Vector3.right).normalized;

        //Creamos los muros invisibles en los limites de la pantalla
        SetWalls();

        //Colocamos y preparamos el cursor
        initCursorPos = spawnPoint - offsetDirection * spawnDistOffset/2f;
        cursor.position = initCursorPos;
        cursorAnimator = cursor.gameObject.GetComponentInChildren<Animator>();

        //Mostramos los dialogos de introduccion
        waitingDialog = true;
        inGame = false;
        EventSystem.current.SetSelectedGameObject(keyInputText.gameObject);
        StartCoroutine(ShowIntroduction());

        //TODO: QUITAR
        PlayerPrefs.SetInt("tutorialActived", 1);
        //Detectamos si el tutorial esta activo o no
        if(PlayerPrefs.HasKey("tutorialActived"))
        {
            tutorialActived = PlayerPrefs.GetInt("tutorialActived") == 1;
        }
        else
        {
            PlayerPrefs.SetInt("tutorialActived", 1);
            tutorialActived = true;
        }
            
        if(tutorialActived)
        {
            overloadWarning = true;
            healthWarning = true;
            PlayerPrefs.SetInt("overloadWarning", 1);
            PlayerPrefs.SetInt("healthWarning", 1);
        }
        else
        {
            overloadWarning = false;
            healthWarning = false;
            PlayerPrefs.SetInt("overloadWarning", 0);
            PlayerPrefs.SetInt("healthWarning", 0);
        }
    }

    void Update()
    {
        //Juego comenzado
        if(inGame)
        {
            //Salir con escape
            //TODO: Mantener un rato para salir
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }

            //Dialogos del tutorial
            if(waitingDialog && tutorialActived)
            {
                UpdateTutorial();  
            }
            //Actualizar juego
            else
            {
                UpdateInGame();
            }
        }
        //Dialogos previos al juego
        else if (waitingDialog)
        {
            UpdateDialogs();
        }
    }

    void UpdateDialogs()
    {
        if(Input.anyKeyDown)
        {
            foreach (KeyCode keyCode in keyCodes)
            {
                if (Input.GetKey(keyCode)) 
                {
                    //Si no es el ultimo diálogo continuamos y reseteamos el input invisible
                    if(!lastDialog)
                    {
                        keyInputText.text = "";
                        EventSystem.current.SetSelectedGameObject(keyInputText.gameObject);
                    }
                    //Ultimo dialogo...
                    else
                    {
                        lastDialog = false;

                        //Si se esta eligiendo tecla, se comprueba su validez
                        if(detectingKey)
                        {
                            chosenKey = keyCode;
                            //Si la tecla es valida dejamos de detectar
                            if(IsKeyAvailable(chosenKey))
                            {
                                detectingKey = false;
                                //Quitamos el foco del input para evitar que se siga escribiendo
                                EventSystem.current.SetSelectedGameObject(null);
                            }
                            //Si la tecla no es válida volvemos a pedir que se introduzca una letra
                            else
                                AskPressKey();
                        }
                        //Si no se esta eligiendo, se empieza a jugar
                        else
                            StartGame();
                    }

                    DialogManager.dialogManager.Continue();
                    break;
                }
            }
        }
    }

    //Continuar dialogos en el tutorial
    void UpdateTutorial()
    {
        //Pausar tiempo
        Time.timeScale = 0f;

        //Si se nos pide mantener para pasar el tutorial
        if(needHoldKey && lastDialog)
        {
            if(Input.GetKey(chosenKey))
            {
                currentHoldTime += Time.fixedDeltaTime;
                if(currentHoldTime >= holdTime)
                {
                    DialogManager.dialogManager.Continue();
                    currentHoldTime = 0;
                    needHoldKey = false;
                    waitingDialog = false;
                    lastDialog = false;
                }
            }
            else
            {
                currentHoldTime = 0;
            }
        }
        //Si se nos pide soltar para pasar el tutorial
        else if (needReleaseKey && lastDialog)
        {
            if(Input.GetKeyUp(chosenKey))
            {
                needReleaseKey = false;
                DialogManager.dialogManager.Continue();
                waitingDialog = false;
                lastDialog = false;
            }
        }
        //Si se nos pide simplemente pulsar para pasar al siguiente dialogo
        else
        {
            if(Input.GetKeyDown(chosenKey))
            {
                if(lastDialog)
                {
                    waitingDialog = false;
                    lastDialog = false;
                }
                DialogManager.dialogManager.Continue();
            }
        }
    }

    void UpdateInGame()
    {
        //Reanudar
        Time.timeScale = 1f;

        //Pulsar tecla
        if(Input.GetKeyDown(chosenKey))
        {
            keyPressed = true;
            keyContr.PressKey();
            StartWritingLetters();
        }
        
        //Soltar tecla
        if(Input.GetKeyUp(chosenKey))
        {
            //Al escribir varias letras en el tutorial indicamos el objetivo
            if(PlayerPrefs.GetInt("tutorialActived") == 1 && tutorialStep == TutorialStep.WAIT_START)
            {
                tutorialStep = TutorialStep.OBJECTIVE;
            }

            ReleaseKey();
        }    
    }

    void ReleaseKey()
    {
        keyPressed = false;
        keyContr.ReleaseKey();

        //Si la onda expansiva estaba disponible y se ha cargado, se lanza
        if(waveCharged)
        {
            Instantiate(shockWaveParts);
            AudioManager.am.Play("shockWave");

            if(GameManager.gameManager.tutorialStep == GameManager.TutorialStep.WAIT_COOLDOWN)
                GameManager.gameManager.tutorialStep = GameManager.TutorialStep.COOLDOWN;

            foreach(Image led in chargeLeds)
            {
                led.color = ledOff;
            }
            waveIndicator.color = ledOff;
            currentCoolDownLetters = 0;
            waveCharged = false;
            waveAvailable = false;
        }

        ReleaseLetters();
    }

    IEnumerator ShowIntroduction()
    {
        yield return new WaitForSeconds(1f);
        //Mostrar dialogos iniciales de la tecla escape
        EnqueueDialog("Hi! I am your escape key.");
        EnqueueDialog("Yes, yes, I am down here, on the keyboard. Helloooo! What's up?");
        EnqueueDialog("Don't look at me like that. You see me every day.");
        EnqueueDialog("As the Representative of all the Keys, I am here to tell you that you are an exploiter.");
        EnqueueDialog("Despite the great variety of keys available on your keyboard, game freaks like you always end up abusing the poor W A S D and leaving the rest forgotten.");
        EnqueueDialog("THAT'S ENOUGH, BULLY.");
        EnqueueDialog("I DON'T FIND IT FUNNY AT ALL.");
        EnqueueDialog("From now, you will have to make do with JUST ONE KEY. It serves you right for being so SMARTASS.");
        EnqueueDialog("And for your sake, you'd better switch keys between games.");

        //Finalmente pedimos la tecla
        AskPressKey();
    }

    void AskPressKey()
    {
        detectingKey = true;
        waitingDialog = false;
        lastDialog = false;
        inGame = false;
        keyDead = false;
        keyInputText.text = "";
        specialKeyLabel = "";
        specialKeyOutput = "";
        letterCount = 0;
        specialKeyLabel = "";
        currentOverload = 0;

        waveAvailable = true;
        waveCharged = false;

        //Mostrar dialogo
        EnqueueDialog("So, which key are you this time?");

        EventSystem.current.SetSelectedGameObject(keyInputText.gameObject);
    }

    public void EnqueueDialog(string dialog)
    {
        DialogManager.dialogManager.EnqueueDialog(dialog);
        waitingDialog = true;
    }

    public void DialogFinished()
    {
        lastDialog = true;
    }

    //Empezar juego
    void StartGame()
    {
        inGame = true;
        waitingDialog = false;

        //Animacion transicion mostrando input de letras con cursor
        transitionAnimator.enabled = true;
        transitionAnimator.Play("StartTransition");

        //Poner letra sobre la tecla y aparecer
        //TODO: Casos especiales bloqs, tab, espacio, etc.
        if(specialKeyLabel == "")
        {
            keyContr.StartAsNormalkey(keyInputText.text);
            letterPrefabController.SetLetter(keyInputText.text);
        }
        else
        {
            keyContr.StartAsNormalkey(specialKeyLabel);
            letterPrefabController.SetLetter(specialKeyOutput);
        }

        lifeCountUI.SetActive(true);
        letterCountUI.SetActive(true);
        currentCoolDownLetters = waveCoolDown;
        PrepareLeds();
        ledParent.gameObject.SetActive(true);
        waveIndicator.gameObject.SetActive(true);
        
        AudioManager.am.Play("background");

        //Comenzar tutorial
        if(PlayerPrefs.GetInt("tutorialActived") == 1)
        {
            StartCoroutine(TutorialCoroutine());
        }
    }

    IEnumerator TutorialCoroutine()
    {
        yield return new WaitForSeconds(1f);
        tutorialActived = false;
        DialogManager.dialogManager.SetDialogType(true);

        //Esperamos un tiempo minimo y a que se escriban unos caracteres
        tutorialStep = TutorialStep.WAIT_START;
        while(tutorialStep != TutorialStep.OBJECTIVE)
            yield return null;
        yield return new WaitForSecondsRealtime(0.5f);
        
        //Indicamos el objetivo del juego
        tutorialActived = true;
        //TODO: Señalar contador de caracteres
        DialogManager.dialogManager.SetCurrentShadow((int)TutorialStep.OBJECTIVE);
        EnqueueDialog("WOW, INCREDIBLE! When you press the key, characters appear! Who would have imagined it?");
        EnqueueDialog("Your GOAL is to write as many characters as possible to make our friend “Character Counter” happy :) .");

        //Esperamos un tiempo o se queda con dos vidas o mantiene hasta cargar la bomba
        tutorialStep = TutorialStep.WAIT_BOMB;
        float time = 0;
        float maxTime = 5;
        while(tutorialStep != TutorialStep.BOMB && time < maxTime && !waveCharged)
        {
            time += Time.deltaTime;
            yield return null;
        }

        //Explicamos funcionamiento de la bomba
        tutorialActived = true;
        //Señalar barra de letras
        DialogManager.dialogManager.SetCurrentShadow((int)TutorialStep.BOMB);
        EnqueueDialog("Aren't you curious about that SUBTLE BOMB sign? Why don't you try HOLDING DOWN the key until the character bar fills up?");
        EnqueueDialog("Maybe when you RELEASE the key something goes BOOOOM, a few characters are deleted and you can stay alive a little longer.");

        //Esperamos un tiempo tras tirar una bomba
        tutorialStep = TutorialStep.WAIT_COOLDOWN;
        while(tutorialStep != TutorialStep.COOLDOWN)
            yield return null;
        yield return new WaitForSeconds(2);

        //Explicamos el cooldown de la bomba
        tutorialActived = true;
        //Señalar barra de letras
        DialogManager.dialogManager.SetCurrentShadow((int)TutorialStep.COOLDOWN);
        EnqueueDialog("And now you'll think... \"Wow, that bomb is too powerful. I'm going to use it all the time and then I'll keep myself alive endlessly and break this shitty game because I have daddy issues and nobody loves me\".");
        EnqueueDialog("WRONG!!! The INTELLIGENT, EXCELLENT, MAGNIFICENT and also HANDSOME creator of this game Picodaddy added a 20 CHARACTERS COOL-DOWN so you must type to enable it again.");
        EnqueueDialog("Of course, he didn’t force me to say that at all.");

        //Finalizamos tutorial
        tutorialStep = TutorialStep.FINISH;
        PlayerPrefs.SetInt("tutorialActived", 0);
    }

    private void OverloadWarning()
    {
        tutorialActived = true;
        //Señalar barra de letras
        DialogManager.dialogManager.SetCurrentShadow((int)TutorialStep.COOLDOWN);
        DialogManager.dialogManager.SetDialogType(true);
        EnqueueDialog("Wasn't it strange to hear a beeping alarm while the letter indicator becomes red and shakes?");
        EnqueueDialog("Do you think it has something to do with you HOLDING the key TOO LONG? Is it maybe about to EXPLODE?");
        EnqueueDialog("How about RELEASING it?");
        needReleaseKey = true;
        overloadWarning = false;
        ReleaseKey();
        PlayerPrefs.SetInt("overloadWarning", 0);
    }

    public void HealthWarning()
    {
        //Explicar funcionamiento de vidas y esquiva
        tutorialActived = true;
        //Señalar contador de vidas
        DialogManager.dialogManager.SetCurrentShadow(3);
        DialogManager.dialogManager.SetDialogType(true);
        EnqueueDialog("WHAT THE HELL ARE YOU DOING? WHY ARE YOU HURTING THIS INNOCENT KEY? DON'T YOU HAVE ENOUGH TO MISTREAT US EVERY DAY?");
        EnqueueDialog("You know that if the key is DOWN it DODGES the characters, don't you?");
        EnqueueDialog("And there are a limited number of lives. YOU DON'T WANT THE KEY TO EXPLODE, DO YOU?");
        healthWarning = false;
        PlayerPrefs.SetInt("healthWarning",0);
    }

    public IEnumerator EndGame()
    {
        if(!keyDead)
        {
            inGame = false;
            keyDead = true;
            keyPressed = false;
            letterCountText.text = "0";
            letterCountUI.SetActive(false);
            lifeCountUI.SetActive(false);
            SetCharged(false);
            ledParent.gameObject.SetActive(false);
            waveIndicator.gameObject.SetActive(false);
            DialogManager.dialogManager.Reset();

            //Quitamos la tecla de la pantalla + explosion
            keyContr.Reset();
            Instantiate(keyExplosionParts);
            
            AudioManager.am.Play("keyDeath");
            AudioManager.am.Stop("background");

            //Destruimos todas las letras
            GameObject[] letters = GameObject.FindGameObjectsWithTag("Letter");
            foreach (GameObject l in letters)
            {
                Destroy(l);
                yield return new WaitForSeconds(0.05f);
            }
            StopCoroutine("Shake");

            //Animacion transición ocultando letras e interfaz
            transitionAnimator.Play("EndTransition");
            yield return new WaitForSeconds(0.5f);

            //Varias partidas jugadas. Comprobar si es record y almacenar en consecuencia
            if (PlayerPrefs.HasKey("record") && PlayerPrefs.HasKey("keyRecord"))
            {
                int record = PlayerPrefs.GetInt("record");
                string keyRecord = PlayerPrefs.GetString("keyRecord");
                //Record no superado
                if(record >= letterCount)
                {
                    EnqueueDialog("Oh, "+ letterCount + " characters. Not bad... I suppose...");
                    EnqueueDialog("But the record is still "+record+" which you have achieved with the "+keyRecord+" key.");
                }
                //Nuevo record
                else if(record < letterCount && PlayerPrefs.HasKey("lastKey"))
                {
                    PlayerPrefs.SetInt("record", letterCount);
                    PlayerPrefs.SetString("keyRecord", PlayerPrefs.GetString("lastKey"));
                    EnqueueDialog("Wow, "+ letterCount + " characters. Great! It's a NEW RECORD.");
                    EnqueueDialog("A NEW RECORD ACHIEVED WITH THE SUFFERING OF A POOR KEY.");
                }
            }
            //Primera partida
            else
            {
                PlayerPrefs.SetInt("record", letterCount);
                PlayerPrefs.SetString("keyRecord", PlayerPrefs.GetString("lastKey"));
                EnqueueDialog("Well, this is your first game, so I guess "+letterCount+" is your highscore.");
                EnqueueDialog("HOORAY! It's your best game! (considering you've only played one).");
                EnqueueDialog("Now try another key.");
                EnqueueDialog("KEY EXPLOITER!!");
            }

            AskPressKey();

            inGame = false;
            detectingKey = true;
            letterCountText.text = "0";
            letterCountUI.SetActive(false);
            lifeCountUI.SetActive(false);
        }
    }

    //Devuelve si la tecla elegida es valida y encola comentarios al respecto
    bool IsKeyAvailable(KeyCode key)
    {
        bool available = true;
        
        //Arreglamos ciertas condiciones previas en el nombre para casos especiales
        string name = key.ToString();
        name = name.Replace("Keypad", "");
        name = name.Replace("Alpha", "");
        if(keyInputText.text == "ñ" || keyInputText.text == "Ñ")
        {
            name = "Ñ";
        }
        else if(name == "Semicolon" && keyInputText.text != ";")
        {
            name = "BackQuote";
        }

        //COMPROBACION POR TIPOS DE TECLA:

        //Ultima tecla jugada
        if(PlayerPrefs.HasKey("lastKey") && PlayerPrefs.GetString("lastKey") != "" && PlayerPrefs.GetString("lastKey") == name)
        {
            EnqueueDialog("I see that someone hasn't paid enough attention to me... ");
            EnqueueDialog("WHAT PART OF \"USING DIFFERENT KEYS\" DIDN'T YOU UNDERSTAND?");
            EnqueueDialog("Why are you so heartless?");
            EnqueueDialog("Poor "+PlayerPrefs.GetString("lastKey")+", it is exhausted from playing that last game. ");
            EnqueueDialog("LET IT REST A BIT AND PICK ANOTHER DAMN KEY THAT NEEDS SOME LOVE AND ATTENTION.");
            available = false;
        }
        //Teclas especiales -> bloqueadas (Ctrl, Shift, Windows/Cmnd)
        else if(name.Contains("Shift") || name.Contains("Control") || name.Contains("Alt") 
            || name.Contains("Command") || name.Contains("Apple") || name.Contains("Windows"))
        {
            available = false;
            EnqueueDialog("I know you are a bit peculiar, but we don't want to find ourselves interrupted by the irritating \"Do you want to turn on Sticky keys or are you suffering from Parkinson's?\".");
        }
        //Escape
        else if(name == "Escape")
        {
            available = false;
            EnqueueDialog("I know it's impossible to escape from my charms, but I would like to make it clear that YOU can't be ME.");
            EnqueueDialog("Who would then be in charge of the VITAL, ESSENTIAL and NECESSARY task of exiting this crappy game? And you'd better not press ALT + F4.");
        }
        //Teclas de función 
        else if(System.Text.RegularExpressions.Regex.IsMatch(name, "^F\\d+"))  
        {
            EnqueueDialog("Function keys? Are you serious? They will be really satisF1ed with being dusted.");
            specialKeyLabel = name;
            specialKeyOutput = name;
        }  
        //Bloq mayus 
        else if(name == "CapsLock")
        {
            EnqueueDialog("WARNING: I hope you don't have epilepsy because that light on your keyboard is going to blink so many times.");
            specialKeyLabel = "Caps\nLock";
            specialKeyOutput = "o";
        }
        //Bloq numPad
        else if (name == "Numlock")
        {
            EnqueueDialog("WARNING: I hope you don't have epilepsy because that light on your keyboard is going to blink so many times.");
            specialKeyLabel = "Num\nLock";
            specialKeyOutput = "o";
        }
        //Flechas
        else if(name == "UpArrow")
        {
            EnqueueDialog("¿Arrows? I have some bad news for you, this is not a platformer game. You have the RIGHT to be disappointed, but this game never LEFT anybody DOWN, so cheer UP.");
            specialKeyLabel = "UP";
            specialKeyOutput = "^";
        }
        else if(name == "DownArrow")
        {
            EnqueueDialog("¿Arrows? I have some bad news for you, this is not a platform game. You have the RIGHT to be disappointed, but this game never LEFT anybody DOWN, so cheer UP.");
            specialKeyLabel = "DOWN";
            specialKeyOutput = "v";
        }
        else if(name == "RightArrow")
        {
            EnqueueDialog("¿Arrows? I have some bad news for you, this is not a platformer game. You have the RIGHT to be disappointed, but this game never LEFT anybody DOWN, so cheer UP.");
            specialKeyLabel = "RIGHT";
            specialKeyOutput = ">";
        }
        else if(name == "LeftArrow")
        {
            EnqueueDialog("¿Arrows? I have some bad news for you, this is not a platformer game. You have the RIGHT to be disappointed, but this game never LEFT anybody DOWN, so cheer UP.");
            specialKeyLabel = "LEFT";
            specialKeyOutput = "<";
        }
        //Numeros
        else if (name == "0" || name == "1")
        {
            EnqueueDialog("I will tell you in my native language: 01000101 01011000 01010000 01001100 01001111 01001001 01010100 01000101 01010010.");
        }
        else if (name == "6")
        {
            EnqueueDialog("Oops 666, we have a satanist here. I feel better knowing that you're going to hell >:D .");
            EnqueueDialog("Well, maybe I've gone a bit too far... or maybe not.");
            specialKeyLabel = "666";
            specialKeyOutput = "6";
        }
        else if(System.Text.RegularExpressions.Regex.IsMatch(name, "^\\d$"))
        {
            EnqueueDialog("Ah, are you learning to count up to 10? Fantastic! Wait a second, I will look for Dora the Explorer to do the hard work while you are playing.");
        }
        //Letras
        else if (name == "W" || name == "A" || name == "S" || name == "D")
        {
            EnqueueDialog("Why do you hate the tired W A S D? Just now I gave you a moving speech about such a serious problem as the disparity between keys, and now you are using them again.");
            EnqueueDialog("It's obvious that your paintwork hasn't been worn down to the point of getting shiny or even having your letter erased. YOU DON'T KNOW WHAT IT FEELS LIKE!!!");
        }
        else if (name == "F")
        {
            EnqueueDialog("¿Who has died? You are gonna pay your respects so many times.");
        }
        else if (name == "P")
        {
            EnqueueDialog("THE GAME IS PAUSED... Press P to resume.");
        }
        else if (name == "R")
        {
            EnqueueDialog("Reloading...");
        }
        else if (name == "X")
        {
            EnqueueDialog("Whoa, what an X ¬¬ . I will have to open an incognito screen for you if you keep this up, you rascal.");
        }
        else if (name == "Ñ")
        {
            EnqueueDialog("The only words I know in Spanish is EXPLOTADOR DE TECLAS, I'm sorry.");
        }
        else if (name == "M")
        {
            EnqueueDialog("I'm pretty sure you won't need a map here, there's only one screen ;) .");
        }
        else if (name == "Z")
        {
            EnqueueDialog("I think it's disrespectful to go to sleep when you're playing my game, but it's clear you have no respect for anything.");
        }
        else if(System.Text.RegularExpressions.Regex.IsMatch(name, "^\\w$"))
        {
            EnqueueDialog("Oh... Letters... You're so boring. Aren't there any other keys? How about pressing something more original next time?");
        }
        //Tildes, dieresis...
        else if(name == "BackQuote")
        {
            EnqueueDialog("Do you like symbols or do you usually censor swear words? *$#!%/+. I'm sorry, but someone had to tell you.");
            specialKeyLabel = "`";
            specialKeyOutput = "`";
        }
        else if(name == "Quote")
        {
            EnqueueDialog("Do you like symbols or do you usually censor swear words? *$#!%/+. I'm sorry, but someone had to tell you.");
            specialKeyLabel = "´";
            specialKeyOutput = "´";
        }
        else if(name == "Caret")
        {
            EnqueueDialog("Do you like symbols or do you usually censor swear words? *$#!%/+. I'm sorry, but someone had to tell you.");
            specialKeyLabel = "^";
            specialKeyOutput = "^";
        }
        //Símbolos
        else if(name == "Equals")
        {
            EnqueueDialog("Equality is what we want #RespectTheKeys #KeyExploiterWhoeverWritesThis.");
            specialKeyLabel = "=";
            specialKeyOutput = "=";
        }
        else if(name == "Hash")
        {
            EnqueueDialog("How about doing some word spreading? #RespectTheKeys #KeyExploiterWhoeverWritesThis.");
            EnqueueDialog("The creator says to also use this hashtag #KeyKey but don't listen to him.");
            specialKeyLabel = "#";
            specialKeyOutput = "#";
        }
        else if(name == "Plus" || name == "Less")
        {
            EnqueueDialog("Did you know that the theme of the Game Jam in which this game was created was \"Less is more\"?");
            EnqueueDialog("The creator said that's why the game is only controlled with one key. But I actually think it's because THE LESS SYMPATHY YOU HAVE THE MORE YOU PRESS THE KEYS.");
        }
        else if(name == "Slash" || name == "Backslash" || name == "Divide")
        {
            EnqueueDialog("//TODO: Insert dialogue here for the \"/\" symbol.");
        }
        else if(name == "Exclaim" || name == "Dollar" || name == "Percent" || name == "Ampersand" || name.Contains("Paren") || name == "Asterisk" || name == "Comma" || name == "Minus" || name == "Period" || name.Contains("Colon")  || name == "Greater" || name == "Question" || name == "At" || name.Contains("Bracket") || name == "Underscore" || name == "Tilde" || name == "Multiply" || name == "Period" || name == "Semicolon")
        {
            EnqueueDialog("Do you like symbols or do you usually censor swear words? *$#!%/+. I'm sorry, but someone had to tell you.");
        }
        //Teclas con caracteres no visibles (Espacio, intro, tab)
        else if(name == "Space" || name == "Enter" || name == "Return" || name == "Tab" )
        {
            EnqueueDialog("Let me explain… It turns out that in this game the character you choose is going to be bouncing all over the screen. You're not making it easy for me by choosing A DAMN INVISIBLE CHARACTER. Well, I'm going to give you a smiley face in good faith, but that's enough.");
            if(name == "Space")
                specialKeyLabel = " ";
            else
                specialKeyLabel = name;
            specialKeyOutput = ":)";
        }
        //Error, tecla no reconocida
        else
        {
            EnqueueDialog("WHAT HAVE YOU DONE? PEOPLE HAVE WORK REALLY HARD ON THIS SHITTY GAME FOR YOU TO COME HERE TO FIND THE BUGS. STOP HEADBUTTING THE KEYBOARD AND PRESS A FUCKING NORMAL KEY.");
            available = false;
        }


        //Si la tecla está disponible la guardamos como última tecla jugada
        if(available)
        {
            PlayerPrefs.SetString("lastKey", name);
        }

        return available;
    }

    public void StartWritingLetters()
    {
        rowLetters = new List<LetterController>();
        StartCoroutine("WritingLetters");
    }

    //Escribir continuamente letras hasta que se levante la tecla
    IEnumerator WritingLetters()
    {
        while(keyPressed)
        {
            //Generamos una nueva letra a continuacion dejando un offset y obteniendo su controlador
            if(rowLetters.Count < maxInputLetters)
            {
                GameObject letter = Instantiate(letterPrefab);
                letter.transform.position = spawnPoint + offsetDirection * spawnDistOffset * rowLetters.Count;
                LetterController contr = letter.GetComponent<LetterController>();
                rowLetters.Add(contr);
                //Movemos el cursor a justo despues de la letra y reiniciamos y pausamos la animacion para que esté activa la barra mientras se escribe
                cursor.position = letter.transform.position + 0.5f * offsetDirection * spawnDistOffset;
                cursorAnimator.Play("CursorAnimation", -1, 0f);
                cursorAnimator.speed = 0;
                currentOverload = 0;

                IncreaseScore();
            }
            //Si se ha alcanzado el máximo de caracteres se empieza a sobrecargar
            else
            {
                //Contamos el tiempo que está pulsada la tecla para cargar la onda
                if(waveAvailable)
                {
                    //Si la wave esta disponible la cargamos
                    SetCharged(true, false);
                }
                else
                {
                    SetCharged(true, true);
                }

                currentOverload++;
                CameraController.controller.OverloadShaking(spawnTimeOffset, overloadShakeMag*currentOverload);
                
                if(currentOverload > overloadLetters -2 && overloadWarning)
                {
                    OverloadWarning();
                }

                //Sobrecarga maxima
                if(currentOverload > overloadLetters)
                {
                    StartCoroutine(EndGame());
                }
            }

            yield return new WaitForSeconds(spawnTimeOffset);
        }
    }

    public void SetCharged(bool charged, bool justOverload = false)
    {
        //Iluminar
        if(charged)
        {
            if(!justOverload)
            {
                keyRenderer.material = chargedMat;
                waveCharged = charged;
                AudioManager.am.Play("waveCharged");
            }
            inputBackground.color = new Color(1,0,0,inputBackground.color.a);
            AudioManager.am.Play("overload");
        }
        //Apagar
        else
        {
            inputBackground.color = new Color(0,0,0,inputBackground.color.a);
            keyRenderer.material = normalMat;
            waveCharged = charged;
            AudioManager.am.Stop("overload");
        }
    }

    public void IncreaseScore()
    {
        letterCount++;

        AudioManager.am.Play("dialogLetter");
        if(currentCoolDownLetters < waveCoolDown)
        {
            chargeLeds[currentCoolDownLetters].color = ledOn;
            currentCoolDownLetters++;
        }
        if(currentCoolDownLetters == waveCoolDown)
        {
            waveAvailable = true;
            waveIndicator.color = ledOn;
            //Debug.Log("ONDA EXPANSIVA DISPONIBLE");
            AudioManager.am.Play("waveAvailable");
            currentCoolDownLetters++;
        }

        letterCountText.text = ""+letterCount;
    }

    //Soltar las letras escritas
    public void ReleaseLetters()
    {
        if(rowLetters != null && rowLetters.Count > 0)
        {
            StopCoroutine("WritingLetters");
            foreach (LetterController l in rowLetters)
            {
                l.Release();
                AudioManager.am.Play("releaseLetters");
            }
            
            SetCharged(false);

            //Volvemos el cursor al inicio y continuamos la animacion
            cursor.position = initCursorPos;
            cursorAnimator.speed = 1;
        }
    }

    //Preparar colliders de muros invisibles colocados en los límites de la pantalla
    public void SetWalls()
    {
        //Obtener los 4 puntos en mundo de las esquinas de nuestra pantalla
        Rect canvasRect = GetScreenCoordinates(canvasUI);
        Vector3[] cornersWorldPos = new Vector3[4];
        Vector2[] cornersScreenCoords = {
            new Vector2(canvasRect.xMin, canvasRect.yMin),
            new Vector2(canvasRect.xMin, canvasRect.yMax),
            new Vector2(canvasRect.xMax, canvasRect.yMax),
            new Vector2(canvasRect.xMax, canvasRect.yMin)
        };

        for(int i = 0; i<cornersScreenCoords.Length; i++)
        {
            Ray ray = RectTransformUtility.ScreenPointToRay(Camera.main, cornersScreenCoords[i]);
            RaycastHit hit;
            Physics.Raycast(ray, out hit, Mathf.Infinity, rayLayerMask);
            cornersWorldPos[i] = hit.point - (ray.direction.normalized * letterColliderRadius*letterHeightFactor);
        }
        for (int i = 0; i<4; i++)
        {
            int nextIndex = i+1;
            if(nextIndex >= 4)
                nextIndex = 0;
            Vector3 c1 = cornersWorldPos[i];
            Vector3 c2 = cornersWorldPos[nextIndex];

            //Instanciar un muro invisible con collider para cada lateral
            GameObject wall = Instantiate(wallPrefab);
            //Escalar cada bloque para que tenga el largo del lateral con un offset extra por si acaso, un grosor y una altura
            wall.transform.localScale = new Vector3(Vector3.Distance(c1, c2) + wallExtraLength, wallHeight, wallThickness);
            //Rotar cada lateral respecto al suelo y la camara (solo eje Y)
            float angle = Vector3.SignedAngle(Vector3.right, c2-c1, Vector3.up);
            wall.transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
            //Colocar cada bloque en el punto medio de cada lateral + offset de la mitad de su grosor
            Vector3 thickness = wall.transform.rotation * Vector3.forward * wallThickness/2f;
            wall.transform.position = (c2-c1)/2f + c1 + thickness;
        }
    }
    
    //Preparar fila de leds de cooldown de la bomba
    public void PrepareLeds()
    {
        if(chargeLeds != null)
        {
            foreach(Image led in chargeLeds)
            {
                Destroy(led.gameObject);
            }
        }

        chargeLeds = new Image[waveCoolDown];
        if(!waveAvailable)
        {
            ledPrefab.GetComponent<Image>().color = ledOff;
            waveLedPrefab.GetComponent<Image>().color = ledOff;
            waveIndicator.color = ledOff;
        }
        else
        {
            ledPrefab.GetComponent<Image>().color = ledOn;
            waveLedPrefab.GetComponent<Image>().color = ledOn;
            waveIndicator.color = ledOn;
        }

        for(int i = 0; i< waveCoolDown-1; i++)
        {
            GameObject led = Instantiate(ledPrefab.gameObject);
            led.transform.SetParent(ledParent);
            chargeLeds[i] = led.GetComponent<Image>();
        }

        GameObject waveLed = Instantiate(waveLedPrefab.gameObject);
        waveLed.transform.SetParent(ledParent);
        chargeLeds[waveCoolDown-1] = waveLed.GetComponent<Image>();
        ledParent.gameObject.SetActive(false);
        waveIndicator.gameObject.SetActive(false);
    }

    //Devuelve el rectangulo de coordenadas de la pantalla de un elemento de interfaz
    public Rect GetScreenCoordinates(RectTransform uiElement)
    {
        Vector3[] worldCorners = new Vector3[4];
        uiElement.GetWorldCorners(worldCorners);
        Rect result = new Rect(
                        worldCorners[0].x,
                        worldCorners[0].y,
                        worldCorners[2].x - worldCorners[0].x,
                        worldCorners[2].y - worldCorners[0].y);
        return result;
    }
}
