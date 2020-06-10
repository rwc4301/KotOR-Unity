using UnityEngine;

namespace KotORVR
{
	public class Door : TemplateObject
	{
		private bool isOpen;

		public static Door Create(GFFObject gff)
		{
			GameObject gameObject;

			//get the resource reference for this object, which we'll use as it's in-engine name
			string name = gff["TemplateResRef"].GetValue<string>();

			//get the appearance row number in genericdoors.2da
			int appearance = gff["GenericType"].GetValue<byte>();

			//get the model name for this door id
			string modelRef = Resources.Load2DA("genericdoors")[appearance, "modelname"];
			
			//create a new game object and load the model into the scene
			gameObject = Resources.LoadModel(modelRef);
			gameObject.name = name;

			//add the template component to the new object
			Door door = gameObject.AddComponent<Door>();
			door.template = gff;

			return door;
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
			gameObject.GetComponent<Animation>().Play("opening1");
		}

		public void Close()
		{
			isOpen = false;
			gameObject.GetComponent<Animation>().Play("closing1");
		}
	}
}
