using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using network;

public class Direct_Tank_Topfire : MonoBehaviour {
    //총알 발사좌표
    public Transform firePos_p1;
    //MuzzleFlash의 MeshRenderer 컴포넌트 연결 변수
    public MeshRenderer muzzleFlash_1;

    private protobuf_session session_;

    RaycastHit TFire;
    Vector3 Click;
    Quaternion dir;
    Tank_State state;

    //Dictionary<Int64, GameObject> bullets = null;

    public float nextfire = 0.0f;//다음 총알 발사시간

    void Start()
    {
        session_ = protobuf_session.getInstance();
        state = gameObject.GetComponentInParent<Direct_Tank_State>();
        //최초에 MuzzleFlash MeshRenderer를 비활성화
        muzzleFlash_1.enabled = false;
    }

    void Update()
    {

        var skill_id = 1;

#if UNITY_STANDALONE_WIN
        // pc
        if (Input.GetMouseButtonDown(0))
        {
            Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out TFire);
            Click = TFire.point;
            Click.y = transform.position.y;
            dir = Quaternion.LookRotation((Click - transform.position).normalized);
 
            transform.rotation = dir;

            Debug.Log("x: " + transform.forward.x + ", y: " + transform.forward.y + ", z: " + transform.forward.z);
            //transform.rotation = new Quaternion( dir.x , dir.y, dir.w, 1.0f);

            SendUseSkill(skill_id, transform.forward, dir);
            //Fire();
        }
#endif


#if UNITY_ANDROID
        // mobile device
        int count = Input.touchCount;
        for (int i = 0; i < count; i++)
        {
            if (EventSystem.current.IsPointerOverGameObject(i) == false)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                {

                    Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out TFire);
                    Click = TFire.point;
                    Click.y = transform.position.y;
                    dir = Quaternion.LookRotation((Click - transform.position).normalized);

                    transform.rotation = dir;

                    SendUseSkill(skill_id, transform.forward, dir);
                    //Fire();
                }
            }
        }
#endif
    }

    void SendUseSkill(int skill_id, Vector3 forward, Quaternion rot)
    {
        GAME.CS_USE_SKILL send = new GAME.CS_USE_SKILL();

        send.SkillId = skill_id;

        send.ForwardX = forward.x;
        send.ForwardY = forward.y;
        send.ForwardZ = forward.z;

        send.RotX = rot.x;
        send.RotY = rot.y;
        send.RotZ = rot.z;
        send.RotW = rot.w;

        session_.send_protobuf(opcode.CS_USE_SKILL, send);
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

    public void Fire(Quaternion rot, Vector3 pos, Vector3 forward, float distance, float speed)
    {
        Debug.Log("호출2");
        transform.rotation = rot;

        CreateBullet(rot, pos, forward, distance, speed);

        //잠시 기다리는 루틴을 위해 코루틴 함수로 호출
        StartCoroutine(this.ShowMuzzleFlash());
    }

    void CreateBullet(Quaternion rot, Vector3 pos, Vector3 forward, float distance, float speed)
    {
        //Bullet 프리팹을 동적으로 생성
        var bullet = Instantiate(state.bullet, pos, rot);
        bullet.transform.localScale = new Vector3(bullet.transform.localScale.x * state.bulletSize, bullet.transform.localScale.y * state.bulletSize, bullet.transform.localScale.z * state.bulletSize);
        bullet.GetComponent<DirectBullet>().GetDamageType(state.damage, 2, transform.parent.gameObject, state.range, 18.0f);
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
