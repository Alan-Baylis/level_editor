using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal partial class InternalCSGModelManager
	{ 
		internal static void ClearCaches()
		{
			foreach (var brush in Brushes)
				CreateBrushCache(brush);
			
			foreach (var operation in Operations)
				CreateOperationCache(operation);
			
			foreach (var model in Models)
				CreateModelCache(model);
		}

		public static CSGBrushCache CreateBrushCache(CSGBrush brush)
		{
			var brushCache = new CSGBrushCache
			{
				controlMeshGeneration = (brush.ControlMesh != null) ? brush.ControlMesh.Generation - 1 : -1
			};
			brush.cache = brushCache;
			return brushCache;
		}

		public static CSGBrushCache GetBrushCache(CSGBrush brush)
		{
			if (System.Object.Equals(brush, null) || !brush || brush.brushNodeID == CSGNode.InvalidNodeID)
				return null;

			var brushCache = brush.cache as CSGBrushCache;
			if (brushCache != null)
				return brushCache;

			return CreateBrushCache(brush);
		}

		public static CSGOperationCache CreateOperationCache(CSGOperation operation)
		{
			var operationCache = new CSGOperationCache();
			operation.cache = operationCache;
			return operationCache;
		}

		public static CSGOperationCache GetOperationCache(CSGOperation operation, bool forceRecreate = false)
		{
			if (System.Object.Equals(operation, null) || !operation || operation.operationNodeID == CSGNode.InvalidNodeID)
				return null;

			if (!forceRecreate)
			{
				var operationCache = operation.cache as CSGOperationCache;
				if (operationCache != null)
					return operationCache;
			}
			
			return CreateOperationCache(operation);
		}
		
		public static CSGModelCache CreateModelCache(CSGModel model)
		{
			var modelCache = new CSGModelCache();
			model.cache = modelCache;
			return modelCache;
		}

		public static CSGModelCache GetModelCache(CSGModel model)
		{
			if (System.Object.Equals(model, null) || !model || model.modelNodeID == CSGNode.InvalidNodeID)
				return null;
			
			var modelCache = model.cache as CSGModelCache;
			if (modelCache != null)
				return modelCache;
			
			return CreateModelCache(model);
		}
	}
}