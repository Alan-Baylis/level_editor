using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RealtimeCSG.Foundation;

namespace RealtimeCSG
{
	[Serializable]
	internal sealed class CSGOperationCache
	{
        public ParentNodeData	ParentData	= new ParentNodeData();
        public ChildNodeData	ChildData	= new ChildNodeData();

		// this allows us to detect if our operation has been modified
        public CSGOperationType	PrevOperation	= (CSGOperationType)0xff;
		public bool             PrevPassThrough = false;
		
		public void Reset()
		{
			ParentData.Reset();
			ChildData.Reset();
		}
	}
}
