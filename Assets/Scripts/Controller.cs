using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Controller : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler {
    private Image joystickImg;
    private Vector3 inputVector;
    private Vector3 first;
    public Vector3 dir;
    public Vector2 pos;
    public float distance;
    // Use this for initialization
    
    void Start () {
        joystickImg = transform.GetChild(0).GetComponent<Image>();
        first = joystickImg.transform.position;
    }
	
    public void OnDrag(PointerEventData ped)
    {
        dir = (new Vector3(ped.position.x, ped.position.y) - first).normalized;
        pos = ped.position;
        distance = Vector3.Distance(first, pos);
        if (distance >= gameObject.transform.localScale.x*50)
        {
            joystickImg.transform.position = first + (dir * gameObject.transform.localScale.x * 50);
        }
        else
        {
            joystickImg.transform.position = ped.position;
        }
    }

	public void OnPointerDown(PointerEventData ped)
    {
        OnDrag(ped);
    }

   public virtual void OnPointerUp(PointerEventData ped)
    {
        inputVector = Vector3.zero;
        joystickImg.rectTransform.anchoredPosition = Vector3.zero;
        dir = new Vector3(0 , 0,0);
    }

    public float GetHorizontalValue()
    {
        return inputVector.x;
    }

    public float GetVerticalValue()
    {
        return inputVector.y;
    }

    public void OnClick()
    {
        Debug.Log("123");
    }
}
