using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LetterController : MonoBehaviour
{
    private Rigidbody rb;
    private Collider coll;
    public static float releaseForceMag;
    public float minSpeed = 5;
    public float maxSpeed = 10;
    //Texto de la letra
    public TextMeshProUGUI letterText;

    bool isQuitting = false;

    public GameObject explosionParts;

    void Awake()
    {
        //Desactivar colisiones inicialmente
        coll = gameObject.GetComponent<Collider>();
        coll.isTrigger = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        //Orientar con la camara
        transform.rotation = Camera.main.transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        //Mantener una velocidad minima
        if(rb.velocity.magnitude < minSpeed)
        {
            rb.velocity = rb.velocity.normalized*minSpeed;
        }
        else if(rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized*maxSpeed;
        }
    }

    //FIX bloquear rotacion manualmente (con rb a veces falla)
    void LateUpdate()
    {
        transform.localEulerAngles = new Vector3(Camera.main.transform.localEulerAngles.x, Camera.main.transform.localEulerAngles.y, transform.localEulerAngles.z);
    }

    //Cambiar letra que se instanciará
    public void SetLetter(string letter)
    {
        letterText.text = letter;
    }

    //TODO: METODO PARPADEO DE BLOQS

    public void Release()
    {
        //Desactivar ignorar colisiones
        if(coll != null && rb!= null)
        {
            coll.isTrigger = false;
            //Comunicar impulso en direccion random a la letra
            Vector3 randomDir = new Vector3 (Random.Range(-1f,1f), 0, Random.Range(-1f,1f)).normalized;
            rb.AddForce(randomDir * releaseForceMag, ForceMode.Impulse);
        }
    }

    //Evitar mensaje de objetos no destruidos al cerrar aplicacion
    void OnApplicationQuit()
    {
        isQuitting = true;
    }

    //Al destruir instanciamos la explosion de letras
    void OnDestroy()
    {
        //Evitar mensaje de objetos no destruidos al cerrar aplicacion
        if(!isQuitting)
        {
            AudioManager.am.Play("letterDeath");
            GameObject parts = Instantiate(explosionParts);
            parts.transform.position = transform.position;
            Destroy(parts, parts.GetComponent<ParticleSystem>().main.duration);
        }
    }
}
