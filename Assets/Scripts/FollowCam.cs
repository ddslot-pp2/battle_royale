using UnityEngine;
using System.Collections;

public class FollowCam : MonoBehaviour {

    public GameObject Player;

    public float offsetX = 25f;
    public float offsetY = 0f;
    public float offsetZ = -35f;

    public float followSpeed = 0.25f;

    Vector3 comeraPosition;

    void Start()
    {
       
    }
        
    void LateUpdate()
    {
        comeraPosition.x = Player.transform.position.x + offsetX;
        comeraPosition.y = Player.transform.position.y + offsetY;
        comeraPosition.z = Player.transform.position.z + offsetZ;

        transform.position = Vector3.Lerp(transform.position, comeraPosition, followSpeed * Time.deltaTime);
    }
}
