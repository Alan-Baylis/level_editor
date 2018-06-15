using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;


namespace InternalRealtimeCSG
{
#if UNITY_EDITOR
	public struct MeshInstanceKey : IEqualityComparer<MeshInstanceKey>, IEquatable<MeshInstanceKey> 
	{
		public static MeshInstanceKey GenerateKey(RealtimeCSG.Foundation.GeneratedMeshDescription meshDescription)
		{
			return new MeshInstanceKey(meshDescription.meshQuery, meshDescription.surfaceParameter, meshDescription.subMeshQueryIndex);
		}

		private MeshInstanceKey(RealtimeCSG.Foundation.MeshQuery meshType, int surfaceParameter, int subMeshIndex)
		{
			SubMeshIndex		= subMeshIndex;
			MeshType			= meshType;
			SurfaceParameter	= surfaceParameter;
		}

		public readonly int  SubMeshIndex;
		public int			 SurfaceParameter;
		public readonly RealtimeCSG.Foundation.MeshQuery MeshType;

		#region Comparison
		public override int GetHashCode()
		{
			var hash1 = SubMeshIndex     .GetHashCode();
			var hash2 = SurfaceParameter .GetHashCode();
			var hash3 = MeshType		 .GetHashCode();
			var hash = hash1;
			hash *= 389 + hash2;
			hash *= 397 + hash3;

			return hash + (hash1 ^ hash2 ^ hash3) + (hash1 + hash2 + hash3) + (hash1 * hash2 * hash3);
		}

		public int GetHashCode(MeshInstanceKey obj)
		{
			return obj.GetHashCode();
		}

		public bool Equals(MeshInstanceKey other)
		{
			if (System.Object.ReferenceEquals(this, other))
				return true;
			if (System.Object.ReferenceEquals(other, null))
				return false;
			return SubMeshIndex == other.SubMeshIndex &&
				   SurfaceParameter == other.SurfaceParameter &&
				   MeshType == other.MeshType;
		}

		public override bool Equals(object obj)
		{
			if (System.Object.ReferenceEquals(this, obj))
				return true;
			if (!(obj is MeshInstanceKey))
				return false;
			MeshInstanceKey other = (MeshInstanceKey)obj;
			if (System.Object.ReferenceEquals(other, null))
				return false;
			return SubMeshIndex == other.SubMeshIndex &&
				   SurfaceParameter == other.SurfaceParameter &&
				   MeshType == other.MeshType;
		}

		public bool Equals(MeshInstanceKey left, MeshInstanceKey right)
		{
			if (System.Object.ReferenceEquals(left, right))
				return true;
			if (System.Object.ReferenceEquals(left, null) ||
				System.Object.ReferenceEquals(right, null))
				return false;
			return left.SubMeshIndex == right.SubMeshIndex &&
				   left.SurfaceParameter == right.SurfaceParameter &&
				   left.MeshType == right.MeshType;
		}

		public static bool operator ==(MeshInstanceKey left, MeshInstanceKey right)
		{
			if (System.Object.ReferenceEquals(left, right))
				return true;
			if (System.Object.ReferenceEquals(left, null) ||
				System.Object.ReferenceEquals(right, null))
				return false;
			return left.SubMeshIndex == right.SubMeshIndex &&
				   left.SurfaceParameter == right.SurfaceParameter &&
				   left.MeshType == right.MeshType;
		}

		public static bool operator !=(MeshInstanceKey left, MeshInstanceKey right)
		{
			if (System.Object.ReferenceEquals(left, right))
				return false;
			if (System.Object.ReferenceEquals(left, null) ||
				System.Object.ReferenceEquals(right, null))
				return true;
			return left.SubMeshIndex != right.SubMeshIndex ||
				   left.SurfaceParameter != right.SurfaceParameter ||
				   left.MeshType != right.MeshType;
		}
		#endregion
	}

	[Serializable]
	public enum RenderSurfaceType
	{
		Normal,
		[FormerlySerializedAs("Discarded")] Hidden,	// manually hidden by user
		[FormerlySerializedAs("Invisible")] Culled, // removed by CSG process
		ShadowOnly,									// surface that casts shadows
		Collider,
		Trigger,
		CastShadows,								// surface that casts shadows
		ReceiveShadows								// surface that receive shadows
	}
#endif

	[DisallowMultipleComponent]
	[ExecuteInEditMode]
	[System.Reflection.Obfuscation(Exclude = true)]
	public sealed class GeneratedMeshInstance : MonoBehaviour
	{
		[HideInInspector] public float Version = 1.00f;
#if UNITY_EDITOR
		public Mesh					SharedMesh;
		public Material				RenderMaterial;
		public PhysicMaterial		PhysicsMaterial;
		public RenderSurfaceType	RenderSurfaceType = (RenderSurfaceType)999;

		public RealtimeCSG.Foundation.GeneratedMeshDescription MeshDescription;

		[HideInInspector] public bool   HasGeneratedNormals = false;
		[HideInInspector] public bool	HasUV2				= false;
		[HideInInspector] public float	ResetUVTime			= float.PositiveInfinity;
		[HideInInspector] public Int64	LightingHashValue;

		[NonSerialized] [HideInInspector] public bool Dirty	= true;
		[NonSerialized] [HideInInspector] public MeshCollider	CachedMeshCollider;
		[NonSerialized] [HideInInspector] public MeshFilter		CachedMeshFilter;
		[NonSerialized] [HideInInspector] public MeshRenderer	CachedMeshRenderer;
		[NonSerialized] [HideInInspector] public System.Object	CachedMeshRendererSO;

		public MeshInstanceKey GenerateKey()
		{
			return MeshInstanceKey.GenerateKey(MeshDescription);
		}

		internal void Awake()
		{
			// cannot change visibility since this might have an effect on exporter
			this.gameObject.hideFlags = HideFlags.DontSaveInBuild;
			this.hideFlags = HideFlags.DontSaveInBuild;

			// InstanceIDs are not properly remembered across domain reloads,
			//	this causes issues on, for instance, first startup of Unity. 
			//	So we need to refresh the instanceIDs
			if      (!ReferenceEquals(RenderMaterial,  null)) { if (RenderMaterial)  MeshDescription.surfaceParameter = RenderMaterial .GetInstanceID(); }
			else if (!ReferenceEquals(PhysicsMaterial, null)) { if (PhysicsMaterial) MeshDescription.surfaceParameter = PhysicsMaterial.GetInstanceID(); }
		}
#else
		void Awake() 
		{
			this.hideFlags = HideFlags.DontSaveInBuild;
		}
#endif
	}
}