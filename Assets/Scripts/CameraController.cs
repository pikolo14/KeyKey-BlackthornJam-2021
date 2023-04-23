using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CameraController : MonoBehaviour
{
    public static CameraController controller;
    public TextMeshProUGUI dialogText;
    public RectTransform input;
    private Vector3 originalCamPos, originalTextPos, originalInputPos;

    private float duration, magnitude;
    private bool overload = false;

    void Awake()
    {
        if(controller == null)
        {
            controller = this;
        }
        else
        {
            Destroy(this);
        }

        originalCamPos = transform.localPosition;
        originalTextPos = dialogText.rectTransform.position;
        originalInputPos = input.position;
    }

    public void StartShaking(float _duration, float _magnitude)
    {
        duration = _duration;
        magnitude = _magnitude;
        StopCoroutine("Shake");
        StartCoroutine("Shake");
    }

    public void OverloadShaking(float _duration, float _magnitude)
    {
        overload = true;
        StartShaking(_duration, _magnitude);
    }

    
    private IEnumerator Shake()
    {
        float elapsed = 0.0f;
        bool auxOverload = overload;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            float z = Random.Range(-1f, 1f) * magnitude;

            if(!auxOverload)
            {
                transform.localPosition = new Vector3(x,y,z) + originalCamPos;
                dialogText.rectTransform.position = new Vector3(x,y,0)*80 + originalTextPos;
            }
            else
            {
                input.position = new Vector3(x,y,0)*80 + originalInputPos;
                overload = false;
            }

            elapsed += Time.fixedDeltaTime;
            yield return null;
        }

        transform.localPosition = originalCamPos;
        dialogText.rectTransform.position = originalTextPos;
        input.position = originalInputPos;
    }
}
