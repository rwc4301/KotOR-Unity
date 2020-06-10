using UnityEngine;

namespace KotORVR
{
	public class GameManager : MonoBehaviour
	{
		public string kotorDir = "D:\\Program Files\\Star Wars - KotOR";
		public Game targetGame = Game.KotOR;

		public string entryModule = "ebo_m12aa";

		public void Awake()
		{
			Resources.Init(kotorDir, targetGame);
		}

		public void Start()
		{
			Module mod = Module.Load(entryModule);

			GameObject.FindGameObjectWithTag("Player").transform.position = mod.entryPosition;

			AudioSource source = GetComponent<AudioSource>();
			if (mod.ambientMusic) {
				source.clip = mod.ambientMusic;
				source.Play();
			}
		}
	}
}
