using UnityEngine;

namespace KotORVR
{
	public class Character : TemplateObject
	{
		public static Character Create(GFFObject gff)
		{
			GameObject gameObject;

			//get the resource reference for this object, which we'll use as it's in-engine name
			string name = gff["TemplateResRef"].GetValue<string>();

			//get the appearance row number in appearance.2da
			int appearance = gff["Appearance_Type"].GetValue<ushort>();

			//get the model name for this appearance id
			string modelRef = Resources.Load2DA("appearance")[appearance, "modela"];
			if (modelRef == null) {
				modelRef = Resources.Load2DA("appearance")[appearance, "race"];
			}
			string texRef = Resources.Load2DA("appearance")[appearance, "texa"];

			//create a new game object and load the model into the scene
			gameObject = Resources.LoadModel(modelRef);
			gameObject.name = name;

			//add the template component to the new object
			Character character = gameObject.AddComponent<Character>();
			character.template = gff;

			return character;
		}
	}
}
