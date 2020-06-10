using System;
using System.IO;
using UnityEngine;

namespace KotORVR
{
	public class TPCObject
	{
		public enum PixelFormat {
			R8G8B8 = 1,
			B8G8R8 = 2,
			R8G8B8A8 = 3,
			B8G8R8A8 = 4,
			A1R5G5B5 = 5,
			R5G6B5 = 6,
			Depth16 = 7,
			DXT1 = 8,
			DXT3 = 9,
			DXT5 = 10
		}

		public enum Encoding {
			Gray = 1,
			RGB = 2,
			RGBA = 4,
			BGRA = 12
		}

		public class MipMap
		{
			public int width, height;
			public long size;
			public byte[] data;
		}

		private const int HEADER_SIZE = 128;

		private int _layerCount = 1;	//for layered 3D images and cubemaps, not implemented!
		private Encoding encoding;
		private MipMap[] mipMaps;

		public string envMapTexture { get; private set; }
		public TextureFormat Format { get; private set; }

		public int Width {
			get {
				return mipMaps.Length > 0 ? mipMaps[0].width : 0;
			}
		}

		public int Height {
			get {
				return mipMaps.Length > 0 ? mipMaps[0].height : 0;
			}
		}

		public byte[] RawData {
			get {
				return mipMaps.Length > 0 ? mipMaps[0].data : null;
			}
		}

		public TPCObject(Stream stream)
		{
			using (stream) {
				readHeader(stream);
				readData(stream);
			}
		}

		private void readHeader(Stream stream) {
			byte[] buffer = new byte[HEADER_SIZE];
			stream.Read(buffer, 0, HEADER_SIZE);

			long dataSize = BitConverter.ToUInt32(buffer, 0);
			bool compressed = dataSize != 0;

			float alphaTest = BitConverter.ToSingle(buffer, 4);

			int width = BitConverter.ToUInt16(buffer, 8),
				height = BitConverter.ToUInt16(buffer, 10);

			encoding = (Encoding)buffer[12];
			//bool hasAlpha = ((int)encoding & 4) == 4;

			int mipMapCount = Math.Max(1, (int)buffer[13]);

			int minDataSize;
			switch (encoding) {
				case Encoding.Gray:
					Format = TextureFormat.RGB24;
					minDataSize = 1;
					dataSize = width * height * minDataSize;
					break;
				case Encoding.RGB:
					if (compressed) {
						Format = TextureFormat.DXT1;
						minDataSize = 8;
					}
					else {
						Format = TextureFormat.RGB24;
						minDataSize = 3;
						dataSize = width * height * minDataSize;
					}
					break;
				case Encoding.RGBA:
					if (compressed) {
						Format = TextureFormat.DXT5;
						minDataSize = 16;
					}
					else {
						Format = TextureFormat.RGBA32;
						minDataSize = 4;
						dataSize = width * height * minDataSize;
					}
					break;
				case Encoding.BGRA:
					Format = TextureFormat.BGRA32;
					minDataSize = 4;
					dataSize = width * height * minDataSize;
					break;
				default:
					Format = TextureFormat.RGB24;
					minDataSize = 0;
					break;
			}

			// Calculate the complete data size for images with mipmaps
			long completeDataSize = dataSize;
			int w = width, h = height;
			for (int i = 1; i < mipMapCount; ++i) {
				w = Math.Max(w >> 1, 1);
				h = Math.Max(h >> 1, 1);

				completeDataSize += getDataSize(Format, w, h);
			}
			completeDataSize *= _layerCount;

			stream.Position += completeDataSize;
			readTXIData(stream);

			bool isAnimated = false;
			//checkAnimated(width, height, dataSize);

			stream.Position = HEADER_SIZE;

			long fullImageSize = getDataSize(Format, width, height);
			long combinedSize = 0;

			mipMaps = new MipMap[mipMapCount];
			for (int l = 0; l < _layerCount; l++) {
				int layerWidth = width;
				int layerHeight = height;
				long layerSize = (isAnimated) ? getDataSize(Format, layerWidth, layerHeight) : dataSize;

				for (int i = 0; i < mipMapCount; i++) {
					mipMaps[i] = new MipMap();

					mipMaps[i].width = Math.Max(layerWidth, 1);
					mipMaps[i].height = Math.Max(layerHeight, 1);

					mipMaps[i].size = Math.Max(layerSize, minDataSize);

					long mipMapDataSize = getDataSize(Format, mipMaps[i].width, mipMaps[i].height);

					combinedSize += mipMaps[i].size;

					layerWidth >>= 1;
					layerHeight >>= 1;
					layerSize = getDataSize(Format, layerWidth, layerHeight);

					if ((layerWidth < 1) && (layerHeight < 1))
						break;
				}
			}
		}

		private void readData(Stream stream)
		{
			for (int i = 0; i < mipMaps.Length; i++) {
				// If the texture width is a power of two, the texture memory layout is "swizzled"
				bool widthPOT = mipMaps[i].width % 2 == 0;
				bool swizzled = (encoding == Encoding.BGRA) && widthPOT;

				// Unpacking 8bpp grayscale data into RGB
				if (encoding == Encoding.Gray) {
					byte[] dataGray = mipMaps[i].data;

					mipMaps[i].size = mipMaps[i].width * mipMaps[i].height * 3;
					mipMaps[i].data = new byte[mipMaps[i].size];

					for (int greyIndex = 0, rgbIndex = 0; greyIndex < dataGray.Length; greyIndex++, rgbIndex += 3) {
						mipMaps[i].data[rgbIndex + 0] = mipMaps[i].data[rgbIndex + 1] = mipMaps[i].data[rgbIndex + 2] = dataGray[greyIndex];
					}
				}
				else {
					mipMaps[i].data = new byte[mipMaps[i].size];
					stream.Read(mipMaps[i].data, 0, (int)mipMaps[i].size);
				}
			}
		}

		private byte[] deSwizzle(byte[] src, int width, int height) {
			//for (int y = 0; y < height; y++) {
			//	for (int x = 0; x < width; x++) {
			//		int offset = deSwizzleOffset(x, y, width, height) * 4;

			//		*dst++ = src[offset + 0];
			//		*dst++ = src[offset + 1];
			//		*dst++ = src[offset + 2];
			//		*dst++ = src[offset + 3];
			//	}
			//}
			return src;
		}

		/** Return the number of bytes necessary to hold an image of these dimensions
		* and in this format. */
		private long getDataSize(TextureFormat format, int width, int height)
		{
			if ((width < 0) || (width >= 0x8000) || (height < 0) || (height >= 0x8000))
				throw new Exception("Invalid dimensions " + width + "x" + height);

			switch (format) {
				case TextureFormat.RGB24:
					return width * height * 3;
				case TextureFormat.RGBA32:
				case TextureFormat.BGRA32:
					return width * height * 4;
				case TextureFormat.DXT1:
					return Math.Max(8, ((width + 3) / 4) * ((height + 3) / 4) * 8);
				case TextureFormat.DXT5:
					return Math.Max(16, ((width + 3) / 4) * ((height + 3) / 4) * 16);
				default:
					break;
			}

			throw new Exception("Invalid pixel format " + format);
		}

		private void readTXIData(Stream stream)
		{
			byte[] buffer = new byte[stream.Length - stream.Position];
			stream.Read(buffer, 0, buffer.Length);

			string[] txiData = System.Text.Encoding.UTF8.GetString(buffer).Split('\x20');

			for (int i = 0; i < txiData.Length; i++) {
				if (txiData[i] == "envmaptexture") {
					i++;
					envMapTexture = txiData[i];
				}
			}
		}
	}
}
