using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Core; 

public class Connector
{
    private Func<Session> _sessionFactory;

    public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _sessionFactory = sessionFactory;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnConnectCompleted;   // 연결 완료되면 방생되는 이벤트
            args.RemoteEndPoint = endPoint;
            args.UserToken = socket;

            RegisterConnect(args);
        }
    }
    // 연결 시도
    private void RegisterConnect(SocketAsyncEventArgs args)
    {
        var socket = args.UserToken as Socket;
        if (socket is null)
        {
            return;
        }

        bool pending = socket.ConnectAsync(args);
        if (!pending) // 연결 시도 하자마자 바로 성공한 경우 
        {
            OnConnectCompleted(null, args);
        }
    }
    // 연결 완료 이벤트
    private void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
        {
            Session session = _sessionFactory.Invoke();
            session.Start(args.ConnectSocket);
            session.OnConnected(args.RemoteEndPoint);
        }
        else
        {
            Console.WriteLine($"OnConnectCompleted Fail: {args.SocketError}");
        }
    }

}
