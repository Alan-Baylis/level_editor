using System;
using UnityEngine;
using RealtimeCSG.Legacy;
using InternalRealtimeCSG;

namespace RealtimeCSG.Components
{
	/// <summary>Holds a CSG tree brush</summary>
#if UNITY_EDITOR
	[AddComponentMenu("CSG/Brush")]
	[ExecuteInEditMode, System.Reflection.Obfuscation(Exclude = true)]
#endif
	public sealed partial class CSGBrush : CSGNode
	{
		const float LatestVersion = 2.0f;
		/// <value>The version number of this instance of a <see cref="CSGBrush" /></value>
		[HideInInspector] public float Version = LatestVersion;
		
		/// <value>The CSG operation to perform with this brush</value>
		[SerializeField] public Foundation.CSGOperationType	OperationType	= Foundation.CSGOperationType.Additive;

		/// <value>The <see cref="Shape"/> that defines the shape by this brush together with its <see cref="ControlMesh"/>.</value>
		/// <remarks><note>This will be replaced by <see cref="RealtimeCSG.Foundation.BrushMesh"/> eventually</note></remarks>
		[SerializeField] public Shape				Shape;
		
		/// <value>The <see cref="ControlMesh"/> that defines the shape by this brush together with its <see cref="Shape"/>.</value>
		/// <remarks><note>This will be replaced by <see cref="RealtimeCSG.Foundation.BrushMesh"/> eventually</note></remarks>
		[SerializeField] public ControlMesh			ControlMesh;

#if UNITY_EDITOR
		[SerializeField] public uint				ContentLayer;

		public bool IsRegistered { get { return brushNodeID != CSGNode.InvalidNodeID; } }
		
		[HideInInspector][SerializeField] public BrushFlags	flags = BrushFlags.None;
#if !TEST_ENABLED
		[HideInInspector][NonSerialized]
#endif
		public Int32		brushNodeID  = CSGNode.InvalidNodeID;
		[HideInInspector][NonSerialized] public Color?		outlineColor;
		[HideInInspector][NonSerialized] public object		cache;
		
		void OnApplicationQuit()		{ CSGSceneManagerRedirector.Interface.OnApplicationQuit(); }

		// register ourselves with our scene manager
		void Awake()
		{
			// cannot change visibility since this might have an effect on exporter
			this.hideFlags |= HideFlags.DontSaveInBuild;
			this.gameObject.tag = "EditorOnly";
			this.brushNodeID = CSGNode.InvalidNodeID;
			if (CSGSceneManagerRedirector.Interface != null)
				CSGSceneManagerRedirector.Interface.OnCreated(this);
		}

		void OnEnable()					{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnEnabled(this); }

		// unregister ourselves from our scene manager
		void OnDisable()				{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDisabled(this); }
		void OnDestroy()				{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDestroyed(this); }

		// detect if this node has been moved within the hierarchy
		void OnTransformParentChanged()	{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnTransformParentChanged(this); }

		// called when any value of this brush has been modified from within the inspector
		void OnValidate()				{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnValidate(this); }
		
		public void EnsureInitialized()
		{
			if (CSGSceneManagerRedirector.Interface != null)
				CSGSceneManagerRedirector.Interface.EnsureInitialized(this);			
		}

		public void CheckVersion()
		{
			if (Version >= LatestVersion)
				return;

			if (Version < 1.0f)
				Version = 1.0f;

			if (Version == 1.0f)
			{
#pragma warning disable 618 // Type is now obsolete
				if (Shape.Materials != null && Shape.Materials.Length > 0)
				{
					// update textures
					if (Shape.TexGens != null)
					{
						for (int i = 0; i < Shape.TexGens.Length; i++)
						{ 
							Shape.TexGens[i].RenderMaterial = null;
						}
						
#pragma warning disable 618 // Type is now obsolete
						for (int i = 0; i < Mathf.Min(Shape.Materials.Length, Shape.TexGens.Length); i++) 
						{
#pragma warning disable 618 // Type is now obsolete
							Shape.TexGens[i].RenderMaterial = Shape.Materials[i];
						}

						for (int i = 0; i < Shape.TexGenFlags.Length; i++)
						{
							var oldFlags			= (int)Shape.TexGenFlags[i];
							var isWorldSpaceTexture	= (oldFlags & 1) == 1;

							var isNotVisible		= (oldFlags & 2) == 2;
							var isNoCollision		= isNotVisible;
							var isNotCastingShadows	= ((oldFlags & 4) == 0) && !isNotVisible;

							TexGenFlags newFlags = (TexGenFlags)0;
							if (isNotVisible)		 newFlags |= TexGenFlags.NoRender;
							if (isNoCollision)		 newFlags |= TexGenFlags.NoCollision;
							if (isNotCastingShadows) newFlags |= TexGenFlags.NoCastShadows;
							if (isWorldSpaceTexture) newFlags |= TexGenFlags.WorldSpaceTexture;
						} 
					}
				}

			}

			Version = LatestVersion;
		}
#else
		void Awake()
		{
			this.hideFlags = HideFlags.DontSaveInBuild;
			this.gameObject.tag = "EditorOnly"; 
		}
#endif
	}
}
