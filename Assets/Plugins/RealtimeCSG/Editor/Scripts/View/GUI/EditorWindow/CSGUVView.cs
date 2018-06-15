using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RealtimeCSG.Foundation;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
#if TEST_ENABLED
	sealed class CSGUVView : EditorWindow
	{
		static void OnSelectionChanged()
		{
			foreach (var window in windows)
			{
				if (window)
					window.Repaint();
			}
		}

		static void UpdateSelection()
		{
			triangles = null;
			uv2 = null;
			var obj = Selection.activeObject;
			if (obj is Mesh)
			{
				SetMesh(obj as Mesh);
			}
			if (obj is MeshFilter)
			{
				SetMesh((obj as MeshFilter).sharedMesh);
			}
			if (obj is GameObject)
			{
				var gameObject = obj as GameObject;
				if (gameObject)
				{
					var meshFilter = gameObject.GetComponent<MeshFilter>();
					if (meshFilter)
					{
						SetMesh(meshFilter.sharedMesh);
					}
					var model = gameObject.GetComponent<CSGModel>();
					if (model)
					{
						var gameObjects = CSGModelManager.GetModelMeshes(model);
						for (int i=0;i<gameObjects.Length;i++)
						{
							meshFilter = gameObjects[0].GetComponent<MeshFilter>();
							if (meshFilter)
							{
								SetMesh(meshFilter.sharedMesh);
								return;
							}
						}
					}
				}
			}
		}

		static void SetMesh(Mesh mesh)
		{
			triangles = null;
			uv2 = null;
			if (mesh == null)
				return;
			var _triangles = mesh.triangles;
			var _uv2 = mesh.uv2;
			if (_triangles != null)
				triangles = _triangles.ToArray();
			if (_uv2 != null)
				uv2 = _uv2.ToArray();
		}

		void OnDestroy()
		{
			windows.Remove(this);
		}

		static int[] triangles = null;
		static Vector2[] uv2 = null;

		static List<CSGUVView> windows = new List<CSGUVView>();

		public static void RepaintAll()
		{
			foreach (var window in windows)
			{
				if (window)
					window.Repaint();
			}
		}

		[MenuItem("Window/CSG UV view")]
		static void Create()
		{
			window = (CSGUVView)EditorWindow.GetWindow(typeof(CSGUVView), false, "UV View");
			window.autoRepaintOnSceneChange = true;
			window.wantsMouseMove = true;
			window.Show();
		}

		static CSGUVView window;
		

		void OnGUI()
		{
			if (!windows.Contains(this))
			{
				windows.Add(this);
				Selection.selectionChanged -= OnSelectionChanged;
				Selection.selectionChanged += OnSelectionChanged;
			}

			if (Event.current.type != EventType.Repaint)
				return;

			UpdateSelection();
			if (triangles == null || triangles.Length == 0 ||
				uv2 == null || uv2.Length == 0)
				return;


			Handles.BeginGUI();
			Handles.color = Color.red;
			var width = Screen.width - 40;
			var height = Screen.height - 40;
			var uvChannel = uv2;
			float minU = float.PositiveInfinity, minV = float.PositiveInfinity;
			float maxU = float.NegativeInfinity, maxV = float.NegativeInfinity;
			for (int i = 0; i < uvChannel.Length; i++)
			{
				var uv0 = uvChannel[i];
				minU = Mathf.Min(minU, uv0.x); maxU = Mathf.Max(maxU, uv0.x);
				minV = Mathf.Min(minV, uv0.y); maxV = Mathf.Max(maxV, uv0.y);
			}

			var sizeU = (maxU - minU);
			var sizeV = (maxV - minV);

			var scale = Mathf.Min(width, height) / Mathf.Max(sizeU, sizeV);

			minU -= 20 / scale;
			minV -= 20 / scale;

			for (int i = 0; i < triangles.Length; i += 3)
			{
				var index0 = triangles[i + 0];
				var index1 = triangles[i + 1];
				var index2 = triangles[i + 2];

				var uv0 = uvChannel[index0];
				var uv1 = uvChannel[index1];
				var uv2 = uvChannel[index2];

				uv0.x -= minU;
				uv1.x -= minU;
				uv2.x -= minU;

				uv0.y -= minV;
				uv1.y -= minV;
				uv2.y -= minV;

				uv0.x *= scale;
				uv1.x *= scale;
				uv2.x *= scale;

				uv0.y *= scale;
				uv1.y *= scale;
				uv2.y *= scale;

				Handles.DrawLine(uv0, uv1);
				Handles.DrawLine(uv1, uv2);
				Handles.DrawLine(uv2, uv0);
			}
			Handles.EndGUI();
		}
	}
#endif
}

