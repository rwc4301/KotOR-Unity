using UnityEngine;
using System.IO;

namespace KotORVR
{
	public static partial class Resources
	{
		public static GameObject LoadModel(string resref)
		{
			GameObject CreateObject(AuroraModel.Node node, Transform parent)
			{
				GameObject go = new GameObject(node.name);

				go.transform.position = node.position;
				go.transform.rotation = node.rotation;

				if (parent) {
					go.transform.SetParent(parent, false);
				}

				if (node is AuroraModel.MeshNode) {
					AuroraModel.MeshNode auroraMesh = (AuroraModel.MeshNode)node;

					Mesh mesh = auroraMesh.CreateUnityMesh();

					MeshCollider col = go.AddComponent<MeshCollider>();
					col.cookingOptions = MeshColliderCookingOptions.None;

					go.AddComponent<MeshFilter>().mesh = mesh;

					col.sharedMesh = mesh;

					//meshes with a NULL texture should be invisible
					if (auroraMesh.texMap1 != "NULL") {
						go.AddComponent<MeshRenderer>().material = LoadMaterial(auroraMesh.texMap1, auroraMesh.texMap2);
					}
				}

				for (int i = 0; i < node.children.Length; i++) {
					CreateObject(node.children[i], go.transform);
				}

				return go;
			}

			Stream mdl = GetStream(resref, ResourceType.MDL), mdx = GetStream(resref, ResourceType.MDX);
			if (mdl == null || mdx == null) {
				Debug.Log("Missing model: " + resref);
				return new GameObject(resref);
			}

			AuroraModel auroraModel = new AuroraModel(mdl, mdx);

			GameObject model = CreateObject(auroraModel.rootNode, null);
			Animation animComponent = model.AddComponent<Animation>();

			AnimationClip[] clips = auroraModel.GetUnityAnimationClips();
			
			for (int i = 0; i < clips.Length; i++) {
				animComponent.AddClip(clips[i], clips[i].name);
			}

			return model;
		}
	}
}
