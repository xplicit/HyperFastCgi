//
// Record.cs: Handles FastCGI records.
//
// Author:
//   Sergey Zhukov
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
// Copyright (C) 2013 Sergey Zhukov
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Globalization;
using System.Net.Sockets;

namespace Mono.WebServer.HyperFastCgi.FastCgiProtocol {
	public enum RecordType : byte {
		None            =  0,

		BeginRequest    =  1,

		AbortRequest    =  2,

		EndRequest      =  3,

		Params          =  4,

		StandardInput   =  5,

		StandardOutput  =  6,

		StandardError   =  7,

		Data            =  8,

		GetValues       =  9,

		GetValuesResult = 10,

		UnknownType     = 11
	}

	public struct Record
	{
		public byte Version;

		public RecordType Type;

		public ushort RequestId;

		public ushort BodyLength;

		public byte [] Body;

		public byte PaddingLength;

		public int BodyOffset;


		public const int SuggestedBufferSize = 0x08 + 0xFFFF + 0xFF;



		#region Public Fields

		public const int HeaderSize = 8;

		public const int ProtocolVersion = 1;

		#endregion



		#region Constructors

		public Record (byte version, RecordType type, ushort requestID,
			byte [] bodyData) : this (version, type,
				requestID, bodyData,
				0, -1)
		{
		}

		public Record (byte version, RecordType type, ushort requestID,
			byte [] bodyData, int bodyIndex, int bodyLength)
		{
			if (bodyData == null)
				throw new ArgumentNullException ("bodyData");

			if (bodyIndex < 0 || bodyIndex > bodyData.Length)
				throw new ArgumentOutOfRangeException (
					"bodyIndex");

			if (bodyLength < 0)
				bodyLength = bodyData.Length - bodyIndex;

			if (bodyLength > 0xFFFF)
				throw new ArgumentException (
					Strings.Record_DataTooBig,
					"data");


			this.Version     = version;
			this.Type        = type;
			this.RequestId  = requestID;
			this.Body        = bodyData;
			this.BodyOffset  = bodyIndex;
			this.BodyLength = (ushort) bodyLength;
			this.PaddingLength = 0;
		}

		#endregion

		internal static ushort ReadUInt16 (byte [] array,
			int arrayIndex)
		{
			ushort value = array [arrayIndex];
			value = (ushort) (value << 8);
			value += array [arrayIndex + 1];
			return value;
		}

		#region Public Methods


		public override string ToString ()
		{
			return string.Format (CultureInfo.CurrentCulture,
				Strings.Record_ToString,
				Version, Type, RequestId, BodyLength);
		}

		public byte[] GetRecord()
		{
			byte[] buffer = new byte[HeaderSize + BodyLength + PaddingLength];

			buffer [0] = Version;
			buffer [1] = (byte)Type;
			buffer [2] = (byte)(RequestId >> 8);
			buffer [3] = (byte)(RequestId & 0xff);
			buffer [4] = (byte)(BodyLength >> 8);
			buffer [5] = (byte)(BodyLength & 0xff);
			buffer [6] = PaddingLength;
			Buffer.BlockCopy (this.Body, BodyOffset, buffer, HeaderSize, BodyLength);

			return buffer;
		}


		#endregion

	}
}

