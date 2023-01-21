using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core;

public class RecvBuffer
{
	// [r][][w][][][][][][][]
	private ArraySegment<byte> _buffer;
	private int _readPos;
	private int _writePos;

	public RecvBuffer(int bufferSize)
	{
		_buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
	}

	public int DataSize { get { return _writePos - _readPos; } }		// 사용중인 데이터 크기 
	public int FreeSize { get { return _buffer.Count - _writePos; } }   // 빈공간 
																	
	public ArraySegment<byte> ReadSegment                               // _buffer에서 읽을 위치부터 ArraySegment반환(DataSize)
	{
		get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); }
	}
	
	public ArraySegment<byte> WriteSegment                              // _buffer에서 읽고남은 위치부터(write시작위치) ArraySegment반환 (FreeSize)
	{
		get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); }
	}

	public void Clean()
	{
		int dataSize = DataSize;
		if (dataSize == 0)
		{
			// 남은 데이터가 없으면 복사하지 않고 커서 위치만 리셋
			_readPos = _writePos = 0;
		}
		else
		{
			// 남은게 있으면 시작 위치로 복사
			Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
			_readPos = 0;
			_writePos = dataSize;
		}
	}

	public bool OnRead(int numOfBytes)
	{
		if (DataSize < numOfBytes)
        {
			return false;
		}

		_readPos += numOfBytes;
		return true;
	}

	public bool OnWrite(int numOfBytes)
	{
		if (FreeSize < numOfBytes)
        {
			return false;
		}

		_writePos += numOfBytes;
		return true;
	}
}