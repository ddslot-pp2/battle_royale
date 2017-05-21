using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Event : MonoBehaviour {
    public GameObject enemyTank;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void CreateTank()
    {
        GameObject obj =  Instantiate(enemyTank);
        obj.transform.position = new Vector3(0, 3, 0);
    }
}
