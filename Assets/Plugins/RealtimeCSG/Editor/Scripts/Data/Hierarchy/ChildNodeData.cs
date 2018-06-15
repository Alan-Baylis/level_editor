#if UNITY_EDITOR
using System;
using UnityEngine;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	[Serializable]
    internal sealed class ChildNodeData
    {
		// this allows us to help detect when the operation has been modified in the hierarchy
	    public CSGOperation		Parent			= null;
	    public CSGModel			Model			= null;
		public Transform		ModelTransform	= null;
		public ParentNodeData	OwnerParentData = null; // link to parents' parentData


		public int    parentNodeID    { get { return (Parent != null) ? Parent.operationNodeID : CSGNode.InvalidNodeID; } }
		public int    modelNodeID     { get { return (Model  != null) ? Model.modelNodeID      : CSGNode.InvalidNodeID; } }
		
		public void Reset()
		{
			Parent			= null;
			Model			= null;
			OwnerParentData = null;
			ModelTransform	= null;
		}
	}
}
#endif