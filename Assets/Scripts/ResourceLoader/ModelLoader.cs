using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace KotORVR
{
	public static partial class Resources
	{
		public static GameObject LoadModel(string resref)
		{
			Stream mdl = GetStream(resref, ResourceType.MDL), mdx = GetStream(resref, ResourceType.MDX);
			if (mdl == null || mdx == null) {
				Debug.Log("Missing model: " + resref);
				return new GameObject(resref);
			}

			AuroraModel auroraModel = new AuroraModel(mdl, mdx, targetGame);

			GameObject CreateObject(AuroraModel.Node node, Transform parent)
			{
				GameObject go = new GameObject(node.name);
				if (parent) {
					go.transform.SetParent(parent, false);
				}

				go.transform.localPosition = node.position;
				go.transform.localRotation = node.rotation;

				if (node is AuroraModel.MeshNode) {
					AuroraModel.MeshNode auroraMesh = (AuroraModel.MeshNode)node;
					Mesh mesh = auroraMesh.CreateUnityMesh();

					//if (auroraMesh.isWalkmesh) {
						MeshCollider col = go.AddComponent<MeshCollider>();
						col.cookingOptions = MeshColliderCookingOptions.None;
						col.sharedMesh = mesh;
					//}

					if (node is AuroraModel.SkinnedMeshNode) {
						SkinnedMeshRenderer renderer = go.AddComponent<SkinnedMeshRenderer>();

						renderer.material = LoadMaterial(auroraMesh.DiffuseMap, auroraMesh.LightMap);
						renderer.sharedMesh = mesh;
					}
					else {
						go.AddComponent<MeshFilter>().mesh = mesh;

						MeshRenderer renderer = go.AddComponent<MeshRenderer>();
						renderer.material = LoadMaterial(auroraMesh.DiffuseMap, auroraMesh.LightMap);

						//meshes with a null texture should be invisible
						if (auroraMesh.DiffuseMap == "NULL") {
							renderer.enabled = false;
						}
					}
				}

				for (int i = 0; i < node.children.Length; i++) {
					CreateObject(node.children[i], go.transform);
				}

				node.transform = go.transform;
				return go;
			}

			GameObject model = CreateObject(auroraModel.rootNode, null);

			void SkinObject(AuroraModel.Node node)
			{
				if (node is AuroraModel.SkinnedMeshNode) {					
					SkinnedMeshRenderer renderer = node.transform.GetComponent<SkinnedMeshRenderer>();
					Mesh mesh = renderer.sharedMesh;

					short[] boneMapping = ((AuroraModel.SkinnedMeshNode)node).boneToNodeMap;

					List<Transform> boneTransforms = new List<Transform>();
					List<Matrix4x4> bindPoses = new List<Matrix4x4>();

					for (int i = 0; i < boneMapping.Length; i++) {
						if (boneMapping[i] >= 0 && boneMapping[i] < auroraModel.nodes.Count) {
							Transform t = auroraModel.nodes[boneMapping[i]].transform;

							boneTransforms.Add(t);
							bindPoses.Add(t.worldToLocalMatrix * node.transform.localToWorldMatrix);
						}
					}

					renderer.bones = boneTransforms.ToArray();
					mesh.bindposes = bindPoses.ToArray();
				}

				for (int i = 0; i < node.children.Length; i++) {
					SkinObject(node.children[i]);
				}
			}

			Animation animComponent = model.AddComponent<Animation>();
			AnimationClip[] clips = auroraModel.GetUnityAnimationClips();

			//TODO: check if animation is looping
			
			for (int i = 0; i < clips.Length; i++) {
				animComponent.AddClip(clips[i], clips[i].name);
			}

			SkinObject(auroraModel.rootNode);

			return model;
		}
	}
}
