using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    public static DialogManager dialogManager;

    public static Action OnLastPulsation;

    public Image inputShadow, counterShadow, lifeShadow;
    public Image currentShadow;
    public TextMeshProUGUI dialogText, tutorialText;
    public TextMeshProUGUI currentTextDisplay;
    public Image tutorialPanel;
    public float tutorialPanelOpacity;
    public float tutorialFadeTime = 0.3f;
    public float letterTime = 0.01f;
    private bool dialogInProgress = false;
    private bool shownDialogFinished = false;
    private bool rushTyping = false;

    //Cola de dialogos pendientes por mostrar
    public Queue<string> dialogsQueue = new Queue<string>();

    //Cantidad de temblor al escribir una mayúscula
    public float shakeMag = 0.08f;
    
    void Awake()
    {
        //Patron singleton
        if(dialogManager == null)
        {
            dialogManager = this;
        }
        else
        {
            Destroy(this);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        dialogsQueue = new Queue<string>();
        //Por defecto seran dialogos normales (no de tutorial)
        SetDialogType();
    }

    public void Reset()
    {
        //Si estabamos en tutorial mode eliminamos comentarios, cambiamos modo y ocultamos dialogos del tutorial
        if(tutorialText == currentTextDisplay)
        {
            Color col = tutorialPanel.color;
            col.a = 0;
            tutorialText.text = "";
            tutorialPanel.color = col;
            DialogManager.dialogManager.SetDialogType();
            while(dialogsQueue.Count>0)
            {
                dialogsQueue.Dequeue();
            }
        }
    }

    //Se añade el dialogo a la cola y si no hay dialogo en pantalla se comienza a escribir
    public void EnqueueDialog(string dialog)
    {
        dialogsQueue.Enqueue(dialog);
        
        //Debug.Log("DIALOGO ENCOLADO: "+ dialog);
        if(!dialogInProgress && !shownDialogFinished)
        {
            //Si es dialogo de tutorial hacemos un fade in
            StopCoroutine("TutorialFade");
            StartCoroutine(TutorialFade());
            Continue();
        }
    }

    public void SetCurrentShadow(int type)
    {   
        switch(type)
        {
            //Objetivo
            case 1:
                currentShadow = counterShadow;
                break;
            //Vidas
            case 3:
                currentShadow = lifeShadow;
                break;
            //Bomba o cooldown
            case 5:
            case 7:
                currentShadow = inputShadow;
                break;
            //Por defecto ninguna sombra
            default:
                currentShadow = null;
                break;
        }
    }

    //Corrutina para mostrar letra a letra el texto
    IEnumerator TypeDialog()
    {
        dialogInProgress = true;
        shownDialogFinished = false;
        currentTextDisplay.text = "|";

        //Recogemos el primero de la cola y lo escribimos
        string dialog = dialogsQueue.Dequeue();

        if(dialog == "")
        {
            Debug.Log("ERROR AL RECOGER DIALOGO");
        }
        else
        {
            foreach(char c in dialog)
            {
                //Agitar pantalla cuando haya palabras en mayuscula
                if(char.IsUpper(c) && currentTextDisplay.text.Length >=2 && currentTextDisplay.text[currentTextDisplay.text.Length-2] != ' ')
                {
                    CameraController.controller.StartShaking(letterTime, shakeMag);
                    AudioManager.am.Play("dialogCapital");
                }
                else if(c != ' ')
                {
                    AudioManager.am.Play("dialogLetter");
                }

                currentTextDisplay.text = currentTextDisplay.text.Substring(0, currentTextDisplay.text.Length-1) + c + "|";

                if(rushTyping)
                {
                    currentTextDisplay.text = dialog + "|";
                    rushTyping = false;
                    AudioManager.am.Play("skipDialog");
                    break;
                }
                yield return new WaitForSecondsRealtime(letterTime);
            }
        }

        if(dialogsQueue.Count == 0)
        {
            //Avisamos al gamemanager de que es la ultima pulsación para acabar los dialogos
            OnLastPulsation();
        }

        dialogInProgress = false;
        shownDialogFinished = true;
    }

    //Si estamos en modo tutorial, hacemos fade in o out del panel del dialogo
    IEnumerator TutorialFade(bool fadeOut = false)
    {
        if(currentTextDisplay == tutorialText)
        {
            float elapsed = 0;
            float increment = Time.fixedDeltaTime*tutorialPanelOpacity/tutorialFadeTime;
            Color color = tutorialPanel.color;
            
            //Iniciamos el fade para ser de mas a menos opacidad o al reves
            if(fadeOut)
            {
                color.a = tutorialPanelOpacity;
                increment = -increment;
            }
            else{
                color.a = 0;
            }

            //tutorialPanel.color = color;
            
            //Quitamos/Añadimos opacidad progresivamente
            while (elapsed < tutorialFadeTime)
            {
                elapsed += Time.fixedDeltaTime;
                color.a += increment;

                if(currentShadow != null)
                {
                    currentShadow.color = color;
                }
                else
                {
                    tutorialPanel.color = color;
                }

                yield return new WaitForSecondsRealtime(Time.fixedDeltaTime);
            }

            if(fadeOut)
            {
                SetCurrentShadow(-1);
                tutorialPanel.color = color;
            }
        }
    }

    public void SetDialogType(bool tutorial = false)
    {
        tutorialText.text = "";
        dialogText.text = "";

        if(tutorial)
        {
            currentTextDisplay = tutorialText;
        }
        else
        {
            currentTextDisplay = dialogText;
        }
    }

    //Pasar al siguiente dialogo o mostrar directamente uno en proceso
    public void Continue()
    {
        //Si se está escribiendo mostrar todo el texto inmediatamente
        if(dialogInProgress)
        {
            rushTyping = true;
        }
        //Si el dialogo ya está escrito o no hay texto...
        else
        {
            //...y hay dialogos en cola, se escribe el siguiente
            if(dialogsQueue.Count > 0)
            {
                StartCoroutine("TypeDialog");
            }
            //...y no hay mas dialogos, vaciamos el texto
            else
            {
                currentTextDisplay.text = "";
                shownDialogFinished = false;
                //Si estamos en tutorial hacemos un fade out
                StopCoroutine("TutorialFade");
                StartCoroutine(TutorialFade(true));
            }
        }
    }
}
