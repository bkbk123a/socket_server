using Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
	class GameRoom : IJobQueue
	{
		List<ClientSession> _sessions = new List<ClientSession>();
		JobQueue _jobQueue = new JobQueue();
		List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

		public void Push(Action job)
		{
			_jobQueue.Push(job);
		}
		// 각각의 클라이언트 세션들에 펜딩리스트 전송하고 clear
		public void Flush()
		{
			// N ^ 2
			foreach (ClientSession s in _sessions)
            {
				s.Send(_pendingList);
			}

			Console.WriteLine($"Flushed {_pendingList.Count} items");
			_pendingList.Clear();
		}
		// 펜딩리스트에 채팅 추가
		public void Broadcast(ClientSession session, string chat)
		{
			S_Chat packet = new S_Chat();
			packet.playerId = session.SessionId;
			packet.chat =  $"{chat} I am {packet.playerId}";
			ArraySegment<byte> segment = packet.Write();

			_pendingList.Add(segment);			
		}
		// 방 입장
		public void Enter(ClientSession session)
		{
			_sessions.Add(session);
			session.Room = this;
		}
		// 방 나가기
		public void Leave(ClientSession session)
		{
			_sessions.Remove(session);
		}
	}
}
