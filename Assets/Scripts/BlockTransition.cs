using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockTransition : MonoBehaviour {

    public Transform cursorPosition;
    public Material opaqueMaterial;
	public Material transitionMaterial;
	private Material sharedMaterialIn;
    private Material sharedMaterialOut;
    public string tagNameIn;
    public string tagNameOut;
    public bool setOpaqueMaterial = false;
    public bool transitionIsOver = false;
    public float waveamount = 0.0f;
    public float wavedistance = 0.0f;
    public float wavesin = 0.0f;
    public float cursoralpha = 1.0f;
    public float cutoffOut = 0.0f;
    public float cutoffIn = 0.0f;
	private GameObject[] targetObjectsIn;
    private GameObject[] targetObjectsOut;


    void OnEnable ()
    {
        transform.position = cursorPosition.position;

        Shader.SetGlobalFloat("_WorldX", transform.position.x);
        Shader.SetGlobalFloat("_WorldY", transform.position.y);
        Shader.SetGlobalFloat("_WorldZ", transform.position.z);

        sharedMaterialIn = new Material(transitionMaterial);
        sharedMaterialOut = new Material(transitionMaterial);

        targetObjectsIn = GameObject.FindGameObjectsWithTag(tagNameIn);
		
			foreach (GameObject rendererIn in targetObjectsIn)
        {
            rendererIn.GetComponent<Renderer>().material = sharedMaterialIn;
        }

        targetObjectsOut = GameObject.FindGameObjectsWithTag(tagNameOut);

        foreach (GameObject rendererOut in targetObjectsOut)
        {
            rendererOut.GetComponent<Renderer>().material = sharedMaterialOut;
        }
    }

        void LateUpdate ()
    {
        Shader.SetGlobalFloat("_WaveAmount", waveamount);
        Shader.SetGlobalFloat("_WaveDistance", wavedistance);
        Shader.SetGlobalFloat("_WaveSin", wavesin);
        Shader.SetGlobalFloat("_CursorAlpha", cursoralpha);
        sharedMaterialOut.SetFloat("_Cutoff", cutoffOut);
        sharedMaterialIn.SetFloat("_Cutoff", cutoffIn);

        if (transitionIsOver) {
            gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat("_WaveAmount", 0.0f);
        Shader.SetGlobalFloat("_WaveDistance", 0.5f);
        Shader.SetGlobalFloat("_WaveSin", 0.0f);
        Shader.SetGlobalFloat("_CursorAlpha", 1.0f);

        if (setOpaqueMaterial) {

        foreach (GameObject rendererOut in targetObjectsIn)
        {
                rendererOut.GetComponent<Renderer>().material = opaqueMaterial;
        }

        }
        transitionIsOver = false;
    }

}
