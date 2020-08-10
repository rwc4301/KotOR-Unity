using System.IO;
using UnityEngine;

namespace KotORVR
{
	public class Placeable : TemplateObject
	{
		private bool isOpen;

		public static Placeable Create(GFFStruct templateRoot)
		{
			GameObject gameObject;

			//get the resource reference for this object, which we'll use as it's in-engine name
			string name = templateRoot["TemplateResRef"].GetValue<string>();

			//get the appearance row number in placeables.2da
			int appearance = (int)templateRoot["Appearance"].GetValue<uint>();

			//get the model name for this appearance id
			string modelRef = Resources.Load2DA("placeables")[appearance, "modelname"];
			if (modelRef == "PLC_Invis") {
				gameObject = new GameObject(name);
			}
			else {
				gameObject = Resources.LoadModel(modelRef);
				gameObject.name = name;
			}

			//add the template component to the new object
			Placeable placeable = gameObject.AddComponent<Placeable>();
			placeable.templateRoot = templateRoot;

			return placeable;
		}

		protected override void Update()
		{
			if (!isOpen && Vector3.Distance(transform.position, GameObject.FindGameObjectWithTag("Player").transform.position) < 5) {
				Open();
			}
			else if (isOpen && Vector3.Distance(transform.position, GameObject.FindGameObjectWithTag("Player").transform.position) > 5) {
				Close();
			}
		}

		public void Open()
		{
			isOpen = true;
			gameObject.GetComponent<Animation>().Play("close2open");
		}

		public void Close()
		{
			isOpen = false;
			gameObject.GetComponent<Animation>().Play("open2close");
		}
	}
}
