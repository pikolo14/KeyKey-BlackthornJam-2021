using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class KeyController : MonoBehaviour
{
    //Animator para subir, bajar, aparecer...
    private Animator keyAnimator;
    
    private bool inGame = false;
    public int initLifes = 5;
    public int currentLifes;
    

    //Texto de la tecla
    public TextMeshProUGUI keyText;

    //ONDA EXPANSIVA:
    public GameObject shockWaveParts;
    private float pressedTime = 0;
    private bool pressed = false;
    
    public float wavePressedTime = 1.5f;
    public int waveCoolDown = 10;
    private bool waveCharged = false;
    public bool waveAvailable = true;


    //TEMBLOR PANTALLA:
    public float hitShakeDuration, hitShakeMagnitude; 
    public float deathShakeDuration, deathShakeMagnitude; 

    public TextMeshProUGUI lifeCountText;
    


    // Start is called before the first frame update
    void Start()
    {
        keyAnimator = GetComponent<Animator>();
        keyAnimator.speed = 0;
        currentLifes = initLifes;
        lifeCountText.text = ""+ currentLifes;
    }

    public void Reset ()
    {
        keyAnimator.SetBool("Pressed", false);
        keyAnimator.Play("SpawnKey");
        keyAnimator.speed = 0;
        currentLifes = initLifes;
        lifeCountText.text = ""+ currentLifes;
        inGame = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(inGame)
        {
            keyAnimator.speed = 1;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.tag == "Letter")
        {
            Destroy(collision.collider.gameObject);
            GetHit();
        }
    }

    void GetHit()
    {
        currentLifes--;
        lifeCountText.text = ""+ currentLifes;
        AudioManager.am.Play("keyHit");

        //Mostramos la explicacion de las vidas del tutorial       
        if(GameManager.gameManager.healthWarning)
            GameManager.gameManager.HealthWarning();

        if(currentLifes == 0)
        {
            StartCoroutine(GameManager.gameManager.EndGame());
            CameraController.controller.StartShaking(deathShakeDuration, deathShakeMagnitude);
        }
        else
        {
            if(GameManager.gameManager.tutorialStep == GameManager.TutorialStep.WAIT_BOMB && currentLifes <= 2)
                GameManager.gameManager.tutorialStep = GameManager.TutorialStep.BOMB;
                
            CameraController.controller.StartShaking(hitShakeDuration, hitShakeMagnitude);
        }
    }

    public void StartAsNormalkey(string text)
    {   
        //Poner letra en tecla sobre el modelo
        keyText.text = text;
        //Animacion de aparecer o algo
        keyAnimator.Play("SpawnKey");
        inGame = true;
    }

    public void PressKey()
    {
        if(inGame)
        {
            keyAnimator.SetBool("Pressed", true);    
            pressed = true;
            AudioManager.am.Play("keyDown");
        }
    }

    public void ReleaseKey()
    {
        if(inGame)
        {
            keyAnimator.SetBool("Pressed", false);
            AudioManager.am.Play("keyUp");
        }
    }
}
