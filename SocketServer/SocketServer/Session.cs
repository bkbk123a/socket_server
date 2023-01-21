using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Core;

public abstract class PacketSession : Session
{
	public static readonly int HeaderSize = 2;

	// [size(2)][packetId(2)][ ... ][size(2)][packetId(2)][ ... ]
	public sealed override int OnRecv(ArraySegment<byte> buffer)
	{
		int processLen = 0;
		int packetCount = 0;

		while (true)
		{
			// 최소한 헤더는 파싱할 수 있는지 확인
			if (buffer.Count < HeaderSize)
            {
				break;
			}

			// 패킷이 완전체로 도착했는지 확인
			ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
			if (buffer.Count < dataSize)
            {
				break;
			}

			// 여기까지 왔으면 패킷 조립 가능
			OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));
			packetCount++;

			processLen += dataSize;
			buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
		}

		if (packetCount > 1)
        {
			Console.WriteLine($"패킷 모아보내기 : {packetCount}");
		}

		return processLen;
	}

	public abstract void OnRecvPacket(ArraySegment<byte> buffer);
}

public abstract class Session
{
	private Socket _socket;
	private int _disconnected = 0;  // Interlocked 함수 사용으로 bool아님

	private RecvBuffer _recvBuffer = new RecvBuffer(65535);

	private object _lock = new object();
	private Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();//보낼 작업
	private List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();//보낼 내용
	private SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
	private SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

	public abstract void OnConnected(EndPoint endPoint);
	public abstract int OnRecv(ArraySegment<byte> buffer);
	public abstract void OnSend(int numOfBytes);
	public abstract void OnDisconnected(EndPoint endPoint);

	void Clear()
	{
		lock (_lock)
		{
			_sendQueue.Clear();
			_pendingList.Clear();
		}
	}

	public void Start(Socket socket)
	{
		_socket = socket;

		_recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
		_sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

		RegisterRecv();
	}

	public void Send(List<ArraySegment<byte>> sendBuffList)
	{
		if (sendBuffList.Count <= 0)
        {
			return;
        }

		lock (_lock)
		{	// sendBuff내용을 sendQueue에  push
			foreach (var sendBuff in sendBuffList)
            {
				_sendQueue.Enqueue(sendBuff);
			}

			if (_pendingList.Count <= 0)
            {
				RegisterSend();
			}
		}
	}

	public void Send(ArraySegment<byte> sendBuff)
	{
		lock (_lock)
		{
			_sendQueue.Enqueue(sendBuff);
			if (_pendingList.Count == 0)
            {
				RegisterSend();
			}
		}
	}

	public void Disconnect()
	{
		if (Interlocked.Exchange(ref _disconnected, 1) == 1)
        {
			return;
		}

		OnDisconnected(_socket.RemoteEndPoint);
		_socket.Shutdown(SocketShutdown.Both);
		_socket.Close();
		Clear();
	}


	#region 네트워크 통신

	void RegisterSend()
	{
		if (_disconnected == 1)
        {
			return;
		}
		// sendQueue에 있는것을 _pendingList에 담는다.
		while (_sendQueue.Count > 0)
		{
			ArraySegment<byte> buff = _sendQueue.Dequeue();
			_pendingList.Add(buff);
		}
		_sendArgs.BufferList = _pendingList;// 실제로 전송할 내용

		try
		{
			bool pending = _socket.SendAsync(_sendArgs);
			if (!pending)
            {
				OnSendCompleted(null, _sendArgs); // 전송 성공
			}
		}
		catch (Exception e)
		{
			Console.WriteLine($"RegisterSend Failed {e}");
		}
	}

	void OnSendCompleted(object sender, SocketAsyncEventArgs args)
	{
		lock (_lock)
		{
			if ((0 < args.BytesTransferred) && (args.SocketError == SocketError.Success))
			{
				try
				{
					_sendArgs.BufferList = null;// 보낼 버퍼리스트 null
					_pendingList.Clear();// 전송한 sendingList clear

					OnSend(_sendArgs.BytesTransferred);	// 내가 send완료되고 추후에 정의할 내용

					if (0 < _sendQueue.Count)
                    {
						RegisterSend();
					}
				}
				catch (Exception e)
				{
					Console.WriteLine($"OnSendCompleted Failed {e}");
				}
			}
			else
			{
				Disconnect();
			}
		}
	}

	void RegisterRecv()
	{
		if (_disconnected == 1)
        {
			return;
		}

		_recvBuffer.Clean();
		ArraySegment<byte> segment = _recvBuffer.WriteSegment;
		_recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

		try
		{
			bool pending = _socket.ReceiveAsync(_recvArgs);
			if (!pending)
            {
				OnRecvCompleted(null, _recvArgs);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine($"RegisterRecv Failed {e}");
		}
	}

	void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
	{
		if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
		{
			try
			{
				// Write 커서 이동
				if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
				{
					Disconnect();
					return;
				}

				// 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
				int processLen = OnRecv(_recvBuffer.ReadSegment);
				if (processLen < 0 || _recvBuffer.DataSize < processLen)
				{
					Disconnect();
					return;
				}

				// Read 커서 이동
				if (_recvBuffer.OnRead(processLen) == false)
				{
					Disconnect();
					return;
				}

				RegisterRecv();
			}
			catch (Exception e)
			{
				Console.WriteLine($"OnRecvCompleted Failed {e}");
			}
		}
		else
		{
			Disconnect();
		}
	}

	#endregion
}

