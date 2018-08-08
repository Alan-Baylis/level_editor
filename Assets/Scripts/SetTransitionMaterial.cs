using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTransitionMaterial : MonoBehaviour {

	public Material opaqueMaterial;
	public Material transitionMaterial;
	private Material sharedMaterial;
	public string tagName;
    public bool setOpaqueMaterial = false;
    public bool transitionIsOver = false;
    public float normalPush = 0.0f;
    public float cutoff = 0.0f;
	private GameObject[] targetObjects;


	void OnEnable ()
    {
		sharedMaterial = new Material(transitionMaterial);

        targetObjects = GameObject.FindGameObjectsWithTag(tagName);
		
			foreach (GameObject renderer in targetObjects)
        {
            renderer.GetComponent<Renderer>().material = sharedMaterial;
        }
	}
	

	void LateUpdate ()
    {
        sharedMaterial.SetFloat("_NormalPush", normalPush);
        sharedMaterial.SetFloat("_Cutoff", cutoff);
        if (transitionIsOver) {
            gameObject.SetActive(false);
        }
	}

    void OnDisable()
    {
        if (setOpaqueMaterial) {
        foreach (GameObject renderer in targetObjects)
        {
            renderer.GetComponent<Renderer>().material = opaqueMaterial;
        }
    }
        transitionIsOver = false;
    }

}
