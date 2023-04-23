using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleManager : MonoBehaviour
{
    public Animator fade;
    public bool controlAvailable = false;
    public bool tutorial = true;
    public bool credits = false;

    public TextMeshProUGUI textTutorial;
    
    void Start()
    {
        StartCoroutine(StartTransition());
        //PlayerPrefs.DeleteAll();

        if(PlayerPrefs.HasKey("tutorialActived"))
            tutorial = PlayerPrefs.GetInt("tutorialActived") == 1;
        else
            PlayerPrefs.SetInt("tutorialActived", 1);

        if(tutorial && !credits && textTutorial != null)
            textTutorial.text = "Tutorial: <color=#FFFFFF>ON</color>";
        else
            textTutorial.text = "Tutorial: <color=#FFFFFF>OFF</color>";
    }

    // Update is called once per frame
    void Update()
    {
        if(controlAvailable)
        {
            if(!credits)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Application.Quit();
                    Debug.Log("SALIENDOOOOOO");
                }
                else if (Input.GetKeyDown(KeyCode.C))
                {
                    StartCoroutine(ChangeScene("Credits"));
                }
                else if (Input.GetKeyDown(KeyCode.T))
                {
                    SwitchTutorial();
                }
                else if(Input.anyKeyDown)
                {
                    StartCoroutine(ChangeScene("Game"));
                }
            }
            else
            {
                if(Input.anyKeyDown)
                {
                    StartCoroutine(ChangeScene("Title"));
                }
            }
        }
    }

    void SwitchTutorial()
    {
        tutorial = !tutorial;
        
        if(tutorial)
        {
            textTutorial.text = "Tutorial: <color=#FFFFFF>ON</color>";
            PlayerPrefs.SetInt("tutorialActived", 1);
        }
        else
        {
            textTutorial.text = "Tutorial: <color=#FFFFFF>OFF</color>";
            PlayerPrefs.SetInt("tutorialActived", 0);
        }
    }   

    IEnumerator ChangeScene(string scene)
    {
        //Transcion de salida
        controlAvailable = false;
        fade.Play("FadeBlackOut");
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene(scene);
    }

    IEnumerator StartTransition()
    {
        fade.Play("FadeBlackIn");
        yield return new WaitForSeconds(0.5f);
        controlAvailable = true;
    }
}
