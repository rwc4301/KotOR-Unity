using UnityEngine;

namespace KotORVR
{
	public class Character : TemplateObject
	{
		[SerializeField] private string[] inventory;

		public short HitPoints {
			get { return templateRoot["HitPoints"].GetValue<short>(); }
			set { templateRoot["HitPoints"].SetValue<short>(value); }
		}

		public short CurrentHitPoints {
			get { return templateRoot["CurrentHitPoints"].GetValue<short>(); }
			set { templateRoot["CurrentHitPoints"].SetValue<short>(value); }
		}

		public short MaxHitPoints {
			get { return templateRoot["MaxHitPoints"].GetValue<short>(); }
			set { templateRoot["MaxHitPoints"].SetValue<short>(value); }
		}

		public short ForcePoints {
			get { return templateRoot["ForcePoints"].GetValue<short>(); }
			set { templateRoot["ForcePoints"].SetValue<short>(value); }
		}

		public short CurrentForce {
			get { return templateRoot["CurrentForce"].GetValue<short>(); }
			set { templateRoot["CurrentForce"].SetValue<short>(value); }
		}

		public string[] Inventory {
			get { return inventory; }
		}

		public static Character Create(GFFStruct templateRoot)
		{
			GameObject gameObject;

			//get the resource reference for this object, which we'll use as it's in-engine name
			string name = templateRoot["TemplateResRef"].GetValue<string>();

			//get the appearance row number in appearance.2da
			int appearance = templateRoot["Appearance_Type"].GetValue<ushort>();

			//get the model name for this appearance id
			string modelRef = Resources.Load2DA("appearance")[appearance, "modela"];
			if (modelRef == null) {
				modelRef = Resources.Load2DA("appearance")[appearance, "race"];
			}
			string texRef = Resources.Load2DA("appearance")[appearance, "texa"];

			//create a new game object and load the model into the scene
			gameObject = Resources.LoadModel(modelRef);
			gameObject.name = name;

			//add the templateRoot component to the new object
			Character character = gameObject.AddComponent<Character>();
			character.templateRoot = templateRoot;

			return character;
		}

		public void EquipItem(Item item, EquipSlot slot)
		{
			Transform hook = transform.FindChildRecursive("rhand");
			Item instance = Instantiate(item);

			instance.transform.parent = hook;
			instance.transform.localPosition = Vector3.zero;
			instance.transform.localRotation = Quaternion.identity;
		}

		protected override void Start()
		{
			animation.wrapMode = WrapMode.Loop;

			if (animation["cpause1"] != null) {
				animation.Play("cpause1");
			}
			else if (animation["pause1"] != null) {
				animation.Play("pause1");
			}
		}
	}
}
