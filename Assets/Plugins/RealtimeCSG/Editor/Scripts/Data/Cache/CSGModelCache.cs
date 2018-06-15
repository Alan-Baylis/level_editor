using InternalRealtimeCSG;
using System;

namespace RealtimeCSG
{
	[Serializable]
	internal sealed class CSGModelCache
	{
		public readonly ParentNodeData	ParentData				= new ParentNodeData();

		public GeneratedMeshes	        GeneratedMeshes;
		
		public bool						ForceUpdate				= false;

		// this allows us to detect if we're enabled/disabled
		public bool						IsEnabled				= false;

		public Foundation.VertexChannelFlags VertexChannelFlags = Foundation.VertexChannelFlags.All;
		public RenderSurfaceType		RenderSurfaceType		= (RenderSurfaceType)(~0);

		public void Reset()
		{
			ParentData.Reset();
			GeneratedMeshes = null;
		}
	}
}
