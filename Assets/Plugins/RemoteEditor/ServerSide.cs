using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RemoteEditor.Messages;
using System.IO;

namespace RemoteEditor
{
    public class ServerSide : MonoBehaviour
    {
        private RouterSocket server;
        internal RemoteEditor remoteEditor;
        internal string address;

        private ConcurrentQueue<byte[]> queue = new ConcurrentQueue<byte[]>();

        internal Dictionary<int, Transform> remoteObjects = new Dictionary<int, Transform>();
        internal Dictionary<int, Mesh> remoteMeshes = new Dictionary<int, Mesh>();
        internal Dictionary<int, Texture2D> remoteTextures = new Dictionary<int, Texture2D>();

        internal void Setup(RemoteEditor remoteEditor, string address)
        {
            this.remoteEditor = remoteEditor;
            this.address = address;
        }

        internal async Task ServerAsync()
        {
            try
            {
                HashSet<RoutingKey> clients = new HashSet<RoutingKey>();

                using (server = new RouterSocket(address.Split(':').First() + "://127.0.0.1:" + address.Split(':').Last()))
                {
                    while (remoteEditor.isRunning)
                    {
                        var (routingKey, more) = await server.ReceiveRoutingKeyAsync();

                        if (!clients.Contains(routingKey))
                        {
                            clients.Add(routingKey);
                            var (message, _) = await server.ReceiveFrameStringAsync();
                            if (message == RemoteEditor.HelloMessage)
                            {
                                Debug.Log($"Received from {routingKey} frame : {message}");
                                server.SendMoreFrame(routingKey);
                                server.SendFrame(RemoteEditor.WelcomeMessage);
                            }
                        }
                        else
                        {
                            var (message, _) = await server.ReceiveFrameBytesAsync();
                            queue.Enqueue(message);
                        }
                    }

                    //for (int i = 0; i < 1000; i++)
                    //{
                    //    var (routingKey, more) = await server.ReceiveRoutingKeyAsync();
                    //    var (message, _) = await server.ReceiveFrameStringAsync();

                    //    // TODO: process message
                    //    Debug.Log("Server received: " + message);

                    //    await Task.Delay(100);
                    //    server.SendMoreFrame(routingKey);
                    //    server.SendFrame("Welcome");
                    //}
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
        }

        private void FixedUpdate()
        {
            while (remoteEditor.isRunning && queue.TryDequeue(out byte[] msgJson))
            {
                Message msg = BinaryWriterExtensions.Deserialize<Message>(msgJson);
                HashSet<int> allIds = remoteObjects.Keys.ToHashSet();

                //meshes
                foreach (var msgMesh in msg.meshes)
                {
                    if (!remoteMeshes.ContainsKey(msgMesh.instanceID))
                    {
                        Mesh mesh = msgMesh.Create();

                        if (mesh)
                            remoteMeshes.Add(msgMesh.instanceID, mesh);
                    }
                }

                //textures
                foreach (var msgTex in msg.textures)
                {
                    if (!remoteTextures.ContainsKey(msgTex.instanceID))
                    {
                        Texture2D tex = msgTex.Create();
                        
                        if (tex)
                            remoteTextures.Add(msgTex.instanceID, tex);
                    }
                }

                //game objects
                foreach (var mgo in msg.gameObjects)
                {
                    allIds.Remove(mgo.instanceID);

                    Transform t;
                    GameObject go;
                    if (!remoteObjects.TryGetValue(mgo.instanceID, out t))
                    {
                        go = new GameObject(mgo.name);
                        t = go.transform;
                        remoteObjects.Add(mgo.instanceID, t);
                    }
                    else
                    {
                        go = t.gameObject;
                    }

                    mgo.ApplyAllComponents(this, t, go);

                    if (mgo.parentInstanceID != 0 && remoteObjects.ContainsKey(mgo.parentInstanceID))
                        t.parent = remoteObjects[mgo.parentInstanceID];
                    else
                        t.parent = null;

                    if (go.activeSelf != mgo.activeSelf)
                        go.SetActive(mgo.activeSelf);

                    t.position = mgo.position;
                    t.rotation = mgo.rotation;
                    t.localScale = mgo.localScale;
                }

                foreach (int idToRemove in allIds)
                {
                    Destroy(remoteObjects[idToRemove].gameObject);
                    remoteObjects.Remove(idToRemove);
                }
            }

        }
    }
}