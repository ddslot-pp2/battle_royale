using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using network;
using System;
using UnityEngine.UI;

public class NewbieSceneManager : MonoBehaviour
{

    private protobuf_session session_;

    private bool is_loading = true;

    class snapshot
    {
        public Vector3 pos = new Vector3(0.0f, 0.0f, 0.0f);
        public Quaternion rot = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        public Int64 timestamp;
        public Int64 latency = 0;
    }

    class EnemyTankInfo
    {
        public GameObject obj;
        public snapshot[] snapshots = null;

        public EnemyTankInfo()
        {
            snapshots = new snapshot[3];
            for (var i = 0; i < max_snapshot_size; ++i)
            {
                snapshots[i] = new snapshot();
            }
        }
    }

    class BulletInfo
    {
        public GameObject bullet;
        //public int damage = 0;
    }

    Dictionary<Int64, EnemyTankInfo> enemies = null;
    Dictionary<Int64, BulletInfo> bullets = null;

    float interval_;
    public Vector3 pos_;
    public Quaternion rot_;

    private float ping_interval_;
    private float update_interval_;

    public Text ping_text;
    public Text interpolation_text;
    public Text delta_text;

    Int64 recv_count = 0;

    public Int64 sum_latency = 0;
    public Int64 recv_latency_count = 0;

    public Int64 prev_render_time = 0;

    private static int max_snapshot_size = 2;

    bool is_interpolation = true;

    public Queue<Int64> latency_queue_;

    private static int max_latency_size = 10;

    private Int64 key_ = 0;

    public void OnConnect(bool result)
    {
        if (result)
        {
            Debug.Log("접속 성공2");
        }
        else
        {
            Debug.Log("접속 실패");
        }
    }

    void OnDisconnect()
    {
        Debug.Log("접속 끊김");
    }

    void Connect()
    {
        session_.connect();
    }

    void Disconnect()
    {
        session_.disconnect();
    }

    public void onClickConnectButton()
    {
        Debug.Log("접속 시도 버튼 클릭");
        Connect();
    }

    public void onClickDisconnectButton()
    {
        Debug.Log("연결 끊김 버튼 클릭");
        Disconnect();
    }

    // 여기에 이 씬에서 사용할 패킷 callback을 등록
    void RegisterProcessorHandler()
    {
        session_.processor_SC_ENTER_FIELD        = processor_SC_ENTER_FIELD;
        session_.processor_SC_NOTI_ENTER_FIELD   = processor_SC_NOTI_ENTER_FIELD;
        session_.processor_SC_NOTI_MOVE_OBJECT   = processor_SC_NOTI_MOVE_OBJECT;
        session_.processor_SC_NOTI_LEAVE_FIELD   = processor_SC_NOTI_LEAVE_FIELD;
        session_.processor_SC_NOTI_USE_SKILL     = processor_SC_NOTI_USE_SKILL;
        session_.processor_SC_NOTI_DESTROY_SKILL = processor_SC_NOTI_DESTROY_SKILL;
    }

    void init()
    {
        EnterField();

        latency_queue_ = new Queue<Int64>();
    }

    public void HandleInterpolationClick()
    {
        if (is_interpolation)
        {
            is_interpolation = false;
            //interpolation_text.text = "보간 OFF";
            interpolation_text.text = "업데이트 2";
            Debug.Log("보간 OFF");
        }
        else
        {
            is_interpolation = true;
            //interpolation_text.text = "보간 ON";
            interpolation_text.text = "업데이트 1";
            Debug.Log("보간 ON");
        }
    }

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        enemies = new Dictionary<Int64, EnemyTankInfo>();
        bullets = new Dictionary<Int64, BulletInfo>();

        session_ = protobuf_session.getInstance();

        session_.init();
        //session_.connect(IP, PORT); 

        // 접속 연결 / 유실 callback 처리
        session_.scene_connected_callback = OnConnect;
        session_.scene_disconnected_callback = OnDisconnect;

        RegisterProcessorHandler();

        interval_ = 0.0f;
        ping_interval_ = 0.0f;
        update_interval_ = 0.0f;

        if (is_interpolation)
        {
            interpolation_text.text = "업데이트 1";
            //interpolation_text.text = "보간 ON";
        }
        else
        {
            interpolation_text.text = "업데이트 2";
            //interpolation_text.text = "보간 OFF";
        }

        init();
    }

    void FixedUpdate()
    {
        Send_MOVE_OBJECT();

        //Debug.Log("FixedUpdate time :" + Time.deltaTime);

        /*
        if (ping_interval_ >= 1.0f)
        {
            protobuf_session.send_time = session_.getServerTimestamp();
            GAME.CS_PING send = new GAME.CS_PING();
            send.Timestamp = session_.getServerTimestamp();
            session_.send_protobuf(opcode.CS_PING, send);
            ping_interval_ = 0.0f;
        }

        ping_interval_   = ping_interval_   + Time.fixedDeltaTime;
        */
        //update_interval_ = update_interval_ + Time.fixedDeltaTime;

        //Debug.Log(Time.fixedDeltaTime);

    }

    void Update()
    {
        session_.process_packet();

        //UpdateEnemiesTank();
        /*
        // 주인공 업데이트
        if (interval_ >= 200)
        {
            Send_MOVE_OBJECT();
            interval_ = 0.0f;
        }
        else
        {
            var delta = Time.deltaTime;
            interval_ = interval_ + (delta * 1000);
        }
        */

        //
        if (is_interpolation)
        {
            UpdateEnemiesTank();
        }
        else
        {
            UpdateEnemiesTank2();
        }

        //ping_text.text = protobuf_session.ping_time.ToString();
        delta_text.text = Time.deltaTime.ToString();
    }

    void UpdateEnemiesTank()
    {
        foreach (var enemy_info in enemies)
        {
            var enemyTankInfo = enemy_info.Value;

            Int64 now = session_.getServerTimestamp();

            var t2 = enemyTankInfo.snapshots[1].timestamp;
            var t1 = enemyTankInfo.snapshots[0].timestamp;

            var pos2 = enemyTankInfo.snapshots[1].pos;
            var pos1 = enemyTankInfo.snapshots[0].pos;

            var rot2 = enemyTankInfo.snapshots[1].rot;
            var rot1 = enemyTankInfo.snapshots[0].rot;

            var render_time = now - ((t2 - t1) + enemyTankInfo.snapshots[1].latency);

            if (render_time <= t2 && render_time >= t1 && is_interpolation)
            {
                var total = t2 - t1;
                var portion = render_time - t1;
                var ratio = (float)portion / (float)total;

                enemyTankInfo.obj.transform.position = Vector3.Lerp(pos1, pos2, ratio);
                enemyTankInfo.obj.transform.rotation = Quaternion.Slerp(rot1, rot2, ratio);
            }
            else
            {
                enemyTankInfo.obj.transform.position = pos2;
                enemyTankInfo.obj.transform.rotation = rot2;
            }
 
        }
    }

    bool CheckNearBy(Vector3 current_pos, Vector3 to_pos, float radius = 0.1f)
    {

        var dx = current_pos.x - to_pos.x;
        var dz = current_pos.z - to_pos.z;
        var distance = Math.Sqrt(dx * dx + dz * dz);

        if (distance < radius + radius)
        {
            //Debug.Log("근처에 있음\n");
            return true;
        }

        return false;
    }

    void UpdateEnemiesTank2()
    {
        //ping_text.text = Time.deltaTime.ToString();

        foreach (var enemy_info in enemies)
        {
            var enemyTankInfo = enemy_info.Value;

            Int64 now = session_.getServerTimestamp();

            //var t2 = enemyTankInfo.snapshots[1].timestamp;
            //var t1 = enemyTankInfo.snapshots[0].timestamp;

            var pos2 = enemyTankInfo.snapshots[1].pos;
            var pos1 = enemyTankInfo.obj.transform.position;

            var r = CheckNearBy(pos2, pos1);
            if (r) return;

            var dir = enemyTankInfo.snapshots[1].pos - enemyTankInfo.obj.transform.position;
            dir.Normalize();



            enemyTankInfo.obj.transform.forward = Vector3.Slerp(enemyTankInfo.obj.transform.forward, dir, Time.deltaTime * 10.0f);
            //enemyTankInfo.obj.transform.forward = new Vector3(dir.x, 0.0f, dir.z);

            //enemyTankInfo.obj.transform.Translate(dir * 8 * Time.deltaTime);
            enemyTankInfo.obj.transform.position = enemyTankInfo.obj.transform.position + (dir * 8 * Time.deltaTime);

            //Debug.Log("cdx: " + dir.x);
            //Debug.Log("cdz: " + dir.z);
        }
    }



    void Destroy()
    {

    }

    void EnterField()
    {
        GAME.CS_ENTER_FIELD send = new GAME.CS_ENTER_FIELD();
        session_.send_protobuf(opcode.CS_ENTER_FIELD, send);
    }

    void LeaveField()
    {

    }

    void Send_MOVE_OBJECT()
    {
        GameObject player = GameObject.Find("PlayerTank1");

        var pos = player.transform.position;
        var rot = player.transform.rotation;

        //Debug.Log("x: " + pos.x + ", y: " + pos.y + ", z: " + pos.z);

        GAME.CS_MOVE_OBJECT send = new GAME.CS_MOVE_OBJECT();

        send.Timestamp = session_.getServerTimestamp();

        send.PosX = pos.x;
        send.PosY = pos.y;
        send.PosZ = pos.z;

        send.RotX = rot.x;
        send.RotY = rot.y;
        send.RotZ = rot.z;
        send.RotW = rot.w;
        session_.send_protobuf(opcode.CS_MOVE_OBJECT, send);
    }

    void processor_SC_ENTER_FIELD(GAME.SC_ENTER_FIELD read)
    {
        key_ = read.Key;

        GameObject player = GameObject.Find("PlayerTank1");
        player.transform.position = new Vector3(read.PosX, read.PosY, read.PosZ);

        Debug.Log("processor_GAME_SC_ENTER_FIELD 받음");
        foreach (var other in read.UserInfos)
        {
            var pos = new Vector3(other.PosX, other.PosY, other.PosZ);
            Debug.Log(pos);
            var enemyTank = Instantiate(Resources.Load("Prefabs/EnemyTank2")) as GameObject;
            enemyTank.transform.position = pos;

            //Debug.Log("필드에 존재하는 유닛 key: " + other.Key);

            var enemy_tank_info = new EnemyTankInfo();
            enemy_tank_info.obj = enemyTank;


            for (var i = 0; i < max_snapshot_size; ++i)
            {
                enemy_tank_info.snapshots[i].timestamp = session_.getServerTimestamp();
                enemy_tank_info.snapshots[i].pos = pos;
            }

            enemies[other.Key] = enemy_tank_info;

            Debug.Log("count: " + enemies.Count);
        }
    }

    void processor_SC_NOTI_ENTER_FIELD(GAME.SC_NOTI_ENTER_FIELD read)
    {
        Debug.Log("processor_GAME_SC_NOTI_ENTER_FIELD 받음");
        var key = read.Key;
        var pos = new Vector3(read.PosX, read.PosY, read.PosZ);
        Int64 timestamp = read.Timestamp;

        Debug.Log("SC_NOTI_ENTER_FIELD");

        var enemyTank = Instantiate(Resources.Load("Prefabs/EnemyTank2")) as GameObject;
        enemyTank.transform.position = pos;

        //Debug.Log("들어온 친구 key: " + key);
        var enemy_tank_info = new EnemyTankInfo();

        enemy_tank_info.obj = enemyTank;

        for (var i = 0; i < max_snapshot_size; ++i)
        {
            enemy_tank_info.snapshots[i].timestamp = session_.getServerTimestamp();
            enemy_tank_info.snapshots[i].pos = pos;
        }

        enemies[key] = enemy_tank_info;
        Debug.Log("count: " + enemies.Count);
    }

    void processor_SC_NOTI_DESTROY_SKILL(GAME.SC_NOTI_DESTROY_SKILL read)
    {
        Debug.Log("noti destroy skill recevied\n");
        var skill_key = read.SkillKey;
        var target_key = read.TargetKey;
        var damage = read.Damage;

        // 처리해주고
        if (target_key > 0)
        {
            Debug.Log("맞은 타겟: " + target_key);
        }


        HandleDestroyBullet(skill_key);
    }

    void processor_SC_NOTI_MOVE_OBJECT(GAME.SC_NOTI_MOVE_OBJECT read)
    {
        var key = read.Key;
        var pos = new Vector3(read.PosX, read.PosY, read.PosZ);
        var rot = new Quaternion(read.RotX, read.RotY, read.RotZ, read.RotW);
        Int64 timestamp = read.Timestamp;

        //Debug.Log("SC_NOTI_MOVE_OBJECT");
        //Debug.Log("x: " + pos.x + "y: " +  pos.y + "z: " + pos.z);
        //Debug.Log(timestamp);

        var enemyTankInfo = enemies[key];
        if (enemyTankInfo != null)
        {
            enemyTankInfo.snapshots[0].timestamp = enemyTankInfo.snapshots[1].timestamp;
            enemyTankInfo.snapshots[0].pos = enemyTankInfo.snapshots[1].pos;
            enemyTankInfo.snapshots[0].rot = enemyTankInfo.snapshots[1].rot;
            enemyTankInfo.snapshots[0].latency = enemyTankInfo.snapshots[1].latency;

            enemyTankInfo.snapshots[1].timestamp = read.Timestamp;
            enemyTankInfo.snapshots[1].pos = new Vector3(read.PosX, read.PosY, read.PosZ);
            enemyTankInfo.snapshots[1].rot = new Quaternion(read.RotX, read.RotY, read.RotZ, read.RotW);
            enemyTankInfo.snapshots[1].latency = session_.getServerTimestamp() - read.Timestamp;


            /*
            enemyTankInfo.prev_last_info.timestamp = enemyTankInfo.last_info.timestamp;
            enemyTankInfo.prev_last_info.pos       = enemyTankInfo.last_info.pos;
            enemyTankInfo.prev_last_info.rot       = enemyTankInfo.last_info.rot;
            enemyTankInfo.prev_last_info.latency   = enemyTankInfo.last_info.latency;

            enemyTankInfo.last_info.timestamp = read.Timestamp;
            enemyTankInfo.last_info.pos = new Vector3(read.PosX, read.PosY, read.PosZ);
            enemyTankInfo.last_info.rot = new Quaternion(read.RotX, read.RotY, read.RotZ, read.RotW);

            Int64 Now = session_.getServerTimestamp();
            enemyTankInfo.last_info.latency = Now - read.Timestamp;

            if (Now - read.Timestamp <= 0)
            {
                Debug.Log("@@@@@@@@@@@ 말도안됨 @@@@@@@@@@@");
            }

            if (enemyTankInfo.prev_last_info.timestamp == enemyTankInfo.last_info.timestamp)
            {
                Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@");
                Debug.Log("before_last_info.timestamp : " + enemyTankInfo.prev_last_info.timestamp);
    
            }
            */
        }

        /*
        enemy_before_last_info = enemy_last_info;

        enemy_last_info.timestamp = read.Timestamp;
        enemy_last_info.pos = new Vector3(read.X, read.Y, read.Z);
        enemy_last_info.rot = new Quaternion(read.RotX, read.RotY, read.RotZ, read.RotW);
*/
    }

    void processor_SC_NOTI_LEAVE_FIELD(GAME.SC_NOTI_LEAVE_FIELD read)
    {
        Debug.Log("processor_GAME_SC_NOTI_LEAVE_FIELD  받음");
        var key = read.Key;
        Debug.Log("나가는 친구 key: " + key);
        var enemy = enemies[key];
        Destroy(enemy.obj);
        enemies.Remove(key);
    }

    void processor_SC_NOTI_USE_SKILL(GAME.SC_NOTI_USE_SKILL read)
    {
        var key = read.Key;
        var skill_key = read.SkillKey;
        var skill_id = read.SkillId;
        var rot = new Quaternion(read.RotX, read.RotY, read.RotZ, read.RotW);
        var pos = new Vector3(read.PosX, read.PosY, read.PosZ);
        var forward = new Vector3(read.ForwardX, read.ForwardY, read.ForwardZ);
        var distance = read.Distance;
        var speed = read.Speed;

        // 먼저 본인인지 구별
        if (key_ == read.Key)
        {
            Debug.Log("본인 스킬\n");
            GameObject top = GameObject.Find("Top");
            if (top)
            {
                var script = top.GetComponent<Direct_Tank_Topfire>();
                script.Fire(rot, pos, forward, distance, speed);

                var state = script.state;

                pos.y = script.GetBulletStartY();
                var bullet = Instantiate(state.bullet, pos, rot);
                bullet.transform.localScale = new Vector3(bullet.transform.localScale.x * state.bulletSize, bullet.transform.localScale.y * state.bulletSize, bullet.transform.localScale.z * state.bulletSize);
                bullet.GetComponent<DirectBullet>().SetProperty(speed, distance);

                var bullet_info = new BulletInfo();
                bullet_info.bullet = bullet;

                HandleAddBullet(skill_key, bullet_info);
            }
            //var top = player.Find("Top");
            return;
        }

        var enemy_info = enemies[key];

        if (enemy_info != null)
        {
            var e = enemy_info.obj;
            Debug.Log("enemy 존재");
            var top = e.transform.Find("tank_enemy_02_top").gameObject;
            if (top != null)
            {
                top.transform.rotation = rot;

                var script = GameObject.Find("Top").GetComponent<Direct_Tank_Topfire>();
                script.Fire(rot, pos, forward, distance, speed);

                var state = script.state;

                pos.y = top.transform.position.y;

                var bullet = Instantiate(Resources.Load("Prefabs/FireBall")) as GameObject;

                bullet.transform.position = pos;
                bullet.transform.rotation = rot;

                bullet.transform.localScale = new Vector3(bullet.transform.localScale.x * state.bulletSize, bullet.transform.localScale.y * state.bulletSize, bullet.transform.localScale.z * state.bulletSize);
                bullet.GetComponent<DirectBullet>().SetProperty(speed, distance);

                var bullet_info = new BulletInfo();
                bullet_info.bullet = bullet;

                HandleAddBullet(skill_key, bullet_info);
            }
         
        }


        Debug.Log("패킷받음");
    }

    void HandleAddBullet(Int64 key, BulletInfo bullet_info)
    {
        Debug.Log("추가 키: " + key);
        bullets[key] = bullet_info;
    }

    void HandleDestroyBullet(Int64 key)
    {
        Debug.Log("삭제 키: " + key);
        //
        var bullet_info = bullets[key];

        Destroy(bullet_info.bullet);
    }
}
