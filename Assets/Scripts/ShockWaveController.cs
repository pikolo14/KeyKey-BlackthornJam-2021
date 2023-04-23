using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShockWaveController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    void OnParticleCollision(GameObject other)
    {

        if(other.tag == "Letter")
        {
            Destroy(other);
            //TODOOOOOOO: CAMBIAR LAYER WATER DE TECLA Y SUELO Y CAMBIAR EL COLLIDER DE CADA PARTICULA O ALGO
        }
    }
}
