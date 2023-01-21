using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Core;

public class Listener
{
	Socket _listenSocket;
	Func<Session> _sessionFactory;

	public void Init(IPEndPoint endPoint, Func<Session> sessionFactory, int register = 10, int backlog = 100)
	{
		_listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		_sessionFactory += sessionFactory;

		// 문지기 교육
		_listenSocket.Bind(endPoint);

		// 영업 시작
		// backlog : 최대 대기수
		_listenSocket.Listen(backlog);

		for (int i = 0; i < register; i++)
		{
			SocketAsyncEventArgs args = new SocketAsyncEventArgs();
#pragma warning disable CS8622 // 매개 변수 형식에서 참조 형식의 Null 허용 여부가 대상 대리자와 일치하지 않습니다(Null 허용 여부 특성 때문일 수 있음).
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
#pragma warning restore CS8622 // 매개 변수 형식에서 참조 형식의 Null 허용 여부가 대상 대리자와 일치하지 않습니다(Null 허용 여부 특성 때문일 수 있음).
            RegisterAccept(args);
		}
	}

	void RegisterAccept(SocketAsyncEventArgs args)
	{
		args.AcceptSocket = null;

		bool pending = _listenSocket.AcceptAsync(args);
		if (pending == false)
			OnAcceptCompleted(null, args);
	}

	void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
	{
		if (args.SocketError == SocketError.Success)
		{
			Session session = _sessionFactory.Invoke();
			session.Start(args.AcceptSocket);
			session.OnConnected(args.AcceptSocket.RemoteEndPoint);
		}
		else
			Console.WriteLine(args.SocketError.ToString());

		RegisterAccept(args);
	}
}