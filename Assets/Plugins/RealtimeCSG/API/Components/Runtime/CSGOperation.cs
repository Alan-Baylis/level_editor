using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.Serialization;

namespace RealtimeCSG.Components
{
	/// <summary>Holds a CSG tree branch</summary>
	/// <remarks>The CSG branch that defines a CSGOperation is defined by its child [UnityEngine.GameObject](https://docs.unity3d.com/ScriptReference/GameObject.html)s.</remarks>
#if UNITY_EDITOR
	[AddComponentMenu("CSG/Operation")]
    [ExecuteInEditMode, DisallowMultipleComponent, System.Reflection.Obfuscation(Exclude = true)]
#endif
	public sealed class CSGOperation : CSGNode
    {
		const float LatestVersion = 1.0f;
		/// <value>The version number of this instance of a <see cref="CSGOperation" /></value>
		[HideInInspector] public float Version = LatestVersion;

		/// <value>The CSG operation to perform with this CSG branch</value>
        public Foundation.CSGOperationType OperationType = Foundation.CSGOperationType.Additive;
#if UNITY_EDITOR

		public bool IsRegistered { get { return operationNodeID != CSGNode.InvalidNodeID; } }

		[HideInInspector][NonSerialized] public Int32	operationNodeID	= CSGNode.InvalidNodeID;
		[FormerlySerializedAs("selectOnChild")]
        [HideInInspector][SerializeField] public bool	HandleAsOne		= false;
        [HideInInspector][SerializeField] internal bool	passThrough		= false;
		[HideInInspector][NonSerialized] public object	cache;
		public bool PassThrough
		{
			get { return passThrough; }
			set
			{
				if (passThrough == value)
					return;
				OnDisable();
				passThrough = value;
				OnEnable();
				if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnPassthroughChanged(this);
			}
		}
        
        void OnApplicationQuit()			{ CSGSceneManagerRedirector.Interface.OnApplicationQuit(); }

        // register ourselves with our scene manager
        void Awake()
		{
			// cannot change visibility since this might have an effect on exporter
			this.hideFlags |= HideFlags.DontSaveInBuild;
			this.gameObject.tag = "EditorOnly";
			if (CSGSceneManagerRedirector.Interface != null)
			{
				CSGSceneManagerRedirector.Interface.OnCreated(this);
			}
		}
        public void OnEnable()				{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnEnabled(this); }

        // unregister ourselves from our scene manager
        public void OnDisable()				{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnDisabled(this); }
        void OnDestroy()					{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDestroyed(this); }
        
		// detect if this node has been moved within the hierarchy
		void OnTransformParentChanged()		{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnTransformParentChanged(this); }

		// called when any value of this brush has been modified from within the inspector / or recompile
		void OnValidate()					{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnValidate(this); }
		
		public void EnsureInitialized()		{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.EnsureInitialized(this); }
#else
        void Awake()
		{
			this.hideFlags = HideFlags.DontSaveInBuild;
			this.gameObject.tag = "EditorOnly"; 
		}
#endif
    }
}

