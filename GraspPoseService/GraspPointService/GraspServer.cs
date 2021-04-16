using System;
using System.Collections.Generic;
using System.Threading;
using MMIStandard;
using MMICSharp.Common;
using Thrift.Server;
using Thrift.Protocol;
using Thrift.Transport;


namespace GraspPointService
{
    /// <summary>
    /// Class representation of a buffered transport factory
    /// </summary>
    class BufferedTransportFactory : TTransportFactory
    {
        public override TTransport GetTransport(TTransport trans)
        {
            return new TBufferedTransport(trans);
        }
    }

    /// <summary>
    /// A Server which handles the thrift communication
    /// </summary>
    public class GraspServer : IDisposable
    {
        /// <summary>
        /// The thread pool server which is hosted
        /// </summary>
        private TThreadPoolServer server;
        private readonly int port;

        /// <summary>
        /// The utilized interface implementation
        /// </summary>
        private readonly MGraspPoseService.Iface implementation;



        /// <summary>
        /// Constructor to create a new server
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="implementation"></param>
        public GraspServer(int port, MGraspPoseService.Iface implementation)
        {
            this.port = port;
            this.implementation = implementation;
        }
        /// <summary>
        /// Method starts the server
        /// </summary>
        public void Start()
        {
            MGraspPoseService.Processor processor = new MGraspPoseService.Processor(implementation);

            TServerTransport serverTransport = new TServerSocket(this.port);

            //Use a multithreaded server
            this.server = new TThreadPoolServer(processor, serverTransport, new BufferedTransportFactory(), new TCompactProtocol.Factory());

            Console.WriteLine($"Starting the server at {this.port}");

            //Start the server in a new thread
            // ThreadPool.QueueUserWorkItem(delegate
            // {
            this.server.Serve();
            // });
        }

        /// <summary>
        /// Disposes the server
        /// </summary>
        public void Dispose()
        {
            try
            {
                this.server.Stop();
            }
            catch (Exception)
            {
            }
        }
    }
}
