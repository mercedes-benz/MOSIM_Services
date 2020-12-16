// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Janis Sprenger

using System;
using MMIStandard;
using Thrift.Server;
using Thrift.Protocol;
using Thrift.Transport;

namespace RetargetingServiceServer
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
    public class MMIRetargetingThriftServer : IDisposable
    {
        /// <summary>
        /// The thread pool server which is hosted
        /// </summary>
        private TThreadPoolServer server;
        private readonly int port;

        /// <summary>
        /// The utilized interface implementation
        /// </summary>
        private readonly MRetargetingService.Iface implementation;

      

        /// <summary>
        /// Constructor to create a new server
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="implementation"></param>
        public MMIRetargetingThriftServer(int port, MRetargetingService.Iface implementation)
        {
            this.port = port;
            this.implementation = implementation;
        }
        /// <summary>
        /// Method starts the server
        /// </summary>
        public void Start()
        {
            MRetargetingService.Processor processor = new MRetargetingService.Processor(implementation);

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
