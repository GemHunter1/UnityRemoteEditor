using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace RemoteEditor.Messages
{
    public abstract class MsgComponent : IBinarySerializable
    {
        public MsgComponent() { }

        public static MsgComponent Deserializer(BinaryReader reader)
        {
            string type = reader.ReadString();
            MsgComponent msgComp;
            switch (type)
            {
                case nameof(MsgMeshFilter):
                    msgComp = new MsgMeshFilter();
                    break;
                case nameof(MsgMeshRenderer):
                    msgComp = new MsgMeshRenderer();
                    break;
                case nameof(MsgSkinnedMesh):
                    msgComp = new MsgSkinnedMesh();
                    break;
                default:
                    throw new Exception("Unknown component type: " + type);
            }
            msgComp.Deserialize(reader);
            return msgComp;
        }

        public abstract void Serialize(BinaryWriter writer);
        public abstract void Deserialize(BinaryReader reader);
        public abstract void ApplyComponent(ServerSide server, Transform t, GameObject go);
    }
}
