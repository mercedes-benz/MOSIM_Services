using System;
using System.Collections.Generic;
using MMIStandard;
using MMICSharp.Common;
using MMICSharp.Adapter;



namespace GraspPointService
{
    public class Program
    {

        /// The address of the thrift server
        private static MIPAddress address = new MIPAddress("127.0.0.1", 8900);

        ///The address of the register
        private static MIPAddress mmiRegisterAddress = new MIPAddress("127.0.0.1", 8900);

        /// The path of the mmus
        private static string mmuPath = "";


        public static void Main(string[] args)
        {
            //Create a new logger instance
            Logger.Instance = new Logger
            {
                //Log everything
                Level = Log_level.L_DEBUG
            };

            //Register for unhandled exceptions in the application
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;



            //Console.WriteLine(@"   ______  __ __     ___       __            __           ");
            //Console.WriteLine(@"  / ____/_/ // /_   /   | ____/ /___ _____  / /____  _____");
            //Console.WriteLine(@" / /   /_  _  __/  / /| |/ __  / __ `/ __ \/ __/ _ \/ ___/");
            //Console.WriteLine(@"/ /___/_  _  __/  / ___ / /_/ / /_/ / /_/ / /_/  __/ /    ");
            //Console.WriteLine(@"\____/ /_//_/    /_/  |_\__,_/\__,_/ .___/\__/\___/_/     ");
            //Console.WriteLine(@"                                  /_/                     ");
            //Console.WriteLine(@"_________________________________________________________________");



            //Parse the command line arguments
            if (!ParseCommandLineArguments(args))
            {
                Logger.Log(Log_level.L_ERROR, "Cannot parse the command line arguments. Closing the adapter!");
                return;
            }

            Console.WriteLine($"Adapter is reachable at: {address.Address}:{address.Port}");
            Console.WriteLine($"Register is reachable at: {mmiRegisterAddress.Address}:{mmiRegisterAddress.Port}");
            Console.WriteLine(@"_________________________________________________________________");

            Console.WriteLine("Grasp Point Determination Service");
            string sessionID = Guid.NewGuid().ToString();
            //ServiceAccess serviceAccess = new ServiceAccess(new MIPAddress("127.0.0.1", 9009), sessionID);
            ServiceAccess serviceAccess = new ServiceAccess(mmiRegisterAddress, sessionID);
            var reg = serviceAccess.RegisterService;
            try
            {
                //int servicePort = 8886;
                GraspPointService handler = new GraspPointService(address, mmiRegisterAddress);
                var server = new GraspServer(address.Port, handler);
                Console.WriteLine("Register the service");
                reg.RegisterService(handler.ServiceDescription);

                Console.WriteLine("Starting the server...");
                server.Start();
            }
            catch (Exception x)
            {
                Console.WriteLine(x.StackTrace);
            }
            Console.WriteLine("done.");
        }

        /// <summary>
        /// Callback for unhandled exceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Log(Log_level.L_ERROR, e.ExceptionObject.ToString());

            //Write a log file
            System.IO.File.WriteAllText("CSharpAdapter_Error.log", DateTime.Now.ToString() + " " + e.ExceptionObject.ToString());
        }



        /// <summary>
        /// Tries to parse the command line arguments
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool ParseCommandLineArguments(string[] args)
        {

            //Parse the command line arguments
            OptionSet p = new OptionSet()
            {
                { "a|address=", "The address of the hostet tcp server.",
                  v =>
                  {
                      //Split the address to get the ip and port
                      string[] addr  = v.Split(':');

                      if(addr.Length == 2)
                      {
                          address.Address = addr[0];
                          address.Port = int.Parse(addr[1]);
                      }
                  }
                },

                { "r|raddress=", "The address of the register which holds the central information.",
                  v =>
                  {
                      //Split the address to get the ip and port
                      string[] addr  = v.Split(':');

                      if(addr.Length == 2)
                      {
                          mmiRegisterAddress.Address = addr[0];
                          mmiRegisterAddress.Port = int.Parse(addr[1]);
                      }
                  }
                },
            };

            try
            {
                p.Parse(args);
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot parse arguments");
            }


            return false;

        }


    }
}
