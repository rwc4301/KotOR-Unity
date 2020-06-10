using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using NAudio;
using NAudio.Wave;

namespace KotORVR
{
	public class WAVObject
	{
		private enum AudioFormat { Wave, MP3 }
		private enum AudioEncoding { PCM = 0x01, ADPCM = 0x11 }

		private AudioFormat format;
		private AudioEncoding encoding;
		public Stream dataStream;
		private long dataOffset;
		private int chunkSize;
		
		public int channels, sampleRate;
		public float[] data;

		public WAVObject(Stream stream)
		{
			byte[] buffer = new byte[4];
			stream.Read(buffer, 0, 4);

			if (buffer[0] == 0xFF && buffer[1] == 0xF3 && buffer[2] == 0x60 && buffer[3] == 0xC4) {   //fake header
				format = AudioFormat.Wave;

				stream.Position = 470;		//skip the junk data
				ReadWaveHeader(stream);
			}
			else if (Encoding.UTF8.GetString(buffer, 0, 4) == "RIFF") {         //riff header
				//ReadWaveHeader(stream);

				stream.Read(buffer, 0, 4);
				uint riffSize = BitConverter.ToUInt32(buffer, 0);
				//this is always 50 when the data is formatted as mp3
				if (riffSize == 50) {		
					format = AudioFormat.MP3;

					stream.Position = 58;
					ReadMp3(stream);
					stream.Position = 0;
				}
				//otherwise, the data is wave formatted (pcm or adpcm)
				else {
					format = AudioFormat.Wave;

					ReadWaveHeader(stream);
				}
			}
			else {
				format = AudioFormat.MP3;

				ReadMp3(stream);
			}
		}

		public Stream GetPCMDataStream()
		{
			return dataStream;
		}

		private void ReadWaveHeader(Stream stream)
		{
			byte[] buffer = new byte[12];
			stream.Read(buffer, 0, 12);

			if (Encoding.UTF8.GetString(buffer, 0, 4) != "RIFF") {
				Debug.LogWarning("Invalid riff header");
				return;
			}

			uint riffSize = BitConverter.ToUInt32(buffer, 4);

			if (Encoding.UTF8.GetString(buffer, 8, 4) != "WAVE") {
				Debug.LogWarning("Invalid wave header");
				return;
			}

			bool readChunks = false;
			string chunk;
			while (!readChunks) {
				stream.Read(buffer, 0, 4);
				chunk = Encoding.UTF8.GetString(buffer, 0, 4);

				if (chunk == "fmt ") {
					buffer = new byte[18];
					stream.Read(buffer, 0, 18);

					chunkSize = (int)BitConverter.ToUInt32(buffer, 0);
					encoding = (AudioEncoding)BitConverter.ToUInt16(buffer, 4);
					channels = BitConverter.ToUInt16(buffer, 6);
					sampleRate = BitConverter.ToInt32(buffer, 8);
					uint bytesPerSec = BitConverter.ToUInt32(buffer, 12);
					ushort frameSize = BitConverter.ToUInt16(buffer, 14);
					ushort bits = BitConverter.ToUInt16(buffer, 16);

					if (encoding == AudioEncoding.ADPCM) {
						stream.Read(buffer, 0, 2);
						ushort blobSize = BitConverter.ToUInt16(buffer, 0);

						byte[] blobData = new byte[blobSize];
						stream.Read(blobData, 0, blobSize);
					}
				}
				else if (chunk == "fact") {
					stream.Read(buffer, 0, 8);
					uint factSize = BitConverter.ToUInt32(buffer, 0);
					uint factBOH = BitConverter.ToUInt32(buffer, 4);
				}
				else if (chunk == "data") {
					stream.Read(buffer, 0, 4);
					uint dataSize = BitConverter.ToUInt32(buffer, 0);
					dataOffset = stream.Position;
				}
				else {
					readChunks = true;
				}
			}

			dataStream = ReadDataStream(stream);
		}

		private void ReadMp3(Stream stream)
		{
			byte[] buffer = new byte[stream.Length - stream.Position];
			stream.Read(buffer, 0, buffer.Length);

			WaveStream mp3 = new Mp3FileReader(new MemoryStream(buffer));
			WaveStream wav = WaveFormatConversionStream.CreatePcmStream(mp3);

			//WaveFileWriter.CreateWaveFile("D:\\streammusic\\out2.wav", wav);

			channels = wav.WaveFormat.Channels;
			sampleRate = wav.WaveFormat.SampleRate;
			chunkSize = wav.WaveFormat.BitsPerSample;

			ReadDataStream(wav);
			//dataStream = wav;

			//data = new float[buffer.Length / 2];
			//for (int i = 0; i < data.Length; i++) {
			//	data[i] = (float)BitConverter.ToInt16(buffer, i * 2) / short.MaxValue;
			//}
		}

		private Stream ReadDataStream(Stream stream)
		{
			stream.Position = dataOffset;

			byte[] buffer;
			//if (format == AudioFormat.Wave && encoding == AudioEncoding.ADPCM) {
			//	stream.Position = 60;

			//	buffer = new byte[stream.Length - stream.Position];
			//	stream.Read(buffer, 0, buffer.Length);

			//	//decompile
			//	UnityEngine.Debug.LogWarning("Audio stream is ADPCM");
			//	return null;
			//}

			buffer = new byte[stream.Length - stream.Position];
			stream.Read(buffer, 0, buffer.Length);

			int bytesPerSample = chunkSize / 8;

			data = new float[buffer.Length / bytesPerSample];
			for (int i = 0; i < data.Length; i++) {
				data[i] = (float)BitConverter.ToInt16(buffer, i * bytesPerSample) / short.MaxValue;
			}

			return new MemoryStream(buffer);
		}
	}
}
