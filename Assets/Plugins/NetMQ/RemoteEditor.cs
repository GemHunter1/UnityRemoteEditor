using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class RemoteEditor : MonoBehaviour
{
    [Flags]
    public enum ServerFlags
    {
        None = 0,
        Server = 1,
        Client = 2
    }
    public ServerFlags Mode;

    public bool isRunning = false;
    public CancellationTokenSource cts = new CancellationTokenSource();

    public const string HelloMessage = "Hello";
    public const string WelcomeMessage = "Welcome";

    private ServerSide serverSide;
    private ClientSide clientSide;

    void Start()
    {
        DontDestroyOnLoad(gameObject);

        List<Task> tasks = new List<Task>();

        Debug.Log("Starting NetMQ");

        AsyncIO.ForceDotNet.Force();

        if (Mode.HasFlag(ServerFlags.Server))
        {
            serverSide = gameObject.AddComponent<ServerSide>();
            serverSide.remoteEditor = this;
        }
        if (Mode.HasFlag(ServerFlags.Client))
        {
            clientSide = gameObject.AddComponent<ClientSide>();
            clientSide.remoteEditor = this;
        }

        Task.Factory.StartNew(() => 
        {
            using (var runtime = new NetMQRuntime())
            {
                isRunning = true;

                if (Mode.HasFlag(ServerFlags.Server))
                {
                    tasks.Add(serverSide.ServerAsync());
                }
                if (Mode.HasFlag(ServerFlags.Client))
                {
                    tasks.Add(clientSide.ClientAsync());
                }

                runtime.Run(cts.Token, tasks.ToArray());
            }
        }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private void OnDestroy()
    {
        isRunning = false;
        cts.Cancel();
        NetMQConfig.Cleanup(false);
    }
}