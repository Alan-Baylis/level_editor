using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor;
using RealtimeCSG;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
	internal sealed partial class MeshInstanceManager
	{
		[UnityEditor.Callbacks.PostProcessScene(0)]
		internal static void OnBuild()
		{
			// apparently only way to determine current scene while post processing a scene
			var randomObject = UnityEngine.Object.FindObjectOfType<Transform>();
			if (!randomObject)
				return;

			var currentScene = randomObject.gameObject.scene;
			
			var foundMeshContainers = SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshes>(currentScene);
			foreach (var meshContainer in foundMeshContainers)
			{
				var model = meshContainer.owner;
				if (!model)
				{
					UnityEngine.Object.DestroyImmediate(meshContainer.gameObject);
					continue;
				}
				
				if (model.NeedAutoUpdateRigidBody)
					AutoUpdateRigidBody(meshContainer);

				model.gameObject.hideFlags = HideFlags.None;
				meshContainer.transform.hideFlags = HideFlags.None;
				meshContainer.gameObject.hideFlags = HideFlags.None;
				meshContainer.hideFlags = HideFlags.None;

				var instances = meshContainer.GetComponentsInChildren<GeneratedMeshInstance>(true);
				foreach (var instance in instances)
				{
					if (!instance)
						continue;

					instance.gameObject.hideFlags = HideFlags.None;// HideFlags.NotEditable;
					instance.gameObject.SetActive(true);

					//Refresh(instance, model, postProcessScene: true);
#if SHOW_GENERATED_MESHES
					UpdateName(instance);
#endif

					// TODO: make sure meshes are no longer marked as dynamic!

					if (!HasRuntimeMesh(instance))
					{
						UnityEngine.Object.DestroyImmediate(instance.gameObject);
						continue;
					}

					var surfaceType = GetRenderSurfaceType(model, instance);
					if (surfaceType == RenderSurfaceType.ShadowOnly)
					{
						var meshRenderer = instance.gameObject.GetComponent<MeshRenderer>();
						if (meshRenderer)
						{
							meshRenderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
							meshRenderer.enabled = true;
						}
						RemoveIfEmpty(instance.gameObject);
						continue;
					}
					if (surfaceType == RenderSurfaceType.Normal)
					{
						var meshRenderer = instance.gameObject.GetComponent<MeshRenderer>();
						if (meshRenderer)
						{ 
							meshRenderer.enabled = true;
						}
					}

					if (surfaceType == RenderSurfaceType.Collider ||
						surfaceType == RenderSurfaceType.Trigger)
					{
						var meshRenderer = instance.gameObject.GetComponent<MeshRenderer>();
						if (meshRenderer)
							UnityEngine.Object.DestroyImmediate(meshRenderer);

						var meshFilter = instance.gameObject.GetComponent<MeshFilter>();
						if (meshFilter)
							UnityEngine.Object.DestroyImmediate(meshFilter);

						if (surfaceType == RenderSurfaceType.Trigger)
						{
							var oldMeshCollider = instance.gameObject.GetComponent<MeshCollider>();
							if (oldMeshCollider)
							{
								var newMeshCollider = model.gameObject.AddComponent<MeshCollider>();
								EditorUtility.CopySerialized(oldMeshCollider, newMeshCollider);
								UnityEngine.Object.DestroyImmediate(oldMeshCollider);
							}
						}
						RemoveIfEmpty(instance.gameObject);
						continue;
					}
					RemoveIfEmpty(instance.gameObject);
				}

				if (!meshContainer)
					continue;

				var children = meshContainer.GetComponentsInChildren<GeneratedMeshInstance>();
				foreach (var child in children)
				{
					child.hideFlags = HideFlags.None;
					child.gameObject.hideFlags = HideFlags.None;
					child.transform.hideFlags = HideFlags.None;
				}

				foreach (var instance in instances)
				{
					MeshUtility.Optimize(instance.SharedMesh);
				}

				UnityEngine.Object.DestroyImmediate(meshContainer);
			}


			var meshInstances = SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshInstance>(currentScene);
			foreach (var meshInstance in meshInstances)
			{
				if (meshInstance)
				{
					UnityEngine.Object.DestroyImmediate(meshInstance);
				}
			}

			var csgnodes = new HashSet<CSGNode>(SceneQueryUtility.GetAllComponentsInScene<CSGNode>(currentScene));
			foreach (var csgnode in csgnodes)
			{
				if (!csgnode)
					continue;

				var gameObject = csgnode.gameObject;
				var model = csgnode as CSGModel;

				if (
						(gameObject.tag == "EditorOnly" && !model) ||
						(model && model.name == InternalCSGModelManager.DefaultModelName && 
							(model.transform.childCount == 0 ||
								(model.transform.childCount == 1 &&
								model.transform.GetChild(0).name == MeshContainerName &&
								model.transform.GetChild(0).childCount == 0)
							)
						)
					)
				{
					UnityEngine.Object.DestroyImmediate(gameObject);
				} else
				if (model)
				{
					gameObject.tag = "Untagged";
					AssignLayerToChildren(gameObject);
				}
				if (csgnode)
					UnityEngine.Object.DestroyImmediate(csgnode);
			}
		}

		internal static void DestroyAllMeshInstances()
		{
			if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
			{
				var scene = SceneManager.GetSceneAt(sceneIndex);
				if (!scene.isLoaded)
					continue;
				var sceneModels = SceneQueryUtility.GetAllComponentsInScene<CSGModel>(scene);
				for (int i = 0; i < sceneModels.Count; i++)
				{
					Transform selfTransform = sceneModels[i].transform;
					Transform[] transforms = selfTransform.GetComponentsInChildren<Transform>();
					foreach (var transform in transforms)
					{
						if (!transform || transform.parent != selfTransform)
							continue;

						if (transform.name != MeshInstanceManager.MeshContainerName)
							continue;

						UnityEngine.Object.DestroyImmediate(transform.gameObject);
					}
				}
			}
		}
	}
}
