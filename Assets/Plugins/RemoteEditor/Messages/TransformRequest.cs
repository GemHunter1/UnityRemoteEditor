using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RemoteEditor.Messages
{
    public class TransformRequest : IBinarySerializable
    {
        public int gameObjectInstanceID;

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;

        public TransformRequest() { }

        public TransformRequest(Transform t, int remoteInstanceID)
        {
            this.gameObjectInstanceID = remoteInstanceID;
            this.position = t.position;
            this.rotation = t.rotation;
            this.localScale = t.localScale;
        }

        public void Apply(Transform t)
        {
            t.position = this.position;
            t.rotation = this.rotation;
            t.localScale = this.localScale;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(gameObjectInstanceID);
            writer.Write(position);
            writer.Write(rotation);
            writer.Write(localScale);
        }

        public void Deserialize(BinaryReader reader)
        {
            gameObjectInstanceID = reader.ReadInt32();
            position = reader.ReadVector3();
            rotation = reader.ReadQuaternion();
            localScale = reader.ReadVector3();
        }

#if UNITY_EDITOR
        private static RemoteInstanceID cachedRemoteInstanceID;

        static TransformRequest()
        {
            SceneView.duringSceneGui += OnScene;
        }

        private static bool isMouseDown = false;

        private static void OnScene(SceneView sceneView)
        {
            Event e = Event.current;

            if (e.button == 0)
            {
                if (e.type == EventType.MouseDown)
                {
                    isMouseDown = true;
                    hasChangedWhileMouseDown = false;
                }
                else if (e.type == EventType.MouseUp)
                {
                    isMouseDown = false;
                    hasChangedWhileMouseDown = false;
                }
            }
        }

        private static bool hasChangedWhileMouseDown = false;

        public static bool IsChanging()
        {
            if (Selection.activeTransform && (Selection.activeTransform.hasChanged || hasChangedWhileMouseDown))
            {
                if (isMouseDown)
                    hasChangedWhileMouseDown = true;

                return true;
            }

            return false;
        }

        public static bool GetChanges(out TransformRequest request)
        {
            request = null;

            if (Selection.activeTransform && (Selection.activeTransform.hasChanged || hasChangedWhileMouseDown))
            {
                if (!cachedRemoteInstanceID || cachedRemoteInstanceID.transform != Selection.activeTransform)
                    cachedRemoteInstanceID = Selection.activeTransform.gameObject.GetComponent<RemoteInstanceID>();

                if (!cachedRemoteInstanceID)
                    return false;

                if (isMouseDown)
                    hasChangedWhileMouseDown = true;

                request = new TransformRequest(Selection.activeTransform, cachedRemoteInstanceID.instanceID);
                return true;
            }
            return false;
        }
#else
        public static bool GetChanges(out TransformRequest request)
        {
            request = null;
            return false;
        }
#endif

#if UNITY_EDITOR
        public static void ResetTransformChangesMark()
        {
            if (Selection.activeTransform)
            {
                Selection.activeTransform.hasChanged = false;
            }
        }
#else
        public static void ResetTransformChangesMark() { }
#endif
    }
}
