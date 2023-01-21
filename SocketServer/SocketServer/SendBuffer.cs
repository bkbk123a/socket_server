using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core;

public class SendBufferHelper
{
	public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => { return null; });

	public static int ChunkSize { get; set; } = 65535 * 100;
	// reserveSize만큼 예약된 사용중인 ArraySegment 반환 
	public static ArraySegment<byte> Open(int reserveSize)
	{   // CurrentBuffer에 내용없거나 용량 작으면 ChunkSize만큼 생성
		if (CurrentBuffer.Value is null)
        {
			CurrentBuffer.Value = new SendBuffer(ChunkSize);
		}

		if (CurrentBuffer.Value.FreeSize < reserveSize)
        {
			CurrentBuffer.Value = new SendBuffer(ChunkSize);
		}

		return CurrentBuffer.Value.Open(reserveSize);
	}

	public static ArraySegment<byte> Close(int usedSize)
	{
		return CurrentBuffer.Value.Close(usedSize);
	}
}

public class SendBuffer
{
	// [][][][][][][][][u][]
	byte[] _buffer;
	int _usedSize = 0;

	public int FreeSize { get { return _buffer.Length - _usedSize; } }	// 전체 - 사용중인 사이즈

	public SendBuffer(int chunkSize)
	{
		_buffer = new byte[chunkSize];
	}

	public ArraySegment<byte> Open(int reserveSize)
	{
		if (FreeSize < reserveSize)
        {
			return null;
		}

		return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
	}

	public ArraySegment<byte> Close(int usedSize)
	{
		var segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
		_usedSize += usedSize;
		return segment;
	}
}