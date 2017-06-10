using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Direct_Tank_Topfire : MonoBehaviour {
    //총알 발사좌표
    public Transform firePos_p1;
    //MuzzleFlash의 MeshRenderer 컴포넌트 연결 변수
    public MeshRenderer muzzleFlash_1;


    RaycastHit TFire;
    Vector3 Click;
    Quaternion dir;
    Tank_State state;

    public float nextfire = 0.0f;//다음 총알 발사시간

    void Start()
    {

        state = gameObject.GetComponentInParent<Direct_Tank_State>();
        //최초에 MuzzleFlash MeshRenderer를 비활성화
        muzzleFlash_1.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {

            Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out TFire);
            Click = TFire.point;
            Click.y = transform.position.y;
            dir = Quaternion.LookRotation((Click - transform.position).normalized);

            transform.rotation = dir;


            Fire();
                
            }
        
    }

    void Fire()
    {
        if (Time.time >= nextfire)
        {
            nextfire = Time.time + state.fireRate;
            CreateBullet();

            //잠시 기다리는 루틴을 위해 코루틴 함수로 호출
            StartCoroutine(this.ShowMuzzleFlash());
        }
    }

    void CreateBullet()
    {
        //Bullet 프리팹을 동적으로 생성
        GameObject bulletLocalSize = Instantiate(state.bullet, firePos_p1.position, firePos_p1.rotation);
        bulletLocalSize.transform.localScale = new Vector3(bulletLocalSize.transform.localScale.x * state.bulletSize, bulletLocalSize.transform.localScale.y * state.bulletSize, bulletLocalSize.transform.localScale.z * state.bulletSize);
        bulletLocalSize.GetComponent<DirectBullet>().GetDamageType(state.damage, 2, transform.parent.gameObject, state.range, 18.0f);
    }

    IEnumerator ShowMuzzleFlash()
    {
        //MuzzleFlash 스케일을 불규칙하게 변경
        float scale = Random.Range(2.0f, 4.0f);
        muzzleFlash_1.transform.localScale = Vector3.one * scale;

        //활성화해서 보이게 함
        muzzleFlash_1.enabled = true;

        //불규칙적인 시간 동안 Delay한 다음 MeshRenderer를 비활성화
        yield return new WaitForSeconds(Random.Range(0.05f, 0.3f));

        //비활성화해서 보이지 않게 함
        muzzleFlash_1.enabled = false;

    }
}
