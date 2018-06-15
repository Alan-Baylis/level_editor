using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;
using RealtimeCSG.Foundation;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal partial class InternalCSGModelManager
	{
		#region ClearMeshInstances
		public static void ClearMeshInstances()
		{
			MeshInstanceManager.Reset();

			for (var i = 0; i < Models.Length; i++)
			{
				var model = Models[i];
				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache != null)
					modelCache.GeneratedMeshes = null;
			}

			MeshInstanceManager.DestroyAllMeshInstances();
		}
		#endregion
		

		#region GetMeshTypesForModel
		static MeshQuery[] renderAndColliderMeshTypes = new MeshQuery[]
			{
				// MeshRenderers
				new MeshQuery(LayerUsageFlags.CastShadows,                LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.None),
				new MeshQuery(LayerUsageFlags.Renderable,					LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.RenderCastShadows,          LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.RenderReceiveShadows,       LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.RenderReceiveCastShadows,   LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),

				// MeshColliders
				new MeshQuery(LayerUsageFlags.Collidable,       parameterIndex: LayerParameterIndex.PhysicsMaterial),

#if UNITY_EDITOR
				// Helper surfaces (editor only)
				new MeshQuery(LayerUsageFlags.None,           mask: LayerUsageFlags.Renderable),	// hidden surfaces
				new MeshQuery(LayerUsageFlags.CastShadows		),
				new MeshQuery(LayerUsageFlags.ReceiveShadows	),
				new MeshQuery(LayerUsageFlags.Culled			)
#endif
			};

		static MeshQuery[] renderOnlyTypes = new MeshQuery[]
			{
				// MeshRenderers
				new MeshQuery(LayerUsageFlags.CastShadows,                 LayerUsageFlags.RenderReceiveCastShadows),
				new MeshQuery(LayerUsageFlags.Renderable,                  LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.RenderCastShadows,           LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.RenderReceiveShadows,        LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.RenderReceiveCastShadows,    LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),

#if UNITY_EDITOR
				// Helper surfaces (editor only)
				new MeshQuery(LayerUsageFlags.None,           mask: LayerUsageFlags.Renderable),	// hidden surfaces
				new MeshQuery(LayerUsageFlags.CastShadows		),
				new MeshQuery(LayerUsageFlags.ReceiveShadows	),
				new MeshQuery(LayerUsageFlags.Culled			)
#endif
			};

		readonly static MeshQuery[] colliderMeshTypes = new MeshQuery[]
			{
				// MeshColliders
				new MeshQuery(LayerUsageFlags.Collidable,    parameterIndex: LayerParameterIndex.PhysicsMaterial),

#if UNITY_EDITOR
				// Helper surfaces (editor only)
				new MeshQuery(LayerUsageFlags.None,        mask: LayerUsageFlags.Renderable),	// hidden surfaces
				new MeshQuery(LayerUsageFlags.Culled)
#endif
			};

		readonly static MeshQuery[] triggerMeshTypes = new MeshQuery[]
			{
				// MeshColliders
				new MeshQuery(LayerUsageFlags.Collidable,   parameterIndex: LayerParameterIndex.PhysicsMaterial),

#if UNITY_EDITOR
				// Helper surfaces (editor only)
				new MeshQuery(LayerUsageFlags.None,		mask: LayerUsageFlags.Renderable),	// hidden surfaces
				new MeshQuery(LayerUsageFlags.Culled)
#endif
			};

		readonly static MeshQuery[] emptyMeshTypes = new MeshQuery[]
			{
#if UNITY_EDITOR
				// Helper surfaces (editor only)
				new MeshQuery(LayerUsageFlags.None,		mask: LayerUsageFlags.Renderable),	// hidden surfaces
				new MeshQuery(LayerUsageFlags.Culled)
#endif
			};

		public static MeshQuery[] GetMeshTypesForModel(CSGModel model)
		{
			MeshQuery[] query;
			if (!model.HaveCollider)
			{ 
				if (!model.IsRenderable) query = emptyMeshTypes;
				else query = renderOnlyTypes;
			} else
			{
				if      ( model.IsTrigger	) query = triggerMeshTypes;
				else if (!model.IsRenderable) query = colliderMeshTypes;
				else                          query = renderAndColliderMeshTypes;
			}
			return query;
		}
		#endregion
		/*
		#region UpdateModelMeshTypes
		public static void UpdateModelMeshTypes(CSGModel model)
		{
			if (model)
				External.SetModelMeshTypes(model.modelNodeID, GetMeshTypesForModel(model));
		}
		#endregion
		*/

		#region UpdateModelSettings
		public static bool UpdateModelSettings()
		{
			var forceHierarchyUpdate = false;
			for (var i = 0; i < Models.Length; i++)
			{
				var model = Models[i];
				if (!model || !model.isActiveAndEnabled)
					continue;
				var modelModified = false;
				var invertedWorld = model.InvertedWorld;
				if (invertedWorld)
				{
					if (model.infiniteBrush == null)
					{
						var gameObject = new GameObject("*hidden infinite brush*");
						gameObject.hideFlags = MeshInstanceManager.ComponentHideFlags;
						gameObject.transform.SetParent(model.transform, false);

						model.infiniteBrush = gameObject.AddComponent<CSGBrush>();
						model.infiniteBrush.flags = BrushFlags.InfiniteBrush;

						modelModified = true;
					}
					if (model.infiniteBrush.transform.GetSiblingIndex() != 0)
					{
						model.infiniteBrush.transform.SetSiblingIndex(0);
						modelModified = true;
					}
				} else
				{
					if (model.infiniteBrush)
					{
						if (model.infiniteBrush.gameObject)
							UnityEngine.Object.DestroyImmediate(model.infiniteBrush.gameObject);
						model.infiniteBrush = null;
						modelModified = true;
					}
				}
				if (modelModified)
				{
					var childBrushes = model.GetComponentsInChildren<CSGBrush>();
					for (int j = 0; j < childBrushes.Length; j++)
					{
						if (!childBrushes[j] ||
							childBrushes[j].ControlMesh == null)
							continue;
						childBrushes[j].ControlMesh.Generation++;
					}
					forceHierarchyUpdate = true;
				}
			}
			return forceHierarchyUpdate;
		}
		#endregion


		#region RefreshMeshes
		public static void RefreshMeshes()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			if (skipCheckForChanges)
				return;

			lock (_lockObj)
			{
				InternalCSGModelManager.UpdateModelSettings();
				InternalCSGModelManager.UpdateMeshes();
				MeshInstanceManager.UpdateHelperSurfaceVisibility();
			}
		}
		#endregion

		#region UpdateRemoteMeshes
		public static void UpdateRemoteMeshes()
		{
			if (External != null &&
				External.UpdateAllModelMeshes != null)
				External.UpdateAllModelMeshes();
		}
		#endregion

		/*
		#region OnPostModelModified
		static MethodInfo[] modelModifiedEvents;

		[InitializeOnLoadMethod]
		static void FindModelModifiedAttributeMethods()
		{
			var foundMethods = new List<MethodInfo>();
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var allTypes = assembly.GetTypes();
				for (var i = 0; i < allTypes.Length; i++)
				{
					var allMethods = allTypes[i].GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
					for (var j = 0; j < allMethods.Length; j++)
					{
						if (Attribute.IsDefined(allMethods[j], typeof(CSGModelModifiedEventAttribute)))
						{
							var parameters = allMethods[j].GetParameters();
							if (parameters.Length != 2 ||
								parameters[0].ParameterType != typeof(CSGModel) ||
								parameters[1].ParameterType != typeof(GameObject[]))
							{
								Debug.LogWarning("Found static method with OnCSGModelModifiedAttribute, but incorrect signature. It should be:\nstatic void my_method_name(CSGModel model, GameObject[] modifiedMeshes);\nWhere my_method_name can be any name");
							}
							foundMethods.Add(allMethods[j]);
						}
					}
				}
			}
			modelModifiedEvents = foundMethods.ToArray();
		}

		static bool ignoreEvents = false;
		static void OnPostModelModified(CSGModel model, GameObject[] modifiedMeshes)
		{
			if (ignoreEvents) 
				return;
			ignoreEvents = true;
			try
			{
				for (var i = 0; i < modelModifiedEvents.Length; i++)
				{
					modelModifiedEvents[i].Invoke(null, new object[] { model, modifiedMeshes });
				}
			}
			finally
			{
				ignoreEvents = false;
			}
		}
		
//		static readonly List<GameObject>				__foundMeshInstances = new List<GameObject>();	
		#endregion
		*/

		#region UpdateMeshes


		#region DoForcedMeshUpdate
		static bool forcedUpdateRequired = false;

		public static void DoForcedMeshUpdate()
		{
			forcedUpdateRequired = true;
		}
		#endregion

		internal static int MeshGeneration = 0;

		#region GetModelMesh
		internal static double getMeshInstanceTime	= 0.0;
		internal static double getModelMeshesTime	= 0.0;
		internal static double updateMeshTime		= 0.0;
		
		const int MaxVertexCount = 65000;
		
		private static GeneratedMeshInstance GenerateMeshInstance(GeneratedMeshes meshContainer, int modelNodeID, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription)
		{			
			var indexCount		= meshDescription.indexCount;
			var vertexCount		= meshDescription.vertexCount;
			if ((vertexCount <= 0 || vertexCount > MaxVertexCount) || (indexCount  <= 0))
			{
				if (vertexCount > 0 && indexCount > 0)
				{
					Debug.LogError("Mesh has too many vertices (vertexCount > " + MaxVertexCount + ")");
				}
				return null;
			}

			GeneratedMeshInstance meshInstance;

			var startGetMeshInstanceTime = EditorApplication.timeSinceStartup;
			{
				meshInstance = MeshInstanceManager.GetMeshInstance(meshContainer, modelSettings, meshDescription);
				if (!meshInstance)
					return null;
			}
			getMeshInstanceTime += EditorApplication.timeSinceStartup - startGetMeshInstanceTime;
			if (meshDescription == meshInstance.MeshDescription && meshInstance.SharedMesh)
				return meshInstance;
			//if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal)
			//{
			//	Debug.Log("<" + meshInstance.MeshDescription.meshQuery.UsedVertexChannels + " " + meshInstance.MeshDescription.geometryHashValue + " " + meshInstance.MeshDescription.surfaceHashValue + " " + meshInstance.MeshDescription.vertexCount + " " + meshInstance.MeshDescription.indexCount + "\n" +
			//			  ">" + meshDescription.meshQuery.UsedVertexChannels + " " + meshDescription.geometryHashValue + " " + meshDescription.surfaceHashValue + " " + meshDescription.vertexCount + " " + meshDescription.indexCount);
			//}

			// create our arrays on the C# side with the correct size
			GeneratedMeshContents generatedMesh;
			var startGetModelMeshesTime = EditorApplication.timeSinceStartup;
			{
				generatedMesh = External.GetModelMesh(modelNodeID, meshDescription);
				if (generatedMesh == null)
					return null;
			}
			getModelMeshesTime += EditorApplication.timeSinceStartup - startGetModelMeshesTime;



			var startUpdateMeshTime = EditorApplication.timeSinceStartup;
			{
				MeshInstanceManager.ClearMesh(meshInstance);

				// finally, we start filling our (sub)meshes using the C# arrays
				var sharedMesh = meshInstance.SharedMesh;

				sharedMesh.vertices = generatedMesh.positions;
				if (generatedMesh.normals  != null) sharedMesh.normals	= generatedMesh.normals;
				if (generatedMesh.tangents != null) sharedMesh.tangents	= generatedMesh.tangents;
//				if (generatedMesh.colors   != null) sharedMesh.colors	= generatedMesh.colors;
				if (generatedMesh.uv0      != null) sharedMesh.uv		= generatedMesh.uv0;
			
				// fill the mesh with the given indices
				sharedMesh.SetTriangles(generatedMesh.indices, 0, false);
				sharedMesh.bounds = generatedMesh.bounds;
			}
			updateMeshTime += EditorApplication.timeSinceStartup - startUpdateMeshTime;
			
			if (meshInstance.RenderSurfaceType != RenderSurfaceType.Normal)
				meshInstance.HasGeneratedNormals = ((meshDescription.meshQuery.UsedVertexChannels & VertexChannelFlags.Normal) != 0);
			/*
			if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal)
			{ 
				if (meshInstance.MeshDescription.geometryHashValue != meshDescription.geometryHashValue)
				{
					Debug.Log(meshInstance.MeshDescription.geometryHashValue + " " + meshDescription.geometryHashValue + " " + meshInstance.LightingHashValue, meshInstance);
				}
			}*/
			meshInstance.MeshDescription	= meshDescription;
			
			return meshInstance;
		} 
		#endregion

		#region RemoveForcedUpdates
		public static void RemoveForcedUpdates()
		{
			var modelCount = Models.Length;
			if (modelCount == 0)
				return;

			for (var i = 0; i < modelCount; i++)
			{
				var model = Models[i];
				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null)
					continue;

				modelCache.ForceUpdate  = false;
			}
		}
		#endregion

		internal static uint		__prevSubMeshCount		= 0;
		internal static UInt64[]	__vertexHashValues		= null;
		internal static UInt64[]	__triangleHashValues	= null;
		internal static UInt64[]	__surfaceHashValues		= null;
		internal static Int32[]		__vertexCounts			= null;
		internal static Int32[]		__indexCounts			= null;
			
		static readonly HashSet<GeneratedMeshInstance>	__foundGeneratedMeshInstance = new HashSet<GeneratedMeshInstance>();
		static GeneratedMeshDescription[]						__meshDescriptions		= new GeneratedMeshDescription[0];


		static bool inUpdateMeshes = false;
		public static bool UpdateMeshes(System.Text.StringBuilder text = null, bool forceUpdate = false)
		{
			if (EditorApplication.isPlaying
				|| EditorApplication.isPlayingOrWillChangePlaymode)
				return false;
			
			if (inUpdateMeshes)
				return false;
			
			var unityMeshUpdates		= 0.0;
			var getMeshDescriptionTime	= 0.0;
			
			getMeshInstanceTime		= 0.0;
			getModelMeshesTime		= 0.0;
			updateMeshTime			= 0.0;

			inUpdateMeshes = true;
			try
			{
				if (External == null)
					return false;

				if (forcedUpdateRequired)
				{
					forceUpdate = true;
					forcedUpdateRequired = false;
				}

				var modelCount = Models.Length;
				if (modelCount == 0)
					return false;

				for (var i = 0; i < modelCount; i++)
				{
					var model = Models[i];
					var modelCache = InternalCSGModelManager.GetModelCache(model);
					if (modelCache == null ||
						!modelCache.IsEnabled)
						continue;

					var renderSurfaceType = ModelTraits.GetModelSurfaceType(model);
					modelCache.RenderSurfaceType = renderSurfaceType;

					if (!forceUpdate &&
						!modelCache.ForceUpdate)
						continue;
					
					External.SetDirty(model.modelNodeID);
					modelCache.ForceUpdate = false;
				}

				// update the model meshes
				if (!External.UpdateAllModelMeshes())
					return false; // nothing to do

				MeshGeneration++;

				for (var i = 0; i < modelCount; i++)
				{
					var model = Models[i];
					
					if (!(new CSGTreeNode { nodeID = model.modelNodeID }.Dirty))
						continue;
					
					var modelCache = InternalCSGModelManager.GetModelCache(model);
					if (modelCache == null ||
						!modelCache.IsEnabled)
						continue;
					
					var meshContainer = modelCache.GeneratedMeshes;
					if (!meshContainer)
						return false;
					
					EnsureInitialized(model);
					MeshInstanceManager.ValidateModel(model, modelCache);


					if (meshContainer.meshInstances == null ||
						meshContainer.meshInstances.Count == 0)
					{
						MeshInstanceManager.ValidateContainer(meshContainer);
						if (meshContainer.meshInstances == null)
							continue;
					}
					
					var modelNodeID = model.modelNodeID;
					bool needToUpdateMeshes;
					var startGetMeshDescriptionTime = EditorApplication.timeSinceStartup;
					{
						needToUpdateMeshes = External.GetMeshDescriptions(model, ref __meshDescriptions);
					}
					getMeshDescriptionTime += EditorApplication.timeSinceStartup - startGetMeshDescriptionTime;

					if (!needToUpdateMeshes)
						continue;
					
					__foundGeneratedMeshInstance.Clear();
					var startUnityMeshUpdates = EditorApplication.timeSinceStartup;
					{ 
						for (int meshIndex = 0; meshIndex < __meshDescriptions.Length; meshIndex++)
						{ 
							var meshInstance = GenerateMeshInstance(meshContainer, modelNodeID, model.Settings, __meshDescriptions[meshIndex]);
							if (meshInstance != null) __foundGeneratedMeshInstance.Add(meshInstance);
						}
					}
					unityMeshUpdates += (EditorApplication.timeSinceStartup - startUnityMeshUpdates);
					
//					if (modelModifiedEvents.Length > 0)
//					{
//						__foundMeshInstances.Clear();
//						foreach (var meshInstance in __foundGeneratedMeshInstance)
//						{
//							if (!meshInstance || !meshInstance.gameObject)
//								continue;
//							if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal)
//								__foundMeshInstances.Add(meshInstance.gameObject);
//						}
//					}

					MeshInstanceManager.UpdateContainerComponents(meshContainer, __foundGeneratedMeshInstance);

					if (forceUpdate)
					{
						foreach (var meshInstance in __foundGeneratedMeshInstance)
						{
							if (!meshInstance || !meshInstance.gameObject)
								continue;
							MeshInstanceManager.Refresh(meshInstance, model, forceUpdate: true);
						}
					}
					
//					if (modelModifiedEvents.Length > 0)
//						OnPostModelModified(model, __foundMeshInstances.ToArray());
				}
				MeshInstanceManager.UpdateHelperSurfaceVisibility(force: true);

				
				if (text != null)
				{
					text.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
										"All mesh generation {0:F} ms " +
										"+ retrieving {1:F} ms " +
										"+ Unity mesh updates {2:F} ms " +
										"+ overhead {3:F} ms. ",
										getMeshDescriptionTime * 1000, 
										getModelMeshesTime * 1000, 
										updateMeshTime * 1000,
										(unityMeshUpdates - (getModelMeshesTime + updateMeshTime)) * 1000);
				}

				return true;
			}
			finally
			{
				inUpdateMeshes = false;
			}
		}
		#endregion
	}
}