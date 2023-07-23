using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace RemoteEditor.Messages
{
    public class MsgGameObject : IBinarySerializable
    {
        public string name;
        public int instanceID;
        public int parentInstanceID;

        public bool activeSelf;

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;

        public List<MsgComponent> components = new List<MsgComponent>();

        public MsgGameObject() { }

        public MsgGameObject(Transform t)
        {
            name = t.name;
            instanceID = t.GetInstanceID();
            parentInstanceID = t.parent == null ? 0 : t.parent.GetInstanceID();

            activeSelf = t.gameObject.activeSelf;

            position = t.position;
            rotation = t.rotation;
            localScale = t.localScale;

            if (t.gameObject.TryGetComponent(out MeshFilter meshFilter))
                components.Add(new MsgMeshFilter(meshFilter));

            if (t.gameObject.TryGetComponent(out MeshRenderer meshRenderer))
                components.Add(new MsgMeshRenderer(meshRenderer));
        }

        public T GetComponent<T>() where T : MsgComponent
        {
            return (T)components.FirstOrDefault(x => x is T);
        }

        public bool TryGetComponent<T>(out T component) where T : MsgComponent, new()
        {
            component = (T)components.FirstOrDefault(x => x is T);
            return component != null;
        }

        public void ApplyComponent<T>(ServerSide server, Transform t, GameObject go) where T : MsgComponent, new()
        {
            if (TryGetComponent<T>(out var comp))
                comp.ApplyComponent(server, t, go);
        }

        public void ApplyAllComponents(ServerSide server, Transform t, GameObject go)
        {
            foreach (var comp in components)
                comp.ApplyComponent(server, t, go);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(name);
            writer.Write(instanceID);
            writer.Write(parentInstanceID);

            writer.Write(activeSelf);

            writer.Write(position);
            writer.Write(rotation);
            writer.Write(localScale);

            writer.Write(components);
        }

        public void Deserialize(BinaryReader reader)
        {
            name = reader.ReadString();
            instanceID = reader.ReadInt32();
            parentInstanceID = reader.ReadInt32();

            activeSelf = reader.ReadBoolean();

            position = reader.ReadVector3();
            rotation = reader.ReadQuaternion();
            localScale = reader.ReadVector3();

            components = reader.ReadSerializableList(MsgComponent.Deserializer);
        }
    }
}
