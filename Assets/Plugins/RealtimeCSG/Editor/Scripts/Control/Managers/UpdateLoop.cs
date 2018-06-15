using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	[InitializeOnLoad]
	internal sealed class UpdateLoop
	{
		[MenuItem("Edit/Realtime-CSG/Turn Realtime-CSG on or off %F3", false, 30)]
		static void ToggleRealtimeCSG()
		{
			RealtimeCSG.CSGSettings.SetRealtimeCSGEnabled(!RealtimeCSG.CSGSettings.EnableRealtimeCSG);
		}

		public static bool IsActive() { return (editor != null && editor.initialized); }


		static UpdateLoop editor = null;
		static UpdateLoop()
		{
			if (editor != null)
			{
				editor.Shutdown();
				editor = null;
			}
			editor = new UpdateLoop();
			editor.Initialize();
		}

		bool initialized = false;
		bool had_first_update = false;

		void Initialize()
		{
			if (initialized)
				return;

			CSGKeysPreferenceWindow.ReadKeys();

			initialized = true;
			
			CSGSceneManagerRedirector.Interface = new CSGSceneManagerInstance();
			
			Selection.selectionChanged					-= OnSelectionChanged;
			Selection.selectionChanged					+= OnSelectionChanged;

			//Debug.Log("Register OnFirstUpdate");
			EditorApplication.update					-= OnFirstUpdate;
			EditorApplication.update					+= OnFirstUpdate;

#if UNITY_2018_1_OR_NEWER
			EditorApplication.hierarchyChanged	-= OnHierarchyWindowChanged;
			EditorApplication.hierarchyChanged	+= OnHierarchyWindowChanged;
#else
			EditorApplication.hierarchyWindowChanged	-= OnHierarchyWindowChanged;
			EditorApplication.hierarchyWindowChanged	+= OnHierarchyWindowChanged;
#endif

			EditorApplication.hierarchyWindowItemOnGUI	-= HierarchyWindowItemGUI.OnHierarchyWindowItemOnGUI;
			EditorApplication.hierarchyWindowItemOnGUI	+= HierarchyWindowItemGUI.OnHierarchyWindowItemOnGUI;
			
			UnityCompilerDefineManager.UpdateUnityDefines();
		}
		

		void Shutdown(bool finalizing = false)
		{
			if (editor != this)
				return;

			editor = null;
			CSGSceneManagerRedirector.Interface = null;
			if (!initialized)
				return;

			//Debug.Log("Remove OnFirstUpdate");
			EditorApplication.update					-= OnFirstUpdate;

#if UNITY_2018_1_OR_NEWER
			EditorApplication.hierarchyChanged	-= OnHierarchyWindowChanged;
#else
			EditorApplication.hierarchyWindowChanged	-= OnHierarchyWindowChanged;
#endif
			EditorApplication.hierarchyWindowItemOnGUI	-= HierarchyWindowItemGUI.OnHierarchyWindowItemOnGUI;
			SceneView.onSceneGUIDelegate				-= SceneViewEventHandler.OnScene;
			Undo.undoRedoPerformed						-= UndoRedoPerformed;

			initialized = false;

			// make sure the C++ side of things knows to clear the method pointers
			// so that we don't accidentally use them while closing unity
			NativeMethodBindings.ClearUnityMethods();
			NativeMethodBindings.ClearExternalMethods();

			if (!finalizing)
				SceneToolRenderer.Cleanup();
		}

		static Scene currentScene;
		internal static void UpdateOnSceneChange()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			var activeScene = SceneManager.GetActiveScene();
			if (currentScene != activeScene)
			{
				if (editor == null)
					ResetUpdateRoutine();

				editor.OnSceneUnloaded();
				currentScene = activeScene;
				InternalCSGModelManager.InitOnNewScene();
			}
		}

		void OnSceneUnloaded()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			if (this.initialized)
				this.Shutdown();
			
			MeshInstanceManager.Shutdown();
			InternalCSGModelManager.Shutdown();

			editor = new UpdateLoop();
			editor.Initialize();
		}

		public static void EnsureFirstUpdate()
		{
			if (editor == null)
				return;
			//Debug.Log("EnsureFirstUpdate");
			if (!editor.had_first_update)
				editor.OnFirstUpdate();
		}

		void OnHierarchyWindowChanged()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			SceneDragToolManager.UpdateDragAndDrop();
			InternalCSGModelManager.UpdateHierarchy();
		}  

		void UndoRedoPerformed()
		{
			InternalCSGModelManager.UndoRedoPerformed();
		}

		// Delegate for generic updates
		void OnFirstUpdate()
		{
			//Debug.Log("OnFirstUpdate");

			had_first_update = true;
			EditorApplication.update -= OnFirstUpdate;
			RealtimeCSG.CSGSettings.Reload();
			
			// register unity methods in the c++ code so that some unity functions
			// (such as debug.log) can be called from within the c++ code.
			NativeMethodBindings.RegisterUnityMethods();

			// register dll methods so we can use them
			NativeMethodBindings.RegisterExternalMethods();
			
			RunOnce();
			//CreateSceneChangeDetector();
		}
		
		void RunOnce()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				// when you start playing the game in the editor, it'll call 
				// RunOnce before playing the game, but not after.
				// so we need to wait until the game has stopped, after which we'll 
				// run first update again.
				EditorApplication.update -= OnWaitUntillStoppedPlaying;
				EditorApplication.update += OnWaitUntillStoppedPlaying;
				return;
			}
			
			SceneView.onSceneGUIDelegate -= SceneViewEventHandler.OnScene;
			SceneView.onSceneGUIDelegate += SceneViewEventHandler.OnScene;
			Undo.undoRedoPerformed		 -= UndoRedoPerformed;
			Undo.undoRedoPerformed		 += UndoRedoPerformed;
			
//			InternalCSGModelManager.UpdateHierarchy();
			
			var scene = SceneManager.GetActiveScene();	
			var allGeneratedMeshes = SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshes>(scene);
			for (int i = 0; i < allGeneratedMeshes.Count; i++)
			{
				if (!allGeneratedMeshes[i].owner)
					UnityEngine.Object.DestroyImmediate(allGeneratedMeshes[i].gameObject);
			}


			// we use a co-routine for updates because EditorApplication.update
			// works at a ridiculous rate and the co-routine is only fired in the
			// editor when something has happened.
			ResetUpdateRoutine();
		}

		void OnWaitUntillStoppedPlaying()
		{
			if (!EditorApplication.isPlaying)
			{
				EditorApplication.update -= OnWaitUntillStoppedPlaying;

				//Debug.Log("Register OnFirstUpdate");
				EditorApplication.update -= OnFirstUpdate;	
				EditorApplication.update += OnFirstUpdate;
			}
		}
		
		static void RunEditorUpdate()
		{
			if (!RealtimeCSG.CSGSettings.EnableRealtimeCSG)
				return;

			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			UpdateLoop.UpdateOnSceneChange();
		
			try
			{
				ColorSettings.Update();
				InternalCSGModelManager.CheckForChanges(forceHierarchyUpdate: false);
				TooltipUtility.CleanCache();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		public static void ResetUpdateRoutine()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			if (editor != null &&
				!editor.initialized)
			{
				editor = null;
			}
			if (editor == null)
			{
				editor = new UpdateLoop();
				editor.Initialize();
			}

			//Debug.Log("ResetUpdateRoutine");
			EditorApplication.update -= RunEditorUpdate;
			EditorApplication.update += RunEditorUpdate;
			InternalCSGModelManager.skipCheckForChanges = false;
		}


		static void OnSelectionChanged()
		{
			EditModeManager.UpdateSelection();
		}
	}
}