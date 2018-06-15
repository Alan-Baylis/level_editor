using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal partial class InternalCSGModelManager
	{
		private static bool    _isHierarchyModified    = true;
		
		public static void UpdateHierarchy()
		{
			_isHierarchyModified = true;
		}


		#region GetParentData
		public static ParentNodeData GetParentData(ChildNodeData childNode)
		{
			var parent = childNode.Parent;
			if (System.Object.Equals(parent, null) || !parent || !parent.transform)
			{
				var model = childNode.Model;
				if (System.Object.Equals(model, null) || !model || model.modelNodeID == CSGNode.InvalidNodeID || !model.transform)
					return null;
				var modelCache = model.cache as CSGModelCache;
				if (modelCache == null)
				{
					model.modelNodeID = CSGNode.InvalidNodeID;
					InternalCSGModelManager.RegisterModel(model);
				}
				return modelCache.ParentData;
			}
			if (parent.operationNodeID == CSGNode.InvalidNodeID)
				return null;
			
			return (parent.cache as CSGOperationCache).ParentData;
		}
		#endregion

		#region UpdateChildList
		struct TreePosition
		{
			public TreePosition(HierarchyItem _item) { item = _item; index = 0; }
			public HierarchyItem item;
			public int index;
		}

		static Int32[] UpdateChildList(HierarchyItem top)
		{
			var ids = new List<int>();
			{
				var parents = new List<TreePosition>
				{
					new TreePosition(top)
				};
				while (parents.Count > 0)
				{
					var parent		= parents[parents.Count - 1];
					parents.RemoveAt(parents.Count - 1);
					var children	= parent.item.ChildNodes;
					for (var i = parent.index; i < children.Length; i++)
					{
						var node = children[i];
						var nodeID = node.NodeID;
						if (nodeID == CSGNode.InvalidNodeID)
						{
							var operation = node.Transform ? node.Transform.GetComponent<CSGOperation>() : null;
							if (operation)
							{
								if (operation.operationNodeID != CSGNode.InvalidNodeID)
								{
									nodeID = node.NodeID = operation.operationNodeID;
								}
							}
							if (nodeID == CSGNode.InvalidNodeID)
							{
								if (node.ChildNodes.Length > 0)
								{
									var next_index = i + 1;
									if (next_index < children.Length)
									{
										parent.index = next_index;
										parents.Add(parent);
									}
									parents.Add(new TreePosition(node));
									break;
								}
								continue;
							}
						}
						ids.Add(nodeID);
						if (node.PrevSiblingIndex != node.SiblingIndex)
						{
							External.SetDirty(nodeID);
							node.PrevSiblingIndex = node.SiblingIndex;
						}
					}
				}
			}
			return ids.ToArray();
		}
		#endregion
		
		#region CheckTransformChanged

		internal const int BrushCheckChunk = 3000;
		internal static int BrushCheckPos = 0;

		public static void CheckTransformChanged(bool checkAllBrushes = false)
		{
			if (External == null)
			{
				return;
			}
			
			for (int i = 0; i < Operations.Count; i++)
			{
				var operation = Operations[i];
				if (!Operations[i]) continue;
				
				var operationCache = operation.cache as CSGOperationCache;
				if ((int)operationCache.PrevOperation != (int)operation.OperationType)
				{
					operationCache.PrevOperation = operation.OperationType;
					External.SetOperationOperationType(operation.operationNodeID,
													   operationCache.PrevOperation);
				}
			}

			for (int i = 0; i < Models.Length; i++)
			{
				var model = Models[i];
				if (!model) continue;

				if (!model.cachedTransform) model.cachedTransform = model.transform;
			}
			
			for (int brushIndex = 0; brushIndex < Brushes.Count; brushIndex++)
			{
				var brush		= Brushes[brushIndex];
				if (System.Object.ReferenceEquals(brush, null) || !brush)
					continue;

				var brushNodeID = brush.brushNodeID;
				// make sure it's registered, otherwise ignore it
				if (brushNodeID == CSGNode.InvalidNodeID)
					continue;
				
				var brushCache	= brush.cache as CSGBrushCache;
				if (brushCache == null) continue;
				
				var brushTransform				= brushCache.hierarchyItem.Transform;
				var currentLocalToWorldMatrix	= brushTransform.localToWorldMatrix;					
				var prevTransformMatrix			= brushCache.compareTransformation.localToWorldMatrix;
				if (prevTransformMatrix.m00 != currentLocalToWorldMatrix.m00 ||
					prevTransformMatrix.m01 != currentLocalToWorldMatrix.m01 ||
					prevTransformMatrix.m02 != currentLocalToWorldMatrix.m02 ||
					prevTransformMatrix.m03 != currentLocalToWorldMatrix.m03 ||

					prevTransformMatrix.m10 != currentLocalToWorldMatrix.m10 ||
					prevTransformMatrix.m11 != currentLocalToWorldMatrix.m11 ||
					prevTransformMatrix.m12 != currentLocalToWorldMatrix.m12 ||
					prevTransformMatrix.m13 != currentLocalToWorldMatrix.m13 ||

					prevTransformMatrix.m20 != currentLocalToWorldMatrix.m20 ||
					prevTransformMatrix.m21 != currentLocalToWorldMatrix.m21 ||
					prevTransformMatrix.m22 != currentLocalToWorldMatrix.m22 ||
					prevTransformMatrix.m23 != currentLocalToWorldMatrix.m23)
				{
					var modelTransform = brushCache.ChildData.Model.transform;
					brushCache.compareTransformation.localToWorldMatrix = currentLocalToWorldMatrix;
					brushCache.compareTransformation.brushToModelSpaceMatrix = modelTransform.worldToLocalMatrix * 
						brushCache.compareTransformation.localToWorldMatrix;
					
					var localToModelMatrix = brushCache.compareTransformation.brushToModelSpaceMatrix;
					External.SetBrushToModelSpace(brushNodeID, localToModelMatrix);

					if (brush.ControlMesh != null)
						brush.ControlMesh.Generation = brushCache.controlMeshGeneration + 1;
				}
				
				if (brush.OperationType != brushCache.prevOperation ||
					brush.ContentLayer != brushCache.prevContentLayer)
				{
					brushCache.prevOperation = brush.OperationType;
					brushCache.prevContentLayer = brush.ContentLayer;

					External.SetBrushOperationType(brushNodeID,
												   brush.OperationType);
				}

				if (brush.ControlMesh == null)
				{
					brush.ControlMesh = ControlMeshUtility.EnsureValidControlMesh(brush);
					if (brush.ControlMesh == null)
						continue;
					
					brushCache.controlMeshGeneration = brush.ControlMesh.Generation;
					ControlMeshUtility.RebuildShape(brush);
				} else
				if (brushCache.controlMeshGeneration != brush.ControlMesh.Generation)
				{
					brushCache.controlMeshGeneration = brush.ControlMesh.Generation;
					ControlMeshUtility.RebuildShape(brush);
				}
			}

			for (var i = 0; i < Models.Length; i++)
			{
				var model = Models[i];
				if (!model)
					continue;

				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null)
					continue;

				MeshInstanceManager.ValidateModel(model, modelCache);
			}

			MeshInstanceManager.UpdateTransforms();
		}
		#endregion

		
		// for a given transform, try to to find the first transform parent that is a csg-node
		#region FindParentTransform
		public static Transform FindParentTransform(Transform childTransform)
		{
			var iterator	= childTransform.parent;
			if (!iterator)
				return null;
			
			var currentNodeObj = iterator.GetComponent(TypeConstants.CSGNodeType);
			if (currentNodeObj)
			{
				var brush = currentNodeObj as CSGBrush;
				if (brush)
				{
					var brushCache = InternalCSGModelManager.GetBrushCache(brush);
					return ((!brushCache.ChildData.Parent) ? ((!brushCache.ChildData.Model) ? null : brushCache.ChildData.Model.transform) : brushCache.ChildData.Parent.transform);
				}
			}
			while (iterator)
			{
				currentNodeObj = iterator.GetComponent(TypeConstants.CSGNodeType);
				if (currentNodeObj)
					return iterator;

				iterator = iterator.parent;
			}
			return null;
		}
		#endregion

		// for a given transform, try to to find the model transform
		#region FindParentTransform
		public static Transform FindModelTransform(Transform childTransform)
		{
			var currentNodeObj = childTransform.GetComponent(TypeConstants.CSGNodeType);
			if (currentNodeObj)
			{
				var model = currentNodeObj as CSGModel;
				if (model)
					return null;

				var brush = currentNodeObj as CSGBrush;
				if (brush)
				{
					var brushCache = InternalCSGModelManager.GetBrushCache(brush);
					if (brushCache == null)
						return null;
					return (brushCache.ChildData.Model == null) ? null : brushCache.ChildData.Model.transform;
				}

				var operation = currentNodeObj as CSGOperation;
				if (operation && !operation.PassThrough)
				{
					var operationCache = InternalCSGModelManager.GetOperationCache(operation);
					if (operationCache == null)
						return null;
					return (operationCache.ChildData.Model == null) ? null : operationCache.ChildData.Model.transform;
				}
			}
			
			var iterator = childTransform.parent;
			while (iterator)
			{
				var currentModelObj = iterator.GetComponent(TypeConstants.CSGModelType);
				if (currentModelObj)
					return currentModelObj.transform; 

				iterator = iterator.parent;
			}
			return null;
		}
		#endregion

		#region FindParentNode
		static void FindParentNode(Transform opTransform, out CSGNode parentNode, out Transform parentTransform)
		{
			var iterator = opTransform.parent;
			while (iterator)
			{
				var currentNodeObj = iterator.GetComponent(TypeConstants.CSGNodeType);
				if (currentNodeObj)
				{
					var operationNode = currentNodeObj as CSGOperation;
					if (!operationNode ||
						!operationNode.PassThrough)
					{
						parentNode = currentNodeObj as CSGNode;
						parentTransform = iterator;
						return;
					}
				}

				iterator = iterator.parent;
			}
			parentTransform = null;
			parentNode = null;
		}
		#endregion

		#region FindParentOperation
		static void FindParentOperation(Transform opTransform, out CSGOperation parentOp)
		{
			if (opTransform)
			{
				var iterator = opTransform.parent;
				while (iterator)
				{
					var currentNodeObj = iterator.GetComponent(TypeConstants.CSGOperationType);
					if (currentNodeObj)
					{
						var currentParent = currentNodeObj as CSGOperation;
						if (!currentParent.PassThrough)
						{
							parentOp = currentParent;
							return;
						}
					}

					iterator = iterator.parent;
				}
			}
			parentOp = null;
		}
		#endregion

		#region FindParentModel
		static void FindParentModel(Transform opTransform, out CSGModel parentModel)
		{
			if (!opTransform)
			{
				parentModel = null;
				return;
			}

			var iterator = opTransform.parent;
			while (iterator)
			{
				var currentNodeObj = iterator.GetComponent(TypeConstants.CSGModelType);
				if (currentNodeObj)
				{
					parentModel = currentNodeObj as CSGModel;
					return;
				}

				iterator = iterator.parent;
			}
			parentModel = GetDefaultCSGModelForObject(opTransform);
		}
		#endregion

		#region FindParentOperationAndModel
		static void FindParentOperationAndModel(Transform opTransform, out CSGOperation parentOp, out CSGModel parentModel)
		{
			parentOp = null;
			parentModel = null;
			if (!opTransform)
				return;
			var iterator = opTransform.parent;
			while (iterator)
			{
				var currentNodeObj = iterator.GetComponent(TypeConstants.CSGNodeType);
				if (currentNodeObj)
				{
					parentModel = currentNodeObj as CSGModel;
					if (parentModel)
					{
						parentOp = null;
						return;
					}

					var tempParentOp = currentNodeObj as CSGOperation;
					if (tempParentOp)
					{
						if (tempParentOp.operationNodeID != CSGNode.InvalidNodeID && !tempParentOp.PassThrough)
						{
							parentOp = tempParentOp;
							break;
						}
					}
				}

				iterator = iterator.parent;
			}
			
			while (iterator)
			{
				var currentNodeObj = iterator.GetComponent(TypeConstants.CSGModelType);
				if (currentNodeObj)
				{
					parentModel = currentNodeObj as CSGModel;
					return;
				}

				iterator = iterator.parent;
			}
			parentModel = GetDefaultCSGModelForObject(opTransform);
		}
		#endregion
		


		#region InitializeHierarchy
		static void InitializeHierarchy(ChildNodeData childData, Transform childTransform)
		{
			Transform parentTransform;
			CSGNode parentNode;
			FindParentNode(childTransform, out parentNode, out parentTransform);
			if (!parentNode)
			{
				parentTransform = childTransform.root;
				childData.Model = GetDefaultCSGModelForObject(childTransform);
				childData.Parent = null;
			} else
			{
				// maybe our parent is a model?
				var model = parentNode as CSGModel;
				if (model)
				{
					childData.Model = model;
				} else
				{
					// is our parent an operation?
					var operation = parentNode as CSGOperation;
					if (operation &&
						!operation.PassThrough)
					{
						childData.Parent = operation;
					}

					// see if our parent has already found a model
					if (childData.Parent)
					{
						var operationCache = InternalCSGModelManager.GetOperationCache(childData.Parent);
						if (operationCache != null &&
							operationCache.ChildData != null)
							childData.Model = operationCache.ChildData.Model;
					}
				}

				// haven't found a model?
				if (!childData.Model)
				{
					// if not, try higher up in the hierarchy ..
					FindParentModel(parentTransform, out childData.Model);
				}
			}
		}

		static void InitializeHierarchy(CSGBrushCache brushCache, Transform brushTransform)
		{
			var currentModel	 = brushCache.ChildData.Model;
			var modelTransform	 = (!currentModel) ? null : currentModel.transform;
			brushCache.compareTransformation.EnsureInitialized(brushTransform, modelTransform);			
		}
		#endregion

		#region UpdateChildrenParent
		static void UpdateChildrenParent(CSGOperation parent, Transform container, bool forceSet = false)
		{
			if (External == null)
			{
				return;
			}
			for (int i = 0; i < container.childCount; i++)
			{
				var child = container.GetChild(i);
				var nodeObj = child.GetComponent(TypeConstants.CSGNodeType);
				if (nodeObj)
				{
					var op = nodeObj as CSGOperation;
					if (op &&
						!op.PassThrough)
					{
						// make sure the node has already been initialized, otherwise
						// assume it'll still get initialized at some point, in which
						// case we shouldn't update it's hierarchy here
						if (!op.IsRegistered)
							continue;

						var operationCache = InternalCSGModelManager.GetOperationCache(op);
						if (operationCache != null &&
							operationCache.ChildData != null)
						{
							if ((forceSet || operationCache.ChildData.Parent != parent) &&
								operationCache.ChildData.Model)     // assume we're still initializing
							{
								SetCSGOperationHierarchy(op, parent, operationCache.ChildData.Model);
							}
						}
						continue;
					}

					var brush = nodeObj as CSGBrush;
					if (brush)
					{
						// make sure the node has already been initialized, otherwise
						// assume it'll still get initialized at some point, in which
						// case we shouldn't update it's hierarchy here
						if (!brush.IsRegistered)
							continue;

						var brushCache = InternalCSGModelManager.GetBrushCache(brush);
						if (brushCache != null &&
							brushCache.ChildData != null)
						{
							if ((forceSet || brushCache.ChildData.Parent != parent) &&
								brushCache.ChildData.Model)	// assume we're still initializing
							{
								SetCSGBrushHierarchy(brush, parent, brushCache.ChildData.Model);
							}
						}
						continue;
					}
				}
				UpdateChildrenParent(parent, child);
			}
		}
		#endregion

		#region UpdateChildrenModel
		static void UpdateChildrenModel(CSGModel model, Transform container)
		{
			if (External == null)
			{
				return;
			}
			if (model == null)
				return;

			for (int i = 0; i < container.childCount; i++)
			{
				var child = container.GetChild(i);
				var nodeObj = child.GetComponent(TypeConstants.CSGNodeType);
				if (nodeObj)
				{
					var op = nodeObj as CSGOperation;
					if (op && 
						!op.PassThrough)
					{
						// make sure the node has already been initialized, otherwise
						// assume it'll still get initialized at some point, in which
						// case we shouldn't update it's hierarchy here
						if (!op.IsRegistered)
							continue;

						var operation_cache = InternalCSGModelManager.GetOperationCache(op);
						if (operation_cache.ChildData.Model != model)
						{
							if (model) // assume we're still initializing
							{
								SetCSGOperationHierarchy(op, operation_cache.ChildData.Parent, model);
							}
						} else
						{
							// assume that if this operation already has the 
							// correct model, then it's children will have the same model
							break;
						}
					}
					

					var brush = nodeObj as CSGBrush;
					if (brush)
					{
						// make sure the node has already been initialized, otherwise
						// assume it'll still get initialized at some point, in which
						// case we shouldn't update it's hierarchy here
						if (!brush.IsRegistered)
							continue;

						var brushCache = InternalCSGModelManager.GetBrushCache(brush);
						if (brushCache.ChildData.Model != model)
						{
							if (model) // assume we're still initializing
							{
								SetCSGBrushHierarchy(brush, brushCache.ChildData.Parent, model);
								InternalCSGModelManager.CheckSurfaceModifications(brush);
							}
						} else
						{
							// assume that if this brush already has the 
							// correct model, then it's children will have the same model
							break;
						}
					}
				}
				UpdateChildrenModel(model, child);
			}
		}
		#endregion

		#region OnOperationTransformChanged
		public static void OnOperationTransformChanged(CSGOperation op)
		{
			// unfortunately this event is sent before it's destroyed, so we need to defer it.
			OperationTransformChanged.Add(op);
		}
		#endregion

		#region OnBrushTransformChanged
		public static void OnBrushTransformChanged(CSGBrush brush)
		{
			// unfortunately this event is sent before it's destroyed, so we need to defer it.
			BrushTransformChanged.Add(brush);
		}
		#endregion
		
		#region SetNodeParent
		static void SetNodeParent(ChildNodeData childData, HierarchyItem hierarchyItem, CSGOperation parentOp, CSGModel parentModel)
		{
			var oldParentData = childData.OwnerParentData;

			childData.Parent = parentOp;
			childData.Model  = parentModel;			
			var newParentData = GetParentData(childData); 
			if (oldParentData != newParentData)
			{
				if (oldParentData != null) oldParentData.RemoveNode(hierarchyItem);
				if (newParentData != null) newParentData.AddNode(hierarchyItem);
				childData.OwnerParentData = newParentData;
				childData.ModelTransform = (!childData.Model) ? null : childData.Model.transform;
			}
		}
		#endregion
		
		#region SetCSGOperationHierarchy
		static void SetCSGOperationHierarchy(CSGOperation op, CSGOperation parentOp, CSGModel parentModel)
		{
			var operationCache = InternalCSGModelManager.GetOperationCache(op);
			SetNodeParent(operationCache.ChildData, operationCache.ParentData, parentOp, parentModel);
/*			
			if (!operationCache.ChildData.Model)
				return;
			
			External.SetOperationHierarchy(op.operationNodeID,
										   operationCache.ChildData.modelNodeID,
										   operationCache.ChildData.parentNodeID);*/
		}
		#endregion

		#region CheckOperationHierarchy
		static void CheckOperationHierarchy(CSGOperation op)
		{
			if (External == null)
			{
				return;
			}

			if (!op || !op.gameObject.activeInHierarchy)
			{
				return;
			}

			// make sure the node has already been initialized, 
			// otherwise ignore it
			if (!op.IsRegistered)
			{
				return;
			}

			// NOTE: returns default model when it can't find parent model
			CSGModel parentModel;
			CSGOperation parentOp;
			FindParentOperationAndModel(op.transform, out parentOp, out parentModel);

			var operationCache = InternalCSGModelManager.GetOperationCache(op);
			if (operationCache.ChildData.Parent == parentOp &&
				operationCache.ChildData.Model == parentModel)
			{
				return;
			}

			SetCSGOperationHierarchy(op, parentOp, parentModel);
		}
		#endregion

		#region SetCSGBrushHierarchy
		static void SetCSGBrushHierarchy(CSGBrush brush, CSGOperation parentOp, CSGModel parentModel)
		{
			var brushCache = InternalCSGModelManager.GetBrushCache(brush);
			SetNodeParent(brushCache.ChildData, brushCache.hierarchyItem, parentOp, parentModel);
/*
			if (!brushCache.childData.Model)
				return;

			//Debug.Log("SetCSGBrushHierarchy");
			External.SetBrushHierarchy(brush.brushNodeID,
									   brushCache.childData.modelNodeID,
									   brushCache.childData.parentNodeID);*/
		}
		#endregion

		#region CheckSiblingPosition
		static void CheckSiblingPosition(CSGBrush brush)
		{
			if (!brush || !brush.gameObject.activeInHierarchy)
				return;

			// NOTE: returns default model when it can't find parent model
			CSGModel parentModel;
			CSGOperation parentOp;
			FindParentOperationAndModel(brush.transform, out parentOp, out parentModel);
			if (!parentOp)
				return;

			var brushCache = InternalCSGModelManager.GetBrushCache(brush);
			if (brushCache.ChildData.Parent != parentOp || 
				brushCache.ChildData.Model != parentModel)
				return;

			var operationCache = InternalCSGModelManager.GetOperationCache(parentOp);
			var parentData = operationCache.ParentData;
			ParentNodeData.UpdateNodePosition(brushCache.hierarchyItem, parentData);
		}
		#endregion

		#region CheckBrushHierarchy
		static void CheckBrushHierarchy(CSGBrush brush)
		{
			if (External == null)
			{
				return;
			}

			if (!brush || !brush.gameObject.activeInHierarchy)
			{
				if (!brush && brush.IsRegistered)
					OnDestroyed(brush);
				return;
			}

			// make sure the node has already been initialized, 
			// otherwise ignore it
			if (!brush.IsRegistered)
			{
				return;
			}

			if (RemovedBrushes.Contains(brush.brushNodeID))
			{
				return;
			}

			// NOTE: returns default model when it can't find parent model
			CSGModel parentModel;
			CSGOperation parentOp;
			FindParentOperationAndModel(brush.transform, out parentOp, out parentModel);

			var brushCache = InternalCSGModelManager.GetBrushCache(brush);
			if (brushCache.ChildData.Parent == parentOp &&
				brushCache.ChildData.Model  == parentModel)
			{
				if (parentOp)
				{ 
					var operationCache	= InternalCSGModelManager.GetOperationCache(parentOp);
					if (operationCache != null)
					{
						var parentData	= operationCache.ParentData;
						ParentNodeData.UpdateNodePosition(brushCache.hierarchyItem, parentData);
						return;
					}
				}
			}
			
			SetCSGBrushHierarchy(brush, parentOp, parentModel);
		}
		#endregion


		internal static double hierarchyValidateTime = 0.0;
		internal static double updateHierarchyTime = 0.0;

		public static void OnHierarchyModified()
		{
			if (External == null)
			{
				return;
			}

			HierarchyItem.CurrentLoopCount = (HierarchyItem.CurrentLoopCount + 1);

			UpdateRegistration();

			var startTime = EditorApplication.timeSinceStartup;
			if (OperationTransformChanged.Count > 0)
			{
				//Debug.Log(OperationTransformChanged.Count);
				foreach (var item in OperationTransformChanged)
				{
					if (item) CheckOperationHierarchy(item);
				}
				OperationTransformChanged.Clear();
			}


			if (BrushTransformChanged.Count > 0)
			{
				//Debug.Log(BrushTransformChanged.Count);
				foreach (var item in BrushTransformChanged)
				{
					if (!item)
						continue;

					CheckBrushHierarchy(item);
					ValidateBrush(item); // to detect material changes when moving between models
				}
				BrushTransformChanged.Clear();
			}
			hierarchyValidateTime = EditorApplication.timeSinceStartup - startTime;


			// remove all nodes that have been scheduled for removal
			if (RemovedBrushes   .Count > 0)
			{
				//Debug.Log(RemovedBrushes.Count);
				External.DestroyNodes(RemovedBrushes   .ToArray()); RemovedBrushes   .Clear();
			}
			if (RemovedOperations.Count > 0)
			{
				//Debug.Log(RemovedOperations.Count);
				External.DestroyNodes(RemovedOperations.ToArray()); RemovedOperations.Clear();
			}
			if (RemovedModels    .Count > 0)
			{
				//Debug.Log(RemovedModels.Count);
				External.DestroyNodes(RemovedModels    .ToArray()); RemovedModels    .Clear();
			}
			

			for (var i = Brushes.Count - 1; i >= 0; i--)
			{
				var item = Brushes[i];
				if (item && item.brushNodeID != CSGNode.InvalidNodeID)
					continue;

				UnregisterBrush(item);
			}
			
			for (var i = Operations.Count - 1; i >= 0; i--)
			{
				var item = Operations[i];
				if (!item || item.operationNodeID == CSGNode.InvalidNodeID)
				{
					UnregisterOperation(item);
					continue;
				}

				var cache = GetOperationCache(item);
				if (!cache.ParentData.Transform)
					cache.ParentData.Init(item, item.operationNodeID);
			}

			for (var i = Models.Length - 1; i >= 0; i--)
			{
				var item = Models[i];
				if (!item || item.modelNodeID == CSGNode.InvalidNodeID)
				{
					UnregisterModel(item);
					continue;
				}

				var cache = GetModelCache(item);
				if (!cache.ParentData.Transform)
					cache.ParentData.Init(item, item.modelNodeID);
			}

			startTime = EditorApplication.timeSinceStartup;
			for (var i = Operations.Count - 1; i >= 0; i--)
			{
				var item = Operations[i];
				if (!item || item.operationNodeID == CSGNode.InvalidNodeID)
					continue;
					
				var itemCache = item.cache as CSGOperationCache;
				var parentData = itemCache.ChildData.OwnerParentData;// GetParentData(item_cache.childData);
				if (parentData == null)
					continue;

				ParentNodeData.UpdateNodePosition(itemCache.ParentData, parentData);
				/*if (!)
					continue;
					
				External.SetOperationHierarchy(item.operationNodeID,
												itemCache.ChildData.modelNodeID,
												itemCache.ChildData.parentNodeID);*/
			}

			for (var i = Brushes.Count - 1; i >= 0; i--)
			{
				var item = Brushes[i];
				if (!item || item.brushNodeID == CSGNode.InvalidNodeID)
					continue;

				var itemCache	= item.cache as CSGBrushCache;
				var parentData	= itemCache.ChildData.OwnerParentData;
				if (parentData == null)
					continue;
				
				ParentNodeData.UpdateNodePosition(itemCache.hierarchyItem, parentData);
				/*
				if (!)
					continue;

				External.SetBrushHierarchy(item.brushNodeID,
											itemCache.childData.modelNodeID,
											itemCache.childData.parentNodeID);*/
			}

			if (External.SetChildNodes != null)
			{
				for (var i = 0; i < Operations.Count; i++)
				{
					var item = Operations[i];
					if (!item || item.operationNodeID == CSGNode.InvalidNodeID)
						continue;
					
					var itemCache = item.cache as CSGOperationCache;
					if (!itemCache.ParentData.ChildrenModified)
						continue;
					
					var childList = UpdateChildList(itemCache.ParentData);

					/*
					var builder = new System.Text.StringBuilder();
					builder.Append("operation " + (item.operationNodeID-1) + " SetChildNodes (");
					for (int c=0;c<childList.Length;c++)
					{
						if (c>0)
							builder.Append(", ");
						builder.Append(childList[c]-1);
					}
					builder.Append(")");
					Debug.Log(builder.ToString());
					*/

					External.SetChildNodes(item.operationNodeID, childList.Length, childList);
					itemCache.ParentData.ChildrenModified = false;
				}

				for (var i = 0; i < Models.Length; i++)
				{
					var item = Models[i];
					if (!item || item.modelNodeID == CSGNode.InvalidNodeID)
						continue;
					
					var itemCache = item.cache as CSGModelCache;
					if (!itemCache.ParentData.ChildrenModified)
						continue;
					
					var childList = UpdateChildList(itemCache.ParentData);

					/*
					var builder = new System.Text.StringBuilder();
					builder.Append("model " + (item.modelNodeID-1) + " SetChildNodes (");
					for (int c = 0; c < childList.Length; c++)
					{
						if (c > 0)
							builder.Append(", ");
						builder.Append(childList[c]-1);
					}
					builder.Append(")");
					Debug.Log(builder.ToString());
					*/

					External.SetChildNodes(item.modelNodeID, childList.Length, childList);
					itemCache.ParentData.ChildrenModified = false;
				}
			}

			updateHierarchyTime = EditorApplication.timeSinceStartup - startTime;
		}
	}
}