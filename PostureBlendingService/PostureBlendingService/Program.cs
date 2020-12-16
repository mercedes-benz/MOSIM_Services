// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Felix Gaisbauer

using MMICSharp.Adapter;
using MMIStandard;
using System;

namespace PostureBlendingService
{
    /// <summary>
    /// Entry point for hosting the posture blending service
    /// </summary>
    class Program
    {
        private static MIPAddress address = new MIPAddress();
        private static MIPAddress registerAddress = new MIPAddress();

        static void Main(string[] args)
        {

            //Parse the command line arguments
            if (!ParseCommandLineArguments(System.Environment.GetCommandLineArgs()))
            {
                Logger.Log(Log_level.L_ERROR, "Cannot parse the command line arguments. Closing the service!");

                Console.ReadLine();

                return;
            }

            //Start the server
            using (PostureBlendingServiceImpl server = new PostureBlendingServiceImpl(address, registerAddress))
            {
                server.Start();
                Console.ReadLine();
            }
            
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
                          registerAddress.Address = addr[0];
                          registerAddress.Port = int.Parse(addr[1]);
                      }
                  }
                }
            };

            try
            {
                p.Parse(args);
                return true;
            }

            catch (System.Exception)
            {
                Logger.Log(Log_level.L_ERROR, "Cannot parse command line arguments");
            }

            return false;
        }
    }
}
