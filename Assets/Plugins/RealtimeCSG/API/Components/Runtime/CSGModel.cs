using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.Rendering;

namespace RealtimeCSG.Components
{
#if UNITY_EDITOR
	[Flags, Serializable]
	public enum ModelSettingsFlags
	{
//		ShadowCastingModeFlags	= 1|2|4,
//		DoNotReceiveShadows		= 8,
		DoNotRender				= 16,
		NoCollider				= 32,
		IsTrigger				= 64,
		InvertedWorld			= 128,
		SetColliderConvex		= 256,
		AutoUpdateRigidBody		= 512,
		PreserveUVs             = 1024,
		AutoRebuildUVs			= 2048,
		NoAutoRebuildColliders	= 4096,
		IgnoreNormals			= 8192
	}

	[Serializable]
	public enum ExportType
	{
		FBX,
		UnityMesh
	}

	[Serializable]
	public enum OriginType
	{
		ModelCenter,
		ModelPivot,
		WorldSpace
	}
#endif
	
	/// <summary>Holds a CSG tree and generates meshes for that CSG tree</summary>
	/// <remarks>The CSG tree that defines a model is defined by its child [UnityEngine.GameObject](https://docs.unity3d.com/ScriptReference/GameObject.html)s.</remarks>
#if UNITY_EDITOR
	[AddComponentMenu("CSG/Model")]
	[DisallowMultipleComponent, ExecuteInEditMode, System.Reflection.Obfuscation(Exclude = true)]
#endif
	public sealed class CSGModel : CSGNode
	{
		const float LatestVersion = 1.1f;
		/// <value>The version number of this instance of a <see cref="CSGModel" /></value>
		[HideInInspector] public float Version = LatestVersion;
#if UNITY_EDITOR
//		public ShadowCastingMode	ShadowCastingModeFlags	{ get { return (ShadowCastingMode)(Settings & ModelSettingsFlags.ShadowCastingModeFlags); } }
//		public bool					ShadowsOnly				{ get { return ShadowCastingModeFlags == ShadowCastingMode.ShadowsOnly; } }
//		public bool					HasShadows				{ get { return ShadowCastingModeFlags == UnityEngine.Rendering.ShadowCastingMode.On || ShadowCastingModeFlags == UnityEngine.Rendering.ShadowCastingMode.TwoSided; } }
//		public bool					ReceiveShadows			{ get { return (Settings & ModelSettingsFlags.DoNotReceiveShadows) == (ModelSettingsFlags)0; } }
		public bool					IsRenderable			{ get { return (Settings & ModelSettingsFlags.DoNotRender) == (ModelSettingsFlags)0; } }
		public bool					HaveCollider			{ get { return (Settings & ModelSettingsFlags.NoCollider) == (ModelSettingsFlags)0; } }
		public bool					IsTrigger				{ get { return (Settings & ModelSettingsFlags.IsTrigger) != (ModelSettingsFlags)0; } }
		public bool					InvertedWorld			{ get { return (Settings & ModelSettingsFlags.InvertedWorld) != (ModelSettingsFlags)0; } }
		public bool					SetColliderConvex		{ get { return (Settings & ModelSettingsFlags.SetColliderConvex) != (ModelSettingsFlags)0; } }
		public bool					NeedAutoUpdateRigidBody	{ get { return (Settings & ModelSettingsFlags.AutoUpdateRigidBody) == (ModelSettingsFlags)0; } }
		public bool					PreserveUVs         	{ get { return (Settings & ModelSettingsFlags.PreserveUVs) != (ModelSettingsFlags)0; } }
		public bool					AutoRebuildUVs         	{ get { return (Settings & ModelSettingsFlags.AutoRebuildUVs) != (ModelSettingsFlags)0; } }
		public bool					AutoRebuildColliders  	{ get { return (Settings & ModelSettingsFlags.NoAutoRebuildColliders) == (ModelSettingsFlags)0; } }
		public bool					IgnoreNormals  			{ get { return (Settings & ModelSettingsFlags.IgnoreNormals) != (ModelSettingsFlags)0; } }

		[SerializeField] [EnumAsFlags] public ModelSettingsFlags	Settings				= ((ModelSettingsFlags)UnityEngine.Rendering.ShadowCastingMode.On) | ModelSettingsFlags.PreserveUVs;
		
		[SerializeField] [EnumAsFlags] public Foundation.VertexChannelFlags	VertexChannels	= Foundation.VertexChannelFlags.All;
		[SerializeField] public PhysicMaterial						DefaultPhysicsMaterial	= null;
		
		[HideInInspector] public bool								ShowGeneratedMeshes		= false;
		
		// Note: contents of UnityEditor.UnwrapParam, which is not marked serializable :(
		[HideInInspector][SerializeField] public float				angleError			= 0.08f;
		[HideInInspector][SerializeField] public float				areaError			= 0.15f;
		[HideInInspector][SerializeField] public float				hardAngle			= 88.0f;
		[HideInInspector][SerializeField] public float				packMargin			= 4.0f / 1024.0f;
		
		[HideInInspector][SerializeField] public float				scaleInLightmap		= 1.0f;
		[HideInInspector][SerializeField] public float              autoUVMaxDistance   = 0.5f;
		[HideInInspector][SerializeField] public float              autoUVMaxAngle		= 89.0f;
		[HideInInspector][SerializeField] public int                minimumChartSize    = 4;

		public bool IsRegistered { get { return modelNodeID != CSGNode.InvalidNodeID; } }

		[HideInInspector][NonSerialized] public Int32				modelNodeID			= CSGNode.InvalidNodeID;
		[HideInInspector][SerializeField] public ExportType			exportType			= ExportType.FBX;
		[HideInInspector][SerializeField] public OriginType			originType			= OriginType.ModelCenter;
		[HideInInspector][SerializeField] public bool               exportColliders     = false;
		[HideInInspector][SerializeField] public string				exportPath			= null;
		[HideInInspector][SerializeField] public Transform			cachedTransform;
		
		[HideInInspector][SerializeField] public CSGBrush			infiniteBrush		= null;
		[HideInInspector][NonSerialized] public object				cache;

		public const float minAngleError = 0.001f;
		public const float minAreaError = 0.001f;
		public const float maxAngleError = 1.000f;
		public const float maxAreaError = 1.000f;

		void OnApplicationQuit() { CSGSceneManagerRedirector.Interface.OnApplicationQuit(); }

		// register ourselves with our scene manager
		void Awake()
		{
			// cannot change visibility since this might have an effect on exporter
			this.hideFlags |= HideFlags.DontSaveInBuild;
			if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnCreated(this); //else Debug.Log("CSGSceneManagerRedirector.Interface == null");
		}
		void OnEnable()		{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnEnabled(this); }

		// unregister ourselves from our scene manager
		void OnDisable()    { if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDisabled(this); }
		void OnDestroy()    { if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDestroyed(this); }

		void OnTransformChildrenChanged() { if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnTransformChildrenChanged(this); }
		 
		
		// called when any value of this brush has been modified from within the inspector / or recompile
		// on recompile causes our data to be forgotten, yet awake isn't called
		void OnValidate()   { if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnValidate(this); }

		void Update()       { if (CSGSceneManagerRedirector.Interface != null && !IsRegistered) CSGSceneManagerRedirector.Interface.OnUpdate(this); }
		
		public void EnsureInitialized() { CSGSceneManagerRedirector.Interface.EnsureInitialized(this); }


		public void CheckVersion()
		{
			if (Version >= LatestVersion)
				return;

			if (Version < 1.0f)
				Version = 1.0f;

			if (Version == 1.0f)
			{
#if !PACKAGE_GENERATOR_ACTIVE
				UnityEditor.UnwrapParam uvGenerationSettings;
				UnityEditor.UnwrapParam.SetDefaults(out uvGenerationSettings);
				angleError = uvGenerationSettings.angleError;
				areaError  = uvGenerationSettings.areaError;
				hardAngle  = uvGenerationSettings.hardAngle;
				packMargin = uvGenerationSettings.packMargin;
#endif
				Version = 1.1f;
			}

			angleError = Mathf.Clamp(angleError, CSGModel.minAngleError, CSGModel.maxAngleError);
			areaError = Mathf.Clamp(areaError, CSGModel.minAreaError, CSGModel.maxAreaError);
			
			Version = LatestVersion;
		}
#else
		void Awake()
		{
			this.hideFlags = HideFlags.DontSaveInBuild;
		}
#endif
	}
}
