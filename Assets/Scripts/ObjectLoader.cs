using System;
using UnityEngine;

namespace KotORVR
{
	public class ObjectLoader : MonoBehaviour
	{
		public string resourceRef;
		public ResourceType resourceType;

		// Use this for initialization
		void Start()
		{
			switch (resourceType) {
				case ResourceType.UTC:
					Resources.LoadCharacter(resourceRef);
					break;
				case ResourceType.UTD:
					Resources.LoadDoor(resourceRef);
					break;
				case ResourceType.UTI:
					Resources.LoadItem(resourceRef);
					break;
				case ResourceType.UTP:
					Resources.LoadPlaceable(resourceRef);
					break;
				default:
					Debug.Log("ObjectLoader only works with template resource types (UT*)");
					break;
			}
		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}