using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace RemoteEditor.Messages
{
    [Serializable]
    public class Message : IBinarySerializable
    {
        public List<MsgGameObject> gameObjects = new List<MsgGameObject>();
        public List<MsgMesh> meshes = new List<MsgMesh>();
        public List<MsgTexture2D> textures = new List<MsgTexture2D>();

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(gameObjects);
            writer.Write(meshes);
            writer.Write(textures);
        }

        public void Deserialize(BinaryReader reader)
        {
            gameObjects = reader.ReadSerializableList<MsgGameObject>();
            meshes = reader.ReadSerializableList<MsgMesh>();
            textures = reader.ReadSerializableList<MsgTexture2D>();
        }        
    }
}