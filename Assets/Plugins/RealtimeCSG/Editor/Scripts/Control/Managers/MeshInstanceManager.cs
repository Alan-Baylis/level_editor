//#define SHOW_GENERATED_MESHES
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using RealtimeCSG;
using RealtimeCSG.Foundation;
using RealtimeCSG.Components;
using UnityEditor.SceneManagement;

namespace InternalRealtimeCSG
{
	internal sealed partial class MeshInstanceManager
	{
#if SHOW_GENERATED_MESHES
		public const HideFlags ComponentHideFlags = HideFlags.DontSaveInBuild;
#else
		public const HideFlags ComponentHideFlags = HideFlags.None
													//| HideFlags.NotEditable // when this is put into a prefab (when making a prefab containing a model for instance) this will make it impossible to delete 
													| HideFlags.HideInInspector
													| HideFlags.HideInHierarchy
													| HideFlags.DontSaveInBuild
			;
#endif

		internal const string MeshContainerName			= "[generated-meshes]";
		private const string RenderMeshInstanceName		= "[generated-render-mesh]";
		private const string ColliderMeshInstanceName	= "[generated-collider-mesh]";
		private const string HelperMeshInstanceName		= "[generated-helper-mesh]";

		public static void Shutdown()
		{
		}

		public static void OnDestroyed(GeneratedMeshes container)
		{
		}

		public static void Destroy(GeneratedMeshes container)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			if (container)
			{
				container.gameObject.hideFlags = HideFlags.None;
				//Debug.LogError("Destroy");
				GameObject.DestroyImmediate(container.gameObject);
			}
			// Undo.DestroyObjectImmediate // NOTE: why was this used before?
			// can't use Undo variant here because it'll mark scenes as dirty on load ..
		}

		public static void OnCreated(GeneratedMeshes container)
		{
			ValidateContainer(container);
		}

		public static void OnEnable(GeneratedMeshes container)
		{
			if (container)
				container.gameObject.SetActive(true);
		}

		public static void OnDisable(GeneratedMeshes container)
		{
			if (container)
				container.gameObject.SetActive(false);
		}
		
		public static void OnCreated(GeneratedMeshInstance meshInstance)
		{
			//Debug.LogWarning("OnCreated");
			var parent = meshInstance.transform.parent;
			GeneratedMeshes container = null;
			if (parent)
				container = parent.GetComponent<GeneratedMeshes>();
			if (!container)
			{
				meshInstance.gameObject.hideFlags = HideFlags.None;
				//Debug.LogError("Destroy");
				UnityEngine.Object.DestroyImmediate(meshInstance.gameObject);
				return;
			}

			//EnsureValidHelperMaterials(container.owner, meshInstance);

			Initialize(container, meshInstance);

			var key = meshInstance.GenerateKey();
			if (container.meshInstances[key] != meshInstance)
			{
				if (meshInstance && meshInstance.gameObject)
				{
					meshInstance.gameObject.hideFlags = HideFlags.None;
					//if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal)
					//{
						//Debug.Log("destroy");
					//}
					UnityEngine.Object.DestroyImmediate(meshInstance.gameObject);
				}
			}
		}

		static void Initialize(GeneratedMeshes container, GeneratedMeshInstance meshInstance)
		{
			var key = meshInstance.GenerateKey();
			//if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal)
			//{
			//	Debug.Log("register instance = " + meshInstance.MeshDescription.meshQuery.UsedVertexChannels + " " + meshInstance.MeshDescription.geometryHashValue + " " + meshInstance.MeshDescription.surfaceHashValue + " " + meshInstance.MeshDescription.vertexCount + " " + meshInstance.MeshDescription.indexCount, container);
			//}
			container.meshInstances[key] = meshInstance;
		}

		public static bool ValidateModel(CSGModel model, CSGModelCache modelCache, bool checkChildren = false)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return true;

			if (!checkChildren && modelCache.GeneratedMeshes)
				return true;

			modelCache.GeneratedMeshes = null;
			var meshContainers = model.GetComponentsInChildren<GeneratedMeshes>(true);
			if (meshContainers.Length > 1)
			{
				for (var i = meshContainers.Length - 1; i >= 0; i--)
				{
					var parentModel = meshContainers[i].GetComponentInParent<CSGModel>();
					if (parentModel != model)
					{
						ArrayUtility.RemoveAt(ref meshContainers, i);
					}
				}
			}
			if (meshContainers.Length > 1)
			{
				modelCache.GeneratedMeshes = null;
				// destroy all other meshcontainers ..
				for (var i = meshContainers.Length - 1; i >= 0; i--)
				{
					var gameObject = meshContainers[i].gameObject;
					gameObject.hideFlags = HideFlags.None;
					//Debug.LogError("Destroy");
					UnityEngine.Object.DestroyImmediate(gameObject);
				}
			}
			
			GeneratedMeshes meshContainer;
			if (meshContainers.Length >= 1)
			{
				meshContainer = meshContainers[0];
				meshContainer.owner = model;

				ValidateContainer(meshContainer);
			} else
			{
				// create it if it doesn't exist
				var containerObject = new GameObject { name = MeshContainerName };
				meshContainer = containerObject.AddComponent<GeneratedMeshes>();
				meshContainer.owner = model;
				var containerObjectTransform = containerObject.transform;
				containerObjectTransform.SetParent(model.transform, false);
				UpdateGeneratedMeshesVisibility(meshContainer, model.ShowGeneratedMeshes);
			}

			if (meshContainer)
			{
				if (meshContainer.enabled == false)
					meshContainer.enabled = true;
				var meshContainerGameObject = meshContainer.gameObject;
				var activated = (model.enabled && model.gameObject.activeSelf);
				if (meshContainerGameObject.activeSelf != activated)
					meshContainerGameObject.SetActive(activated);
			}

			modelCache.GeneratedMeshes = meshContainer;
			modelCache.ForceUpdate = true;
			return true;
		}
		
		
		public static RenderSurfaceType GetRenderSurfaceType(CSGModel model, GeneratedMeshInstance meshInstance)
		{
			return GetSurfaceType(meshInstance.MeshDescription, model.Settings);
		}

		private static bool ShouldRenderHelperSurface(GeneratedMeshInstance meshInstance)
		{
			var renderSurfaceType = meshInstance.RenderSurfaceType;
			switch (renderSurfaceType)
			{
				case RenderSurfaceType.Hidden:			return CSGSettings.ShowHiddenSurfaces;
				case RenderSurfaceType.Culled:			return CSGSettings.ShowCulledSurfaces;
				case RenderSurfaceType.Collider:		return CSGSettings.ShowColliderSurfaces;
				case RenderSurfaceType.Trigger:			return CSGSettings.ShowTriggerSurfaces;
				case RenderSurfaceType.ShadowOnly:		
				case RenderSurfaceType.CastShadows:		return CSGSettings.ShowCastShadowsSurfaces;
				case RenderSurfaceType.ReceiveShadows:	return CSGSettings.ShowReceiveShadowsSurfaces;
			}
			return false;
		}

		/*
		private static RenderSurfaceType EnsureValidHelperMaterials(CSGModel model, GeneratedMeshInstance meshInstance)
		{
			var surfaceType = !meshInstance.RenderMaterial ? meshInstance.RenderSurfaceType : 
								GetRenderSurfaceType(model, meshInstance);
			if (surfaceType != RenderSurfaceType.Normal)
				meshInstance.RenderMaterial = MaterialUtility.GetSurfaceMaterial(surfaceType);
			return surfaceType;
		}
		*/

		private static bool ValidMeshInstance(GeneratedMeshInstance instance)
		{
			return (!instance.PhysicsMaterial || instance.PhysicsMaterial.GetInstanceID() != 0) &&
				   (!instance.RenderMaterial || instance.RenderMaterial.GetInstanceID() != 0) &&
				   (!instance.SharedMesh || instance.SharedMesh.GetInstanceID() != 0);
		}

		public static bool HasVisibleMeshRenderer(GeneratedMeshInstance meshInstance)
		{
			if (!meshInstance)
				return false;
			return meshInstance.RenderSurfaceType == RenderSurfaceType.Normal;
		}

		public static bool HasRuntimeMesh(GeneratedMeshInstance meshInstance)
		{
			if (!meshInstance)
				return false;
			return	meshInstance.RenderSurfaceType != RenderSurfaceType.Culled &&
					meshInstance.RenderSurfaceType != RenderSurfaceType.Hidden;
		}

		public static void RenderHelperSurfaces(SceneView sceneView)
		{
			var allHelperSurfaces = (CSGSettings.VisibleHelperSurfaces & ~HelperSurfaceFlags.ShowVisibleSurfaces);
			if (allHelperSurfaces == (HelperSurfaceFlags)0)
			{
				CSGSettings.VisibleHelperSurfaces = CSGSettings.DefaultHelperSurfaceFlags;
				return;
			}

			var camera			= sceneView.camera;			
			var showWireframe	= RealtimeCSG.CSGSettings.IsWireframeShown(sceneView);
				
			var visibleLayers	= Tools.visibleLayers;
			var models			= InternalCSGModelManager.Models;
			for (var i = 0; i < models.Length; i++)
			{
				var model = models[i];
				if (!model)
					continue;

				if (((1 << model.gameObject.layer) & visibleLayers) == 0)
					continue;

				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null ||
					!modelCache.GeneratedMeshes)
					continue;

				var container = modelCache.GeneratedMeshes;
				if (!container.owner ||
					!container.owner.isActiveAndEnabled)
				{
					continue;
				}

				var meshInstances = container.meshInstances;
				if (meshInstances == null ||
					meshInstances.Count == 0)
				{
					ValidateContainer(container);
					continue;
				}

				//var modelDoesNotRender = !model.IsRenderable;
				
				var matrix = container.transform.localToWorldMatrix;
				foreach (var meshInstance in meshInstances.Values)
				{
					if (!meshInstance)
					{
						modelCache.GeneratedMeshes = null;
						meshInstance.hideFlags = HideFlags.None;
						UnityEngine.Object.DestroyImmediate(meshInstance); // wtf?
						break;
					}

					if (!ShouldRenderHelperSurface(meshInstance))
						continue;

					var renderSurfaceType = meshInstance.RenderSurfaceType;
					if (renderSurfaceType == RenderSurfaceType.Normal)
						continue;

					var material = MaterialUtility.GetSurfaceMaterial(renderSurfaceType);
					if (!material)
						continue;

					if (!meshInstance.HasGeneratedNormals)
					{
						meshInstance.SharedMesh.RecalculateNormals();
						meshInstance.HasGeneratedNormals = true;
					}

					if (!showWireframe)
					{
						// "DrawMeshNow" so that it renders properly in all shading modes
						if (material.SetPass(0))
							Graphics.DrawMeshNow(meshInstance.SharedMesh,
													matrix);
					} else
					{
						Graphics.DrawMesh(meshInstance.SharedMesh,
										  matrix,
										  material,
										  layer: 0,
										  camera: camera,
										  submeshIndex: 0,
										  properties: null,
										  castShadows: false,
										  receiveShadows: false);
					}
				}
			}
		}

		public static UnityEngine.Object[] FindRenderers(CSGModel[] models)
		{
			var renderers = new List<UnityEngine.Object>();
			foreach (var model in models)
			{
				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null)
					continue;

				if (!modelCache.GeneratedMeshes)
					continue;

				foreach (var renderer in modelCache.GeneratedMeshes.GetComponentsInChildren<MeshRenderer>())
				{
					if (!renderer)
						continue;

					var type = MaterialUtility.GetMaterialSurfaceType(renderer.sharedMaterial);
					if (type == RenderSurfaceType.Normal)
						continue;

					renderers.Add(renderer);
				}
			}
			return renderers.ToArray();
		}

		internal static void Reset()
		{
			if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
			{
				var scene = SceneManager.GetSceneAt(sceneIndex);
				if (!scene.isLoaded)
					continue;

				var meshInstances = SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshInstance>(scene);
				for (int m = 0; m < meshInstances.Count; m++)
				{
					meshInstances[m].hideFlags = HideFlags.None;
					//if (meshInstances[m].RenderSurfaceType == RenderSurfaceType.Normal)
					//{
					//	Debug.Log("destroy");
					//}
					UnityEngine.Object.DestroyImmediate(meshInstances[m]);
				}
			}
		}

		public static RenderSurfaceType GetSurfaceType(GeneratedMeshDescription meshDescription, ModelSettingsFlags modelSettings)
		{
			if (meshDescription.meshQuery.LayerQuery == LayerUsageFlags.Culled) return RenderSurfaceType.Culled;
			switch (meshDescription.meshQuery.LayerParameterIndex)
			{
				case LayerParameterIndex.LayerParameter1:
				{
					switch (meshDescription.meshQuery.LayerQuery)
					{
						default:
						case LayerUsageFlags.RenderReceiveCastShadows:	
						case LayerUsageFlags.Renderable:				return RenderSurfaceType.Normal; 
						case LayerUsageFlags.CastShadows:				return RenderSurfaceType.ShadowOnly; 
						case LayerUsageFlags.RenderCastShadows:		return RenderSurfaceType.CastShadows; 
						case LayerUsageFlags.RenderReceiveShadows:	return RenderSurfaceType.ReceiveShadows; 
					}
				}
				case LayerParameterIndex.LayerParameter2:
				{
					if ((modelSettings & ModelSettingsFlags.IsTrigger) != 0)
					{
						return RenderSurfaceType.Trigger;
					} else
					{
						return RenderSurfaceType.Collider;
					}
				}
				case LayerParameterIndex.None:
				{
					switch (meshDescription.meshQuery.LayerQuery)
					{
						default:
						case LayerUsageFlags.None:			return RenderSurfaceType.Hidden;
						case LayerUsageFlags.CastShadows:		return RenderSurfaceType.CastShadows;
						case LayerUsageFlags.ReceiveShadows:	return RenderSurfaceType.ReceiveShadows;
						case LayerUsageFlags.Collidable:
						{
							if ((modelSettings & ModelSettingsFlags.IsTrigger) != 0)
							{
								return RenderSurfaceType.Trigger;
							} else
								return RenderSurfaceType.Collider;
						}
						case LayerUsageFlags.Culled:			return RenderSurfaceType.Culled;
					}
				}
			}
			return RenderSurfaceType.Normal;
		}

		public static GeneratedMeshInstance CreateMesh(GeneratedMeshes container, GeneratedMeshDescription meshDescription, ModelSettingsFlags modelSettings)
		{
			if (!container || !container.owner)
				return null;
			
			var meshInstanceGameObject = new GameObject();
			if (!meshInstanceGameObject)
				return null;

			var instance = meshInstanceGameObject.AddComponent<GeneratedMeshInstance>();
			if (!instance)
				return null;

			meshInstanceGameObject.transform.SetParent(container.transform, false);
			meshInstanceGameObject.transform.localPosition	= MathConstants.zeroVector3;
			meshInstanceGameObject.transform.localRotation	= MathConstants.identityQuaternion;
			meshInstanceGameObject.transform.localScale		= MathConstants.oneVector3;

			var containerStaticFlags = GameObjectUtility.GetStaticEditorFlags(container.owner.gameObject);
			GameObjectUtility.SetStaticEditorFlags(meshInstanceGameObject, containerStaticFlags);
			

			Material			renderMaterial		= null;
			PhysicMaterial		physicsMaterial		= null;
			if (meshDescription.surfaceParameter != 0)
			{
				var obj = EditorUtility.InstanceIDToObject(meshDescription.surfaceParameter);
				if (obj)
				{ 
					switch (meshDescription.meshQuery.LayerParameterIndex)
					{
						case LayerParameterIndex.LayerParameter1: { renderMaterial	= obj as Material;       break; }
						case LayerParameterIndex.LayerParameter2: { physicsMaterial	= obj as PhysicMaterial; break; }
					}
				}
			}
			
			instance.MeshDescription = meshDescription;
			
			// Our mesh has not been initialized yet, so make sure we reflect that fact
			instance.MeshDescription.geometryHashValue	= 0;
			instance.MeshDescription.surfaceHashValue	= 0;
			instance.MeshDescription.vertexCount		= 0;
			instance.MeshDescription.indexCount			= 0;
						
			instance.RenderMaterial		= renderMaterial;
			instance.PhysicsMaterial	= physicsMaterial;
			instance.RenderSurfaceType	= GetSurfaceType(meshDescription, modelSettings);

			Initialize(container, instance);
			return instance;
		}

		internal static void ClearMesh(GeneratedMeshInstance meshInstance)
		{
			meshInstance.HasGeneratedNormals = false;
			if (meshInstance.SharedMesh)
			{
				meshInstance.SharedMesh.Clear(keepVertexLayout: true);
				return;
			}

			meshInstance.SharedMesh = new Mesh();
			meshInstance.SharedMesh.MarkDynamic();
		}

		public static bool NeedToGenerateLightmapUVsForModel(CSGModel model)
		{
			if (!model)
				return false;

			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null ||
				!modelCache.GeneratedMeshes)
				return false;

			var container = modelCache.GeneratedMeshes;
			if (!container || container.owner != model)
				return false;

			if (container.meshInstances == null)
				return false;

			var staticFlags = GameObjectUtility.GetStaticEditorFlags(container.owner.gameObject);
			if ((staticFlags & StaticEditorFlags.LightmapStatic) != StaticEditorFlags.LightmapStatic)
				return false;
				
			foreach (var pair in container.meshInstances)
			{
				var instance = pair.Value;
				if (!instance)
					continue;

				if (NeedToGenerateLightmapUVsForInstance(instance))
					return true;
			}
			return false;
		}

		public static void GenerateLightmapUVsForModel(CSGModel model)
		{
			if (!model)
				return;

			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null ||
				!modelCache.GeneratedMeshes)
				return;

			var container = modelCache.GeneratedMeshes;
			if (!container || !container.owner)
				return;

			if (container.meshInstances == null)
				return;

			var uvGenerationSettings = new UnityEditor.UnwrapParam
			{
				angleError = Mathf.Clamp(model.angleError, CSGModel.minAngleError, CSGModel.maxAngleError),
				areaError  = Mathf.Clamp(model.areaError, CSGModel.minAreaError, CSGModel.maxAreaError),
				hardAngle  = model.hardAngle,
				packMargin = model.packMargin
			};

			foreach (var pair in container.meshInstances)
			{
				var instance = pair.Value;
				if (!instance)
					continue;

				GenerateLightmapUVsForInstance(instance, model, uvGenerationSettings);
			}
		}

		private static void GenerateLightmapUVsForInstance(GeneratedMeshInstance instance, CSGModel model, UnwrapParam param)
		{
			var meshRendererComponent = instance.CachedMeshRenderer;
			if (!meshRendererComponent)
			{
				var gameObject = instance.gameObject;
				meshRendererComponent = gameObject.GetComponent<MeshRenderer>();
				instance.CachedMeshRendererSO = null;
			}

			if (!meshRendererComponent)
				return;

			meshRendererComponent.realtimeLightmapIndex = -1;
			meshRendererComponent.lightmapIndex = -1;
			
			var oldVertices		= instance.SharedMesh.vertices;
			if (oldVertices.Length == 0)
				return;

			var oldUV			= instance.SharedMesh.uv;
			var oldNormals		= instance.SharedMesh.normals;
			var oldTangents		= instance.SharedMesh.tangents;
			var oldColors		= instance.SharedMesh.colors;
			var oldTriangles	= instance.SharedMesh.triangles;

			var tempMesh = new Mesh
			{
				vertices	= oldVertices,
				normals		= oldNormals,
				uv			= oldUV,
				tangents	= oldTangents,
				colors		= oldColors,
				triangles	= oldTriangles
			};
			tempMesh.bounds = instance.SharedMesh.bounds;
			instance.SharedMesh = tempMesh;

			Debug.Log("Generating lightmap UVs (by Unity) for the mesh " + instance.name + " of the Model named \"" + model.name +"\"\n", model);
			//var optimizeTime = EditorApplication.timeSinceStartup;
			//MeshUtility.Optimize(instance.SharedMesh);
			//optimizeTime = EditorApplication.timeSinceStartup - optimizeTime;

			var lightmapGenerationTime = EditorApplication.timeSinceStartup;
			Unwrapping.GenerateSecondaryUVSet(instance.SharedMesh, param);
			lightmapGenerationTime = EditorApplication.timeSinceStartup - lightmapGenerationTime;
			
			Debug.Log(//"\tMesh optimizing in " + (optimizeTime * 1000) + " ms\n"+
					  "\tUV generation in " + (lightmapGenerationTime* 1000) + " ms\n", model);

			EditorSceneManager.MarkSceneDirty(instance.gameObject.scene);
			instance.HasUV2 = true;
			instance.LightingHashValue = instance.MeshDescription.geometryHashValue;
		}
		
		private static bool NeedToGenerateLightmapUVsForInstance(GeneratedMeshInstance instance)
		{
			return !instance.HasUV2 && instance.RenderSurfaceType == RenderSurfaceType.Normal;
		}

		private static bool NeedCollider(CSGModel model, GeneratedMeshInstance instance)
		{
			return //((model.HaveCollider || model.IsTrigger) &&

					(//instance.RenderSurfaceType == RenderSurfaceType.Normal ||
						instance.RenderSurfaceType == RenderSurfaceType.Collider ||
						instance.RenderSurfaceType == RenderSurfaceType.Trigger
						) &&

					// Make sure the bounds of the mesh are not empty ...
					(Mathf.Abs(instance.SharedMesh.bounds.size.x) > MathConstants.EqualityEpsilon ||
						Mathf.Abs(instance.SharedMesh.bounds.size.y) > MathConstants.EqualityEpsilon ||
						Mathf.Abs(instance.SharedMesh.bounds.size.z) > MathConstants.EqualityEpsilon)
			//			)
			;
		}

		static StaticEditorFlags FilterStaticEditorFlags(StaticEditorFlags oldStaticFlags, RenderSurfaceType renderSurfaceType)
		{
			var newStaticFlags = oldStaticFlags;
			var walkable =	renderSurfaceType != RenderSurfaceType.Hidden &&
							renderSurfaceType != RenderSurfaceType.ShadowOnly &&
							renderSurfaceType != RenderSurfaceType.Culled &&
							renderSurfaceType != RenderSurfaceType.Trigger;
			if (walkable)	newStaticFlags = newStaticFlags | StaticEditorFlags.NavigationStatic;
			else			newStaticFlags = newStaticFlags & ~StaticEditorFlags.NavigationStatic;
			
			if (renderSurfaceType != RenderSurfaceType.Normal &&
				renderSurfaceType != RenderSurfaceType.ShadowOnly)
				newStaticFlags = (StaticEditorFlags)0;

			return newStaticFlags;
		}

		static string MaterialToString(Material mat)
		{
			if (ReferenceEquals(mat, null))
				return "null";
			if (!mat)
				return "invalid";
			return mat.name + " " + mat.GetInstanceID().ToString();
		}

		public static void ClearUVs(CSGModel model)
		{
			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null ||
				!modelCache.GeneratedMeshes)
				return;

			var container = modelCache.GeneratedMeshes;
			if (!container || !container.owner)
				return;

			foreach (var pair in container.meshInstances)
			{
				var instance = pair.Value;
				if (!instance)
					continue;

				Refresh(instance, model);
				ClearUVs(instance);
			}
		}

		public static void ClearUVs(GeneratedMeshInstance instance)
		{
			var meshRendererComponent	= instance.CachedMeshRenderer;
			if (meshRendererComponent)
			{
				meshRendererComponent.realtimeLightmapIndex = -1;
				meshRendererComponent.lightmapIndex = -1;
			}
			instance.LightingHashValue = 0;
			instance.HasUV2 = false;
		}


//		internal static double updateMeshColliderMeshTime = 0.0;
		public static void Refresh(GeneratedMeshInstance instance, CSGModel owner, bool postProcessScene = false, bool forceUpdate = false)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			if (!instance)
				return;

			if (!instance.SharedMesh)
			{
				ClearMesh(instance);
				instance.Dirty = true;
			}

			if (!instance.SharedMesh)
				return;


			// Update the flags
			var oldRenderSurfaceType	= instance.RenderSurfaceType;
			if (!instance.RenderMaterial)
				instance.RenderMaterial = MaterialUtility.GetSurfaceMaterial(oldRenderSurfaceType);
			instance.RenderSurfaceType	= GetRenderSurfaceType(owner, instance);
			instance.Dirty			= instance.Dirty || (oldRenderSurfaceType != instance.RenderSurfaceType);


			// Update the transform, if incorrect
			var gameObject = instance.gameObject; 
			if (gameObject.transform.localPosition	!= MathConstants.zeroVector3)			gameObject.transform.localPosition	= MathConstants.zeroVector3;
			if (gameObject.transform.localRotation	!= MathConstants.identityQuaternion)	gameObject.transform.localRotation	= MathConstants.identityQuaternion;
			if (gameObject.transform.localScale		!= MathConstants.oneVector3)			gameObject.transform.localScale		= MathConstants.oneVector3;


#if SHOW_GENERATED_MESHES
			var meshInstanceFlags   = HideFlags.None;
			var transformFlags      = HideFlags.None;
			var gameObjectFlags     = HideFlags.None;
#else
			var meshInstanceFlags   = HideFlags.DontSaveInBuild;// | HideFlags.NotEditable;
			var transformFlags      = HideFlags.HideInInspector;// | HideFlags.NotEditable;
			var gameObjectFlags     = HideFlags.None;
#endif

			if (gameObject.transform.hideFlags	!= transformFlags)		{ gameObject.transform.hideFlags	= transformFlags; }
			if (gameObject.hideFlags			!= gameObjectFlags)		{ gameObject.hideFlags				= gameObjectFlags; }
			if (instance.hideFlags				!= meshInstanceFlags)	{ instance.hideFlags				= meshInstanceFlags; }

			
			var showVisibleSurfaces	=	instance.RenderSurfaceType != RenderSurfaceType.Normal ||
										(RealtimeCSG.CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowVisibleSurfaces) != 0;

			if (gameObject.activeSelf != showVisibleSurfaces) gameObject.SetActive(showVisibleSurfaces);
			if (!instance.enabled) instance.enabled = true;
			
			
			// Update navigation on mesh
			var oldStaticFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);
			var newStaticFlags = FilterStaticEditorFlags(GameObjectUtility.GetStaticEditorFlags(owner.gameObject), instance.RenderSurfaceType);
			if (newStaticFlags != oldStaticFlags)
			{
				GameObjectUtility.SetStaticEditorFlags(gameObject, newStaticFlags);
			}

			var meshFilterComponent		= instance.CachedMeshFilter;
			var meshRendererComponent	= instance.CachedMeshRenderer;
			if (!meshRendererComponent)
			{
				meshRendererComponent = gameObject.GetComponent<MeshRenderer>();
				instance.CachedMeshRendererSO = null;
			}

			var needMeshCollider		= NeedCollider(owner, instance);
			
			

			var needMeshRenderer		= (instance.RenderSurfaceType == RenderSurfaceType.Normal ||
										   instance.RenderSurfaceType == RenderSurfaceType.ShadowOnly);
			if (needMeshRenderer)
			{
				if (!meshFilterComponent)
				{
					meshFilterComponent = gameObject.GetComponent<MeshFilter>();
					if (!meshFilterComponent)
					{
						meshFilterComponent = gameObject.AddComponent<MeshFilter>();
						instance.CachedMeshRendererSO = null;
						instance.Dirty = true;
					}
				}

//				var ownerReceiveShadows = owner.ReceiveShadows;
//				var shadowCastingMode	= owner.ShadowCastingModeFlags;
				var ownerReceiveShadows = true;
				var shadowCastingMode	= UnityEngine.Rendering.ShadowCastingMode.On;
				if (instance.RenderSurfaceType == RenderSurfaceType.ShadowOnly)
				{
					shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
				}

				
				switch (instance.MeshDescription.meshQuery.LayerQuery)
				{
					case LayerUsageFlags.RenderReceiveCastShadows:
					{
						break;
					}
					case LayerUsageFlags.RenderReceiveShadows:
					{
						shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						break;
					} 
					case LayerUsageFlags.RenderCastShadows:
					{
						ownerReceiveShadows = false;
						break;
					}
					case LayerUsageFlags.Renderable:
					{
						shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						ownerReceiveShadows = false;
						break;
					}
					case LayerUsageFlags.CastShadows:
					{
						shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
						ownerReceiveShadows = false;
						break;
					}
				}


				var requiredMaterial = instance.RenderMaterial;
				if (shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
					// Note: need non-transparent material here
					requiredMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

				if (!requiredMaterial)
					requiredMaterial = MaterialUtility.MissingMaterial;

				if (!meshRendererComponent)
				{
					meshRendererComponent = gameObject.AddComponent<MeshRenderer>();
					meshRendererComponent.sharedMaterial = requiredMaterial;
					instance.CachedMeshRendererSO = null;
					instance.Dirty = true;
				}

				if ((meshFilterComponent.hideFlags & HideFlags.HideInHierarchy) == 0)
				{
					meshFilterComponent.hideFlags |= HideFlags.HideInHierarchy;
				}

				if ((meshRendererComponent.hideFlags & HideFlags.HideInHierarchy) == 0)
				{
					meshRendererComponent.hideFlags |= HideFlags.HideInHierarchy;
				}
				
				if (instance.RenderSurfaceType != RenderSurfaceType.ShadowOnly)
				{ 
					if (instance.HasUV2 && 
						(instance.LightingHashValue != instance.MeshDescription.geometryHashValue) && meshRendererComponent)
					{
						instance.ResetUVTime = Time.realtimeSinceStartup;
						if (instance.HasUV2)
							ClearUVs(instance);
					}

					if ((owner.AutoRebuildUVs || postProcessScene))
					{
						if ((float.IsPositiveInfinity(instance.ResetUVTime) || ((Time.realtimeSinceStartup - instance.ResetUVTime) > 2.0f)) &&
							NeedToGenerateLightmapUVsForModel(owner))
						{
							GenerateLightmapUVsForModel(owner);
						}
					}
				}

				if (!postProcessScene &&
					meshFilterComponent.sharedMesh != instance.SharedMesh)
					meshFilterComponent.sharedMesh = instance.SharedMesh;



				if (meshRendererComponent &&
					meshRendererComponent.shadowCastingMode != shadowCastingMode)
				{
					meshRendererComponent.shadowCastingMode = shadowCastingMode;
					instance.Dirty = true;
				}

				if (meshRendererComponent &&
					meshRendererComponent.receiveShadows != ownerReceiveShadows)
				{
					meshRendererComponent.receiveShadows = ownerReceiveShadows;
					instance.Dirty = true;
				}


				//*
				var meshRendererComponentSO	= instance.CachedMeshRendererSO as UnityEditor.SerializedObject;
				if (meshRendererComponentSO == null)
				{
					if (meshRendererComponent)
					{
						instance.CachedMeshRendererSO =
						meshRendererComponentSO = new SerializedObject(meshRendererComponent);
					}
				} else
				if (!meshRendererComponent)
				{
					instance.CachedMeshRendererSO =
					meshRendererComponentSO = null; 
				}
				if (meshRendererComponentSO != null)
				{
					bool SOModified = false;
					meshRendererComponentSO.Update(); 
					var scaleInLightmapProperty = meshRendererComponentSO.FindProperty("m_ScaleInLightmap");
					var scaleInLightmap			= owner.scaleInLightmap;
					if (scaleInLightmapProperty.floatValue != scaleInLightmap)
					{
						scaleInLightmapProperty.floatValue = scaleInLightmap;
						SOModified = true;
					}

					var autoUVMaxDistanceProperty		= meshRendererComponentSO.FindProperty("m_AutoUVMaxDistance");
					var autoUVMaxDistance				= owner.autoUVMaxDistance;
					if (autoUVMaxDistanceProperty.floatValue != autoUVMaxDistance)
					{
						autoUVMaxDistanceProperty.floatValue = autoUVMaxDistance;
						SOModified = true;
					}

					var autoUVMaxAngleProperty			= meshRendererComponentSO.FindProperty("m_AutoUVMaxAngle");
					var autoUVMaxAngle					= owner.autoUVMaxAngle;
					if (autoUVMaxAngleProperty.floatValue != autoUVMaxAngle)
					{
						autoUVMaxAngleProperty.floatValue = autoUVMaxAngle;
						SOModified = true;
					}

					var ignoreNormalsProperty			= meshRendererComponentSO.FindProperty("m_IgnoreNormalsForChartDetection");
					var ignoreNormals					= owner.IgnoreNormals;
					if (ignoreNormalsProperty.boolValue != ignoreNormals)
					{
						ignoreNormalsProperty.boolValue = ignoreNormals;
						SOModified = true;
					}
					
					var minimumChartSizeProperty		= meshRendererComponentSO.FindProperty("m_MinimumChartSize");
					var minimumChartSize				= owner.minimumChartSize;
					if (minimumChartSizeProperty.intValue != minimumChartSize)
					{
						minimumChartSizeProperty.intValue = minimumChartSize;
						SOModified = true;
					}


					var preserveUVsProperty		= meshRendererComponentSO.FindProperty("m_PreserveUVs");
					var preserveUVs				= owner.PreserveUVs;
					if (preserveUVsProperty.boolValue != preserveUVs)
					{
						preserveUVsProperty.boolValue = preserveUVs;
						SOModified = true;
					}

					if (SOModified)
						meshRendererComponentSO.ApplyModifiedProperties();
				}
				//*/

				if (meshRendererComponent &&
					meshRendererComponent.sharedMaterial != requiredMaterial)
				{
					//Debug.Log(MaterialToString(meshRendererComponent.sharedMaterial) + " => " + MaterialToString(requiredMaterial));
					meshRendererComponent.sharedMaterial = requiredMaterial;
					instance.Dirty = true;
				}

				// we don't actually want the unity style of rendering a wireframe 
				// for our meshes, so we turn it off
				//*
#if UNITY_5_5_OR_NEWER && !UNITY_5_5_0
				EditorUtility.SetSelectedRenderState(meshRendererComponent, EditorSelectedRenderState.Hidden);
#else
				EditorUtility.SetSelectedWireframeHidden(meshRendererComponent, true);
#endif
				//*/
			} else
			{
				if (!meshFilterComponent)	{ meshFilterComponent = gameObject.GetComponent<MeshFilter>(); }
				if (meshFilterComponent)	{ meshFilterComponent.hideFlags = HideFlags.None; UnityEngine.Object.DestroyImmediate(meshFilterComponent); instance.Dirty = true; }
				if (meshRendererComponent)	{ meshRendererComponent.hideFlags = HideFlags.None; UnityEngine.Object.DestroyImmediate(meshRendererComponent); instance.Dirty = true; }
				instance.LightingHashValue = instance.MeshDescription.geometryHashValue;
				meshFilterComponent = null;
				meshRendererComponent = null;
				instance.CachedMeshRendererSO = null;
			}
						
			instance.CachedMeshFilter   = meshFilterComponent;
			instance.CachedMeshRenderer = meshRendererComponent;

			// TODO:	navmesh specific mesh
			// TODO:	occludee/reflection probe static
			
			var meshColliderComponent	= instance.CachedMeshCollider;
			if (!meshColliderComponent)
				meshColliderComponent = gameObject.GetComponent<MeshCollider>();
			if (needMeshCollider)
			{
				if (meshColliderComponent && !meshColliderComponent.enabled)
					meshColliderComponent.enabled = true;

				if (!meshColliderComponent)
				{
					meshColliderComponent = gameObject.AddComponent<MeshCollider>();
					instance.Dirty = true;
				}

				// stops it from rendering wireframe in scene
				if ((meshColliderComponent.hideFlags & HideFlags.HideInHierarchy) == 0)
				{
					meshColliderComponent.hideFlags |= HideFlags.HideInHierarchy;
				}

				if (meshColliderComponent.sharedMaterial != instance.PhysicsMaterial)
				{
					meshColliderComponent.sharedMaterial = instance.PhysicsMaterial;
					instance.Dirty = true;
				}

				var setToConvex = owner.SetColliderConvex;
				if (meshColliderComponent.convex != setToConvex)
				{
					meshColliderComponent.convex = setToConvex;
					instance.Dirty = true;
				}

				if (instance.RenderSurfaceType == RenderSurfaceType.Trigger ||
					owner.IsTrigger)
				{
					if (!meshColliderComponent.isTrigger)
					{
						meshColliderComponent.isTrigger = true;
						instance.Dirty = true;
					}
				} else
				{
					if (meshColliderComponent.isTrigger)
					{
						meshColliderComponent.isTrigger = false;
						instance.Dirty = true;
					}
				}

				if (//instance.HasCollider &&
					meshColliderComponent.sharedMesh != instance.SharedMesh)
				{
//					instance.HasCollider = false; 
//					instance.ResetColliderTime = Time.realtimeSinceStartup;
//				}

				/*
				if ((owner.AutoRebuildColliders || postProcessScene || forceUpdate) && 
					(
						!instance.HasCollider && ((postProcessScene || forceUpdate) ||
						(float.IsPositiveInfinity(instance.ResetColliderTime) || ((Time.realtimeSinceStartup - instance.ResetColliderTime) > 2.0f)))
					))*/
//				{
//					var startUpdateMeshColliderMeshTime = EditorApplication.timeSinceStartup;
					meshColliderComponent.sharedMesh = instance.SharedMesh;
//					updateMeshColliderMeshTime += EditorApplication.timeSinceStartup - startUpdateMeshColliderMeshTime;
//					instance.HasCollider = true;
				}

				// .. for some reason this fixes mesh-colliders not being found with ray-casts in the editor?
#if UNITY_EDITOR
				if (instance.Dirty)
				{
					meshColliderComponent.enabled = false;
					meshColliderComponent.enabled = true;
				}
#endif
			} else
			{
				if (meshColliderComponent) { meshColliderComponent.hideFlags = HideFlags.None; UnityEngine.Object.DestroyImmediate(meshColliderComponent); instance.Dirty = true; }
				meshColliderComponent = null;
			}
			instance.CachedMeshCollider = meshColliderComponent;
			
			if (!postProcessScene)
			{
#if SHOW_GENERATED_MESHES
				if (instance.Dirty)
					UpdateName(instance);
#else
				if (needMeshRenderer)
				{
					if (instance.name != RenderMeshInstanceName)
						instance.name = RenderMeshInstanceName;
				} else
				if (needMeshCollider)
				{
					if (instance.name != ColliderMeshInstanceName)
						instance.name = ColliderMeshInstanceName;
				} else
				{
					if (instance.name != HelperMeshInstanceName)
						instance.name = HelperMeshInstanceName;
				}
#endif
				instance.Dirty = false;
			}
		}

#if SHOW_GENERATED_MESHES
		private static void UpdateName(GeneratedMeshInstance instance)
		{
			var renderMaterial			= instance.RenderMaterial;
			var parentObject			= instance.gameObject;

			var builder = new System.Text.StringBuilder();
			builder.Append(instance.RenderSurfaceType);
			builder.Append(' ');
			builder.Append(instance.GetInstanceID());

			if (instance.PhysicsMaterial)
			{
				var physicmaterialName = ((!instance.PhysicsMaterial) ? "default" : instance.PhysicsMaterial.name);
				if (builder.Length > 0) builder.Append(' ');
				builder.AppendFormat(" Physics [{0}]", physicmaterialName);
			}
			if (renderMaterial)
			{
				builder.AppendFormat(" Material [{0} {1}]", renderMaterial.name, renderMaterial.GetInstanceID());
			}

			builder.AppendFormat(" Key {0}", instance.GenerateKey().GetHashCode());

			var objectName = builder.ToString();
			if (parentObject.name != objectName) parentObject.name = objectName;
			if (instance.SharedMesh &&
				instance.SharedMesh.name != objectName)
				instance.SharedMesh.name = objectName;
		}
#endif

		static int RefreshModelCounter = 0;
		
		public static void UpdateHelperSurfaceVisibility(bool force = false)
		{
			//Debug.Log("UpdateHelperSurfaceVisibility");
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;
			
//			updateMeshColliderMeshTime = 0.0;
			var models = InternalCSGModelManager.Models;
			var currentRefreshModelCount = 0;
			for (var i = 0; i < models.Length; i++)
			{
				var model = models[i];
				if (!model)
					continue;

				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null ||
					!modelCache.GeneratedMeshes)
					continue;

				var container = modelCache.GeneratedMeshes;
				if (!container || !container.owner)
					continue;

				if (container.meshInstances == null)
				{
					ValidateContainer(container);

					if (container.meshInstances == null)
						continue;
				}

				if (!container)
					continue;

				if (force ||
					RefreshModelCounter == currentRefreshModelCount)
				{
					UpdateContainerFlags(container);
					foreach (var pair in container.meshInstances)
					{
						var instance = pair.Value;
						if (!instance)
							continue;

						Refresh(instance, container.owner);
					}
				}
				currentRefreshModelCount++;
			}
			
			if (RefreshModelCounter < currentRefreshModelCount)
				RefreshModelCounter++;
			else
				RefreshModelCounter = 0;
		}

		private static void AssignLayerToChildren(GameObject gameObject)
		{
			if (!gameObject)
				return;
			var layer = gameObject.layer;
			foreach (var transform in gameObject.GetComponentsInChildren<Transform>(true)) 
				transform.gameObject.layer = layer;
		}

		public static void UpdateGeneratedMeshesVisibility(CSGModel model)
		{
			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null ||
				!modelCache.GeneratedMeshes)
				return;

			UpdateGeneratedMeshesVisibility(modelCache.GeneratedMeshes, model.ShowGeneratedMeshes);
		}

		public static void UpdateGeneratedMeshesVisibility(GeneratedMeshes container, bool visible)
		{
			if (!container.owner.isActiveAndEnabled ||
				(container.owner.hideFlags & (HideFlags.HideInInspector|HideFlags.HideInHierarchy)) == (HideFlags.HideInInspector|HideFlags.HideInHierarchy))
				return;

			var containerGameObject = container.gameObject; 
			
			HideFlags gameObjectFlags;
			HideFlags transformFlags;
#if SHOW_GENERATED_MESHES
			gameObjectFlags = HideFlags.None;
#else
			if (visible)
			{
				gameObjectFlags = HideFlags.HideInInspector;
			} else
			{
				gameObjectFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
			}
#endif
			transformFlags = gameObjectFlags | HideFlags.NotEditable;

			if (containerGameObject.hideFlags != gameObjectFlags)
			{
				containerGameObject.hideFlags = gameObjectFlags;
			}

			if (container.transform.hideFlags != transformFlags)
			{
				container.transform.hideFlags   = transformFlags;
				container.hideFlags             = transformFlags | ComponentHideFlags;
			}
		}

		static void AutoUpdateRigidBody(GeneratedMeshes container)
		{
			var model		= container.owner;
			var gameObject	= model.gameObject;
			if (ModelTraits.NeedsRigidBody(model))
			{
				var rigidBody = container.CachedRigidBody;
				if (!rigidBody)
					rigidBody = model.GetComponent<Rigidbody>();
				if (!rigidBody)
					rigidBody = gameObject.AddComponent<Rigidbody>();

				if (rigidBody.hideFlags != HideFlags.None)
				{
					rigidBody.hideFlags = HideFlags.None;
				}

				RigidbodyConstraints constraints;
				bool isKinematic;
				bool useGravity;
				if (ModelTraits.NeedsStaticRigidBody(model))
				{
					isKinematic = true;
					useGravity = false;
					constraints = RigidbodyConstraints.FreezeAll;
				} else
				{
					isKinematic = false;
					useGravity = true;
					constraints = RigidbodyConstraints.None;
				}
				
				if (rigidBody.isKinematic != isKinematic) rigidBody.isKinematic = isKinematic;
				if (rigidBody.useGravity  != useGravity) rigidBody.useGravity = useGravity;
				if (rigidBody.constraints != constraints) rigidBody.constraints = constraints;
				container.CachedRigidBody = rigidBody;
			} else
			{
				var rigidBody = container.CachedRigidBody;
				if (!rigidBody)
					rigidBody = model.GetComponent<Rigidbody>();
				if (rigidBody)
				{
					rigidBody.hideFlags = HideFlags.None;
					UnityEngine.Object.DestroyImmediate(rigidBody);
				}
				container.CachedRigidBody = null;
			}
		}

		public static void RemoveIfEmpty(GameObject gameObject)
		{
			var allComponents = gameObject.GetComponents<Component>();
			for (var i = 0; i < allComponents.Length; i++)
			{
				if (allComponents[i] is Transform)
					continue;
				if (allComponents[i] is GeneratedMeshInstance)
					continue;

				return;
			}
			gameObject.hideFlags = HideFlags.None;
			UnityEngine.Object.DestroyImmediate(gameObject);
		}
		
		public static void ValidateContainer(GeneratedMeshes meshContainer)
		{
			if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			if (!meshContainer)
				return;

			var containerObject             = meshContainer.gameObject;
			var containerObjectTransform    = containerObject.transform;
			
			if (meshContainer.owner)
			{
				var modelCache = InternalCSGModelManager.GetModelCache(meshContainer.owner);
				if (modelCache != null)
				{
					if (modelCache.GeneratedMeshes &&
						modelCache.GeneratedMeshes != meshContainer)
					{
						meshContainer.hideFlags				= HideFlags.None;
						meshContainer.gameObject.hideFlags	= HideFlags.None;
						//Debug.LogError("Destroy");
						UnityEngine.Object.DestroyImmediate(meshContainer.gameObject);
						return;
					} 
				}
			} else
			if (meshContainer.owner != null)
			{
				meshContainer.hideFlags				= HideFlags.None;
				meshContainer.gameObject.hideFlags	= HideFlags.None;
				//Debug.LogError("Destroy");
				UnityEngine.Object.DestroyImmediate(meshContainer.gameObject);
				return;
			}

			//Debug.LogWarning("clear " + containerObjectTransform.childCount + " " + containerObjectTransform.name, containerObjectTransform);
			meshContainer.meshInstances.Clear();
			for (var i = 0; i < containerObjectTransform.childCount; i++)
			{
				var child = containerObjectTransform.GetChild(i);
				var meshInstance = child.GetComponent<GeneratedMeshInstance>();
				if (!meshInstance)
				{
					child.hideFlags				= HideFlags.None;
					child.gameObject.hideFlags	= HideFlags.None;
					UnityEngine.Object.DestroyImmediate(child.gameObject);
					continue;
				}
				var key = meshInstance.GenerateKey();
				if (meshContainer.meshInstances.ContainsKey(key))
				{
					//if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal)
					//	Debug.Log("destroy");
					child.hideFlags				= HideFlags.None;
					child.gameObject.hideFlags	= HideFlags.None;
					UnityEngine.Object.DestroyImmediate(child.gameObject);
					continue;
				}

				if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal && !meshInstance.RenderMaterial)
				{
					//if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal)
					//	Debug.Log("destroy");
					child.hideFlags				= HideFlags.None;
					child.gameObject.hideFlags	= HideFlags.None;
					UnityEngine.Object.DestroyImmediate(child.gameObject);
					continue;
				}

				if (!ValidMeshInstance(meshInstance) && !EditorApplication.isPlayingOrWillChangePlaymode)
				{
					//if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal)
					//	Debug.Log("destroy");
					child.hideFlags				= HideFlags.None;
					child.gameObject.hideFlags	= HideFlags.None;
					UnityEngine.Object.DestroyImmediate(child.gameObject);
					continue;
				}

				//if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal)
				//	Debug.Log("register = " + meshInstance.MeshDescription.meshQuery.UsedVertexChannels + " " + meshInstance.MeshDescription.geometryHashValue + " " + meshInstance.MeshDescription.surfaceHashValue + " " + meshInstance.MeshDescription.vertexCount + " " + meshInstance.MeshDescription.indexCount, 
				//				containerObjectTransform);
				meshContainer.meshInstances[key] = meshInstance;
			}

			if (string.IsNullOrEmpty(containerObject.name))
			{
				var flags = containerObject.hideFlags;

				if (containerObject.hideFlags != HideFlags.None)
				{
					containerObject.hideFlags = HideFlags.None;
				}

				containerObject.name  = MeshContainerName;

				if (containerObject.hideFlags != flags)
				{
					containerObject.hideFlags = flags;
				}
			}

			if (meshContainer.owner)
				UpdateGeneratedMeshesVisibility(meshContainer, meshContainer.owner.ShowGeneratedMeshes);
			
			if (meshContainer.owner)
			{
				var modelTransform = meshContainer.owner.transform;
				if (containerObjectTransform.parent != modelTransform)
					containerObjectTransform.parent.SetParent(modelTransform, true);
			}
		}
		
		public static GeneratedMeshInstance[] GetAllModelMeshInstances(GeneratedMeshes container)
		{
			if (container.meshInstances == null ||
				container.meshInstances.Count == 0)
				return null;

			return container.meshInstances.Values.ToArray();
		}
		
		public static GeneratedMeshInstance GetMeshInstance(GeneratedMeshes container, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription)
		{
			var key	= MeshInstanceKey.GenerateKey(meshDescription);
			GeneratedMeshInstance instance;
			if (container.meshInstances.TryGetValue(key, out instance))
			{
				if (instance)
				{
					//if (instance.RenderSurfaceType == RenderSurfaceType.Normal)
					//	Debug.Log("found " + instance.MeshDescription.meshQuery.UsedVertexChannels + " " + instance.MeshDescription.geometryHashValue + " " + instance.MeshDescription.surfaceHashValue + " " + instance.MeshDescription.vertexCount + " " + instance.MeshDescription.indexCount, container);
					return instance;
				} //else
				//	Debug.Log("found but invalid", container);
			}
			/*
			var renderSurfaceType = GetSurfaceType(meshDescription, modelSettings);
			if (renderSurfaceType == RenderSurfaceType.Normal)
			{
				foreach (var pair in container.meshInstances)
				{
					if (key.Equals(pair.Key))
					{
						instance = pair.Value;
						container.meshInstances.Remove(pair.Key);
						container.meshInstances[key] = instance;
						Debug.Log("!");
						return instance;
					}
				}
			}

			if (renderSurfaceType == RenderSurfaceType.Normal)
			{
				var builder = new System.Text.StringBuilder();
				builder.AppendLine("not found " + meshDescription.meshQuery.UsedVertexChannels + " " + meshDescription.geometryHashValue + " " + meshDescription.surfaceHashValue + " " + meshDescription.vertexCount + " " + meshDescription.indexCount);

				foreach (var pair in container.meshInstances)
				{
					if (key.SubMeshIndex != pair.Key.SubMeshIndex ||
						key.MeshType.GetHashCode() != pair.Key.MeshType.GetHashCode())
						continue;
					//if (key.Equals(pair.Key))//pair.Value.MeshDescription == meshDescription)
					//{
					var orgObj = ReferenceEquals(pair.Key.SurfaceObject, null) ? "NULL" : ((pair.Key.SurfaceObject) ? pair.Key.SurfaceObject.GetInstanceID().ToString() : "??");
					var newObj = ReferenceEquals(key.SurfaceObject, null) ? "NULL" : ((key.SurfaceObject) ? key.SurfaceObject.GetInstanceID().ToString() : "??");
					builder.AppendLine(key.Equals(pair.Key) + " " + pair.Key.SurfaceObject.GetType() + " "+ key.SurfaceObject.GetType() + " " + pair.Key.SurfaceObject.name + " "+ key.SurfaceObject.name);
					//}
				}

				Debug.Log(builder.ToString(), container);
			}
			*/

			instance = CreateMesh(container, meshDescription, modelSettings);
			if (!instance)
			{
				//Debug.Log("not found, could not create", container);
				return null;
			}
			
			container.meshInstances[key] = instance;
			return instance;
		}


		#region UpdateTransform
		public static void UpdateTransforms()
		{
			var models = InternalCSGModelManager.Models;
			for (var i = 0; i < models.Length; i++)
			{
				var model = models[i];
				if (!model)
					continue;

				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null)
					continue;

				UpdateTransform(modelCache.GeneratedMeshes);
			}
		}

		static void UpdateTransform(GeneratedMeshes container)
		{
			if (!container || !container.owner)
			{
				return;
			}

			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			// TODO: make sure outlines are updated when models move
			
			var containerTransform = container.transform;
			if (containerTransform.localPosition	!= MathConstants.zeroVector3 ||
				containerTransform.localRotation	!= MathConstants.identityQuaternion ||
				containerTransform.localScale		!= MathConstants.oneVector3)
			{
				containerTransform.localPosition	= MathConstants.zeroVector3;
				containerTransform.localRotation	= MathConstants.identityQuaternion;
				containerTransform.localScale		= MathConstants.oneVector3;
				SceneToolRenderer.SetOutlineDirty();
			}
		}
		#endregion

		#region UpdateContainerComponents
		static readonly List<GeneratedMeshInstance> __notfoundInstances		= new List<GeneratedMeshInstance>();
		static MeshInstanceKey[]					__removeMeshInstances	= new MeshInstanceKey[0];
		public static void UpdateContainerComponents(GeneratedMeshes container, HashSet<GeneratedMeshInstance> foundInstances)
		{
			if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			if (!container || !container.owner)
				return;
			
			if (container.meshInstances == null)
				ValidateContainer(container);


			__notfoundInstances.Clear();
			var instances			= container.GetComponentsInChildren<GeneratedMeshInstance>(true);
			if (foundInstances == null)
			{
				__notfoundInstances.AddRange(instances);
			} else
			{
				for (int i = 0; i < instances.Length; i++)
				{
					var instance = instances[i];
					if (!foundInstances.Contains(instance))
					{
						__notfoundInstances.Add(instance);
						continue;
					}

					var key = instance.GenerateKey();

					//if (instance.RenderSurfaceType == RenderSurfaceType.Normal)
					//	Debug.Log("register = " + instance.MeshDescription.meshQuery.UsedVertexChannels + " " + instance.MeshDescription.geometryHashValue + " " + instance.MeshDescription.surfaceHashValue + " " + instance.MeshDescription.vertexCount + " " + instance.MeshDescription.indexCount, container);
					container.meshInstances[key] = instance;
				}
			}
			
			for (int i = 0; i < __notfoundInstances.Count; i++)
			{
				var instance = __notfoundInstances[i];
				if (instance && instance.gameObject)
				{
					instance.gameObject.hideFlags = HideFlags.None;
					UnityEngine.Object.DestroyImmediate(instance.gameObject);
				}

				if (__removeMeshInstances.Length < container.meshInstances.Count)
				{
					__removeMeshInstances = new MeshInstanceKey[container.meshInstances.Count];
				}

				int removeMeshInstancesCount = 0;
				foreach(var item in container.meshInstances)
				{
					if (!item.Value ||
						item.Value == instance)
					{
						__removeMeshInstances[removeMeshInstancesCount] = item.Key;
						removeMeshInstancesCount++;
					}
				}
				if (removeMeshInstancesCount > 0)
				{
					if (removeMeshInstancesCount == container.meshInstances.Count)
					{
						//Debug.Log("clear");
						container.meshInstances.Clear();
					} else
					{
						for (int j = 0; j < removeMeshInstancesCount; j++)
						{
							//if (container.meshInstances[__removeMeshInstances[j]].RenderSurfaceType == RenderSurfaceType.Normal)
							//{
								//var meshInstance = container.meshInstances[__removeMeshInstances[j]];
								//Debug.Log("remove = " + meshInstance.MeshDescription.meshQuery.UsedVertexChannels + " " + meshInstance.MeshDescription.geometryHashValue + " " + meshInstance.MeshDescription.surfaceHashValue + " " + meshInstance.MeshDescription.vertexCount + " " + meshInstance.MeshDescription.indexCount);
							//}
							container.meshInstances.Remove(__removeMeshInstances[j]);
						}
					}
				}
			}
			 
			if (!container.owner)
				return;

			UpdateTransform(container);
		}
		#endregion
		
		#region UpdateContainerFlags
		private static void UpdateContainerFlags(GeneratedMeshes container)
		{
			if (!container)
				return;
			if (container.owner)
			{
				var ownerTransform = container.owner.transform;
				if (container.transform.parent != ownerTransform) 
				{
					container.transform.SetParent(ownerTransform, false);
				}

				if (!container)
					return;
			}

			//var isTrigger			= container.owner.IsTrigger;
			//var collidable		= container.owner.HaveCollider || isTrigger;
			var ownerStaticFlags	= GameObjectUtility.GetStaticEditorFlags(container.owner.gameObject);
			var previousStaticFlags	= GameObjectUtility.GetStaticEditorFlags(container.gameObject);
			var containerTag		= container.owner.gameObject.tag;
			var containerLayer		= container.owner.gameObject.layer;
			
			var showVisibleSurfaces	= (RealtimeCSG.CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowVisibleSurfaces) != 0;


			if (ownerStaticFlags != previousStaticFlags ||
				containerTag   != container.gameObject.tag ||
				containerLayer != container.gameObject.layer)
			{
				foreach (var meshInstance in container.meshInstances.Values)
				{
					if (!meshInstance)
						continue;

					if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal)
					{
						var gameObject = meshInstance.gameObject;
						if (gameObject.activeSelf != showVisibleSurfaces)
							gameObject.SetActive(showVisibleSurfaces);
					}

					var oldStaticFlags = GameObjectUtility.GetStaticEditorFlags(meshInstance.gameObject);
					var newStaticFlags = FilterStaticEditorFlags(oldStaticFlags, meshInstance.RenderSurfaceType);

					foreach (var transform in meshInstance.GetComponentsInChildren<Transform>(true))
					{
						var gameObject = transform.gameObject;
						if (oldStaticFlags != newStaticFlags)
							GameObjectUtility.SetStaticEditorFlags(gameObject, newStaticFlags);
						if (gameObject.tag != containerTag)
							gameObject.tag = containerTag;
						if (gameObject.layer != containerLayer)
							gameObject.layer = containerLayer;
					}
				}
			}

			if (container.owner.NeedAutoUpdateRigidBody)
				AutoUpdateRigidBody(container);
		}
		#endregion
	}
}
