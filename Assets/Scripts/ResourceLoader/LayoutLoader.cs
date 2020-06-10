using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KotORVR
{
	public static partial class Resources
	{
		public static Dictionary<string, Vector3> LoadLayout(string resref)
		{
			StreamReader reader = new StreamReader(GetStream(resref, ResourceType.LYT));

			Dictionary<string, Vector3> roomVectors = new Dictionary<string, Vector3>();
			bool doingLayout = false;
			int parseType = 0;
			string line;

			while(!reader.EndOfStream) {
				line = reader.ReadLine();

				if (line.Contains("beginlayout")) {
					doingLayout = true;
				} else if (line.Contains("donelayout")) {
					doingLayout = false;
				}

				if (doingLayout) {
					if (line.Contains("roomcount")) {
						parseType = 1;
						continue;
					}
					else if (line.Contains("trackcount")) {
						parseType = 2;
						continue;
					}
					else if (line.Contains("obstaclecount")) {
						parseType = 3;
						continue;
					}
					else if (line.Contains("doorhookcount")) {
						parseType = 4;
						continue;
					}

					switch (parseType) {
						case 1:     //rooms
							string[] arr = line.Trim().Split(' ');
							if (roomVectors != null) {
								roomVectors.Add(arr[0], new Vector3(float.Parse(arr[1]), float.Parse(arr[3]), float.Parse(arr[2])));
							}
							break;
						default:    //TODO: tracks, obstacles, door hooks
							break;
					}
				}
			}

			return roomVectors;
		}
	}
}