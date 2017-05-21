using UnityEngine;
using System.Collections;
using System;
using System.IO;
using Google.Protobuf;
using System.Collections.Generic;
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

public class network_module //: MonoBehaviour
{

    private static network_module instance_ = null;

    // check thread safe
    public Queue<MemoryStream> recv_stream_queue = null;

    private static int head_size = sizeof(Int16);

    private byte[] recv_head_buffer_;
    private byte[] recv_body_buffer_;

    private Socket socket_ = null;
    private IPEndPoint end_point_ = null;

    // handler callback
    public delegate void on_connected(bool r);
    public delegate void on_disconnected();
    public delegate void on_recv_payload(MemoryStream payload);

    public on_connected connected_callback;
    public on_disconnected disconnected_callback;

    bool connected_ = false;

    // Use this for initialization
    /*
    public static network_module getInstance()
    {
        if (instance_ == null)
        {
            GameObject network_module = new GameObject("network_module");
            instance_ = network_module.AddComponent<network_module>();
        }
        return instance_;
    }
    */

    public void init()
    {
        recv_stream_queue = new Queue<MemoryStream>();

        recv_head_buffer_ = new byte[2];
        recv_body_buffer_ = new byte[8096];

    }

    public void connect(String host, int port)
    {
        if (connected_) return;

        this.end_point_ = new IPEndPoint(IPAddress.Parse(host), port);
        this.socket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        SocketAsyncEventArgs event_arg = new SocketAsyncEventArgs();
        event_arg.Completed += on_connect;
        event_arg.RemoteEndPoint = this.end_point_;
        //event_arg.UserToken = tcpSession;

        bool pending = socket_.ConnectAsync(event_arg);
        if (!pending)
        {
            on_connect(null, event_arg);
        }
    }

    public void send_packet(MemoryStream packet)
    {
        byte[] write_buffer;
        write_buffer = packet.ToArray();
        int buffer_size = (int)packet.Length;

        socket_.BeginSend(write_buffer, 0, buffer_size, 0,
        new AsyncCallback(on_send), socket_);
    }


    // on_send 가 불리는게 위에 바이트 보내라는 바이트 만큼 다 보낸것이란 보장?
    public void on_send(IAsyncResult ar)
    {
        try
        {
            //Debug.Log("보내기 종료");
            Socket client = (Socket)ar.AsyncState;

            int bytes_sent = client.EndSend(ar);
            //Debug.Log("sent bytes : " + bytes_sent);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            Debug.Log("@@@@@@@@@@@@SEND ERROR@@@@@@@@@@@@@");
        }
    }


    private void on_connect(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            connected_ = true;
            // recv 받기 시작
            do_read_header(socket_);
            connected_callback(true);
        }
        else
        {
            connected_callback(false);
        }
    }

    private void do_read_header(Socket client)
    {
        try
        {
            client.BeginReceive(recv_head_buffer_, 0, head_size, SocketFlags.None,
                new AsyncCallback(on_read_header), socket_);
        }
        catch (Exception e)
        {
            //Console.WriteLine(e.ToString());
            disconnect();
        }
    }

    private void do_read_body(Socket client, Int16 body_size)
    {
        try
        {
            client.BeginReceive(recv_body_buffer_, 0, body_size, 0,
                new AsyncCallback(on_read_body), socket_);
        }
        catch (Exception e)
        {
            // Debug.Log("첫번쨰 몸통 받다가 에러");
            disconnect();
        }
    }

    private void on_read_header(IAsyncResult ar)
    {
        try
        {
            SocketError SE;
            int buflength = socket_.EndReceive(ar, out SE);
            if (SE == SocketError.Success)
            {
                //Debug.Log("대가리 받음");
                Int16 body_size = BitConverter.ToInt16(recv_head_buffer_, 0);
                Int16 head_size = 2;
                body_size = (Int16)(body_size - head_size);
                //Debug.Log("몸통 사이즈: " + body_size);
                do_read_body(socket_, body_size);
            }
            else
            {
                Debug.Log("DISCONNECTION");
                disconnect();
            }

        }
        catch (Exception e)
        {
            //Debug.Log("대가리 받다가 에러");
            disconnect();
        }
    }

    private void on_read_body(IAsyncResult ar)
    {
        try
        {
            SocketError SE;
            int body_size = socket_.EndReceive(ar, out SE);
            if (SE == SocketError.Success)
            {
                //Debug.Log("몸통 받음");
                var packet = new MemoryStream();
                packet.Write(recv_body_buffer_, 0, body_size);
                recv_stream_queue.Enqueue(packet);
                do_read_header(socket_);
            }
            else
            {
                disconnect();
                Debug.Log("DISCONNECTION");
            }
        }
        catch (Exception e)
        {
            Debug.Log("몸통 받다가 에러");
            disconnect();
        }
    }

    public void disconnect()
    {
        connected_ = false;
        if (socket_ != null)
        {
            socket_.Close();
        }
        disconnected_callback();
    }

}
