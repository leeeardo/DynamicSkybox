using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class SetEffectPos : MonoBehaviour {

    public Material material;
    
    private string positionProperty = "_Pos";
    private string radiusProperty = "_Radius";
    // could add [SerializeField] attribute or make public to set from inspector

    void Start(){
        //material = GetComponent<Renderer>().sharedMaterial;

    }

    void Update(){
        if (material!=null)
        {
            material.SetVector(positionProperty, transform.position);
            material.SetFloat(radiusProperty, transform.localScale.x);
        }
        //transform.Rotate();
        
    }
}
