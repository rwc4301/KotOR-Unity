using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KotORVR
{
	public class Module
	{
		private RIMObject rim, srim;
		private GFFStruct ifo, are, git;

		public Vector3 entryPosition { get; private set; }
		public Quaternion entryRotation { get; private set; }
		public AudioClip ambientMusic { get; private set; }
		public AudioClip ambientSound { get; private set; }

		public static Module Load(string name)
		{
			Module module = new Module(name);
			Resources.AddModule(module);

			module.LoadCreatures();
			module.LoadDoors();
			module.LoadPlaceables();

			//RenderSettings.skybox

			return module;
		}

		private Module(string name)
		{
			rim = new RIMObject(Resources.ModuleDirectory + "\\" + name + ".rim");
			srim = new RIMObject(Resources.ModuleDirectory + "\\" + name + "_s.rim");

			ifo = new GFFLoader(rim.GetResource("module", ResourceType.IFO)).GetRoot();
			string areaName = ifo["Mod_Entry_Area"].GetValue<string>();

			entryPosition = new Vector3(ifo["Mod_Entry_X"].GetValue<float>(), ifo["Mod_Entry_Z"].GetValue<float>(), ifo["Mod_Entry_Y"].GetValue<float>());

			are = new GFFLoader(rim.GetResource(areaName, ResourceType.ARE)).GetRoot();
			git = new GFFLoader(rim.GetResource(areaName, ResourceType.GIT)).GetRoot();

			Dictionary<string, Vector3> layout = Resources.LoadLayout(areaName);
			foreach (var value in layout) {
				string resref = value.Key.ToLower();

				GameObject room = Resources.LoadModel(resref);
				room.transform.position = value.Value;
			}

			int musicId = git["AreaProperties"]["MusicDay"].GetValue<int>();
			string musicResource = Resources.Load2DA("ambientmusic")[musicId, "resource"];

			ambientMusic = Resources.LoadAudio(musicResource);
		}

		private void LoadCreatures()
		{
			GFFStruct[] creatures = git["Creature List"].GetValue<GFFStruct[]>();
			Character character;
			
			foreach (var c in creatures) {
				Vector3 position = new Vector3(c["XPosition"].GetValue<float>(), c["ZPosition"].GetValue<float>(), c["YPosition"].GetValue<float>());

				//character orientation is stored as a vector2 which describes an angle to rotate around
				float x = c["XOrientation"].GetValue<float>(), y = c["YOrientation"].GetValue<float>();
				float bearing = Mathf.Tan(x / y);
				Quaternion rotation = Quaternion.Euler(0, bearing * Mathf.Rad2Deg * -1, 0);

				character = Resources.LoadCharacter(c["TemplateResRef"].GetValue<string>());
				character.gameObject.transform.position = position;
				character.gameObject.transform.rotation = rotation;
			}
		}

		private void LoadDoors()
		{
			GFFStruct[] doors = git["Door List"].GetValue<GFFStruct[]>();
			Door door;

			foreach (var d in doors) {
				Vector3 position = new Vector3(d["X"].GetValue<float>(), d["Z"].GetValue<float>(), d["Y"].GetValue<float>());

				float bearing = d["Bearing"].GetValue<float>();
				Quaternion rotation = Quaternion.Euler(0, bearing * Mathf.Rad2Deg * -1, 0);

				door = Resources.LoadDoor(d["TemplateResRef"].GetValue<string>());
				door.gameObject.transform.position = position;
				door.gameObject.transform.rotation = rotation;
			}
		}

		private void LoadPlaceables()
		{
			GFFStruct[] placeables = git["Placeable List"].GetValue<GFFStruct[]>();
			Placeable placeable;

			foreach (var p in placeables) {
				Vector3 position = new Vector3(p["X"].GetValue<float>(), p["Z"].GetValue<float>(), p["Y"].GetValue<float>());

				float bearing = p["Bearing"].GetValue<float>();
				Quaternion rotation = Quaternion.Euler(0, bearing * Mathf.Rad2Deg * -1, 0);

				placeable = Resources.LoadPlaceable(p["TemplateResRef"].GetValue<string>());
				placeable.gameObject.transform.position = position;
				placeable.gameObject.transform.rotation = rotation;
			}
		}

		public bool TryGetResource(string resref, ResourceType type, out Stream stream)
		{
			stream = srim.GetResource(resref, type);
			return stream != null;
		}
	}
}