// --------------------------------------------------------------------------------------------------------------------
// <copyright file="YUV.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The utilities class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#pragma warning disable
namespace Sample.HueBot
{
    using System.Runtime.InteropServices;
    using Microsoft.Skype.Bots.Media;
    using Sample.HueBot.Bot;

	public class YUV
	{
		public readonly byte[] Buffer;
		public readonly int Width;
		public readonly int Height;
		public readonly int Stride;
		public readonly YBuffer Y;
		public readonly UBuffer U;
		public readonly VBuffer V;

		public YUV(int width, int height, int stride)
			: this(new byte[stride * height * 3 / 2], width, height, stride)
		{ }

		public YUV(byte[] buffer, int width, int height, int stride)
		{
			Buffer = buffer;
			Width = width;
			Height = height;
			Stride = stride;

			Y = new YBuffer(this);
			U = new UBuffer(this);
			V = new VBuffer(this);
		}

		public class YBuffer
		{
			private readonly YUV _yuv;

			public YBuffer(YUV yuv)
				=> _yuv = yuv;

			private int Index(int x, int y)
				=> y * _yuv.Stride + x;

			public byte this[int x, int y]
			{
				get => _yuv.Buffer[Index(x, y)];
				set => _yuv.Buffer[Index(x, y)] = value;
			}
		}

		public class UBuffer
		{
			private readonly YUV _yuv;
			private readonly int _start;

			public UBuffer(YUV yuv)
			{
				_yuv = yuv;
				_start = yuv.Stride * yuv.Height;
			}

			private int Index(int x, int y)
				=> _start + (y / 2 * _yuv.Stride / 2 + x / 2) * 2;

			public byte this[int x, int y]
			{
				get => _yuv.Buffer[Index(x, y)];
				set => _yuv.Buffer[Index(x, y)] = value;
			}
		}

		public class VBuffer
		{
			private readonly YUV _yuv;
			private readonly int _start;

			public VBuffer(YUV yuv)
			{
				_yuv = yuv;
				_start = yuv.Stride * yuv.Height;
			}

			private int Index(int x, int y)
				=> _start + (y / 2 * _yuv.Stride / 2 + x / 2) * 2 + 1;

			public byte this[int x, int y]
			{
				get => _yuv.Buffer[Index(x, y)];
				set => _yuv.Buffer[Index(x, y)] = value;
			}
		}
	}
}
