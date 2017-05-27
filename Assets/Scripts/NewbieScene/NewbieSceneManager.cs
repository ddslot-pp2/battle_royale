using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using network;
using System;
using UnityEngine.UI;

public class NewbieSceneManager : MonoBehaviour {

    private protobuf_session session_;

    private bool is_loading = true;

    class MoveInfo
    {
        public Vector3 pos = new Vector3(0.0f, 0.0f, 0.0f);
        public Quaternion rot = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        public Int64 timestamp;
    }

    class EnemyTankInfo
    {
        public GameObject obj;
        public MoveInfo before_last_info = new MoveInfo();
        public MoveInfo last_info = new MoveInfo();
    }

    Dictionary<Int64, EnemyTankInfo> enemies = null;

    float interval_;
    public Vector3 pos_;
    public Quaternion rot_;

    private float ping_interval_;
    private float update_interval_;

    public Text ping_text;

    public void OnConnect(bool result)
    {
        if (result)
        {
            Debug.Log("접속 성공");
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
        session_.processor_SC_ENTER_FIELD = processor_SC_ENTER_FIELD;
        session_.processor_SC_NOTI_ENTER_FIELD = processor_SC_NOTI_ENTER_FIELD;
        session_.processor_SC_NOTI_MOVE_OBJECT = processor_SC_NOTI_MOVE_OBJECT;
        session_.processor_SC_NOTI_LEAVE_FIELD = processor_SC_NOTI_LEAVE_FIELD;
    }

    void init()
    {
        EnterField();
    }

    void Start()
    {
        enemies = new Dictionary<Int64, EnemyTankInfo>();

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
        init();
    }

    void FixedUpdate()
    {
        //Debug.Log("FIXED DELTA: " + Time.deltaTime);
        session_.process_packet();

        if (update_interval_ >= 0.25)
        {
            Send_MOVE_OBJECT();
            update_interval_ = 0.0f;
        }
        
        //Debug.Log("FixedUpdate time :" + Time.deltaTime);
       
        if (ping_interval_ > 1)
        {
            ping_text.text = protobuf_session.ping_time.ToString();

            protobuf_session.send_time = session_.getServerTimestamp();
            GAME.CS_PING send = new GAME.CS_PING();
            send.Timestamp = session_.getServerTimestamp();
            session_.send_protobuf(opcode.CS_PING, send);
            ping_interval_ = 0.0f;
        }

        ping_interval_   = ping_interval_   + Time.deltaTime;
        update_interval_ = update_interval_ + Time.deltaTime;


        //UpdateEnemiesTank();
    }

    void Update()
    {
        UpdateEnemiesTank();
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
    }

    void UpdateEnemiesTank()
    {
        foreach (var enemy_info in enemies)
        {
            var enemyTankInfo = enemy_info.Value;

            Int64 Now = session_.getServerTimestamp();

            var Past = enemyTankInfo.last_info.timestamp - enemyTankInfo.before_last_info.timestamp;

            //Debug.Log("Past: " + Past);  
            // 랜더타임 잘못됨
            var renderTime = (Now - Past);

            var t1 = enemyTankInfo.before_last_info.timestamp;
            var t2 = enemyTankInfo.last_info.timestamp;

            /*
            Debug.Log("now: " + Now);
            Debug.Log("render Time: " + renderTime);
            Debug.Log("t2: " + t2);
            Debug.Log("t1: " + t1);
            */

            bool is_interpolation = true;
            if (renderTime <= t2 && renderTime >= t1 && is_interpolation)
            {
                // 서버에서 패킷이 올때까지 걸린시간
                var total = t2 - t1;

                // 드로우콜하는 현재 렌더시간이랑 마지막으로 패킷을 받은 시간의 차이
                var portion = renderTime - t1;

                // 보간량을 측정하는 방법
                var ratio = (float)portion / (float)total;

                //Debug.Log("ratio: " + ratio);

                enemyTankInfo.obj.transform.position = Vector3.Lerp(enemyTankInfo.before_last_info.pos, enemyTankInfo.last_info.pos, ratio);
                enemyTankInfo.obj.transform.rotation = Quaternion.Slerp(enemyTankInfo.before_last_info.rot, enemyTankInfo.last_info.rot, ratio);
            }
            else
            {
                Debug.Log("------------ LAG -----------");
                Debug.Log("N: " + Now);
                Debug.Log("R: " + renderTime);
                Debug.Log("2: " + t2);
                /*
                Debug.Log("-----------------------------------");
                Debug.Log("DT: " + Time.deltaTime);
                Debug.Log("PING: " + protobuf_session.ping_time);
                Debug.Log("N: " + Now);
                Debug.Log("R: " + renderTime);
                Debug.Log("2: " + t2);
                Debug.Log("1: " + t1);
                */

                enemyTankInfo.obj.transform.position = enemyTankInfo.last_info.pos;
                enemyTankInfo.obj.transform.rotation = enemyTankInfo.last_info.rot;
            }


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

        GAME.CS_MOVE_OBJECT send = new GAME.CS_MOVE_OBJECT();

        send.PosX = pos.x;
        send.PosY = pos.y;
        send.PosZ = pos.z;

        send.RotX = rot.x;
        send.RotY = rot.y;
        send.RotZ = rot.z;
        send.RotW = rot.w;

        send.Timestamp = session_.getServerTimestamp();

        session_.send_protobuf(opcode.CS_MOVE_OBJECT, send);
    }

    void processor_SC_ENTER_FIELD(GAME.SC_ENTER_FIELD read)
    {
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

            enemy_tank_info.before_last_info.timestamp = session_.getServerTimestamp();
            enemy_tank_info.before_last_info.pos = pos;

            enemy_tank_info.last_info.timestamp = session_.getServerTimestamp();
            enemy_tank_info.last_info.pos = pos;

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

        enemy_tank_info.before_last_info.timestamp = session_.getServerTimestamp();
        enemy_tank_info.before_last_info.pos = pos;

        enemy_tank_info.last_info.timestamp = session_.getServerTimestamp();
        enemy_tank_info.last_info.pos = pos;

        enemies[key] = enemy_tank_info;
        Debug.Log("count: " + enemies.Count);
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
            enemyTankInfo.before_last_info.timestamp = enemyTankInfo.last_info.timestamp;
            //Debug.Log("before_last_info.timestamp : " + enemyTankInfo.before_last_info.timestamp);
            enemyTankInfo.before_last_info.pos = enemyTankInfo.last_info.pos;
            enemyTankInfo.before_last_info.rot = enemyTankInfo.last_info.rot;

            Int64 Now = session_.getServerTimestamp();

            //Debug.Log("client now: " + Now);
            //Debug.Log("read timestamp: " + read.Timestamp);
            //Debug.Log("diff time: " + protobuf_session.delta_timestamp_);

            enemyTankInfo.last_info.timestamp = read.Timestamp;
            enemyTankInfo.last_info.pos = new Vector3(read.PosX, read.PosY, read.PosZ);
            enemyTankInfo.last_info.rot = new Quaternion(read.RotX, read.RotY, read.RotZ, read.RotW);

            if (enemyTankInfo.before_last_info.timestamp == enemyTankInfo.last_info.timestamp)
            {
                Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@");
                Debug.Log("before_last_info.timestamp : " + enemyTankInfo.before_last_info.timestamp);
    
            }
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
}
