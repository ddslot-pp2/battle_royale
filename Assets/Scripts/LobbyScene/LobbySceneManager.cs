using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using network;
using System;

public class LobbySceneManager : MonoBehaviour {

    private protobuf_session session_;

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

    /*
    public void onClickChangeSceneButton()
    {
        Debug.Log("로드 sample2 씬");
        UnityEngine.SceneManagement.SceneManager.LoadScene("sample2");
    }
    */

    // 여기에 이 씬에서 사용할 패킷 callback을 등록
    void RegisterProcessorHandler()
    {
        session_.processor_SC_LOG_IN = processor_SC_LOG_IN;
    }

    void Start()
    {
        session_ = protobuf_session.getInstance();
        session_.init();
        //session_.connect(IP, PORT); 

        // 접속 연결 / 유실 callback 처리
        session_.scene_connected_callback = OnConnect;
        session_.scene_disconnected_callback = OnDisconnect;

        RegisterProcessorHandler();
    }

    void Update()
    {
        session_.process_packet();

        //Debug.Log("update");
    }

    void Destroy()
    {

    }

    public void onClickSendLoginButton()
    {
        Debug.Log("로그인 패킷 보냄");
        LOBBY.CS_LOG_IN send = new LOBBY.CS_LOG_IN();
        send.Id = "으으앙";
        send.Password = "12345";

        session_.send_protobuf(opcode.CS_LOG_IN, send);
    }

    void processor_SC_LOG_IN(LOBBY.SC_LOG_IN read)
    {
        Debug.Log("패킷 로그인 받음");

        Debug.Log("Result: " + read.Result);
        if (read.Result)
        {
            Debug.Log(read.Timestamp);

            var now = DateTime.Now.ToLocalTime();
            var span = (now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
            var timestamp = (Int64)span.TotalMilliseconds;
            
            session_.delta_timestamp = read.Timestamp - timestamp;
            
            UnityEngine.SceneManagement.SceneManager.LoadScene("Newbie");
        }
    }
}
