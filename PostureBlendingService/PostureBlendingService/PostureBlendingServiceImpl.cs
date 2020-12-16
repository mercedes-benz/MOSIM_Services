// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Felix Gaisbauer


using MMICSharp.Adapter;
using MMICSharp.Common;
using MMICSharp.Common.Tools;
using MMICSharp.Services;
using MMIStandard;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PostureBlendingService
{
    /// <summary>
    /// Posture blending services allows to blend between defined postures
    /// </summary>
    class PostureBlendingServiceImpl : MPostureBlendingService.Iface, IDisposable
    {

        #region private fields

        /// <summary>
        /// The respective skeleton access for the different skeletons
        /// </summary>
        private readonly ConcurrentDictionary<string, IntermediateSkeleton> SkeletonAccesses = new ConcurrentDictionary<string, IntermediateSkeleton>();

        /// <summary>
        /// The service description
        /// </summary>
        private MServiceDescription description = new MServiceDescription()
        {
            ID = "postureBlending1210020",
            Language = "C#",
            Name = "postureBlendingService",

            //Directly define the parameters
            Parameters = new List<MParameter>()
            {
            }
        };


        /// <summary>
        /// The utlized service controller to host the actual service
        /// </summary>
        private ServiceController controller;

        /// <summary>
        /// Log level for the logger (debug output)
        /// </summary>
        private Log_level logLevel = Log_level.L_DEBUG;

        #endregion

        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="address"></param>
        /// <param name="registerAddress"></param>
        public PostureBlendingServiceImpl(MIPAddress address, MIPAddress registerAddress)
        {
            Logger.Instance.Level = logLevel;

            Console.WriteLine(@"    ____             __                     ____  __               ___                _____                 _         ");
            Console.WriteLine(@"   / __ \____  _____/ /___  __________     / __ )/ /__  ____  ____/ (_)___  ____ _   / ___/___  ______   __(_)_______ ");
            Console.WriteLine(@"  / /_/ / __ \/ ___/ __/ / / / ___/ _ \   / __  / / _ \/ __ \/ __  / / __ \/ __ `/   \__ \/ _ \/ ___/ | / / / ___/ _ \");
            Console.WriteLine(@" / ____/ /_/ (__  ) /_/ /_/ / /  /  __/  / /_/ / /  __/ / / / /_/ / / / / / /_/ /   ___/ /  __/ /   | |/ / / /__/  __/");
            Console.WriteLine(@"/_/    \____/____/\__/\__,_/_/   \___/  /_____/_/\___/_/ /_/\__,_/_/_/ /_/\__, /   /____/\___/_/    |___/_/\___/\___/ ");
            Console.WriteLine(@"                                                                           /____/                                     ");
            Console.WriteLine(@"______________________________________________________________________________________________________________________");




            //Add the present address 
            this.description.Addresses = new List<MIPAddress>()
            {
                address
            };

            //Create a new service controller
            this.controller = new ServiceController(description, registerAddress, new MPostureBlendingService.Processor(this));
        }


        /// <summary>
        /// Setup method to setup the posture blending service and corresponding avatars
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public MBoolResponse Setup(MAvatarDescription avatar, Dictionary<string, string> properties)
        {
            if (!this.SkeletonAccesses.ContainsKey(avatar.AvatarID))
            {
                IntermediateSkeleton skeletonAccess = new IntermediateSkeleton();
                skeletonAccess.InitializeAnthropometry(avatar);

                this.SkeletonAccesses.TryAdd(avatar.AvatarID, skeletonAccess);
            }
            else
            {
                Logger.Log(Log_level.L_INFO, $"Avatar with id {avatar.AvatarID} already available -> Reinitializing the skeleton access.");
                //Already available
                this.SkeletonAccesses[avatar.AvatarID].InitializeAnthropometry(avatar);
            }

            //Nothing to do in here
            return new MBoolResponse(true);
        }


        /// <summary>
        /// Basic blending method which performs a posture blending between a start and end posture.
        /// </summary>
        /// <param name="startPosture"></param>
        /// <param name="targetPosture"></param>
        /// <param name="weight"></param>
        /// <param name="mask"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public MAvatarPostureValues Blend(MAvatarPostureValues startPosture, MAvatarPostureValues targetPosture, double weight, Dictionary<MJointType, double> mask, Dictionary<string, string> properties)
        {

            if (!this.SkeletonAccesses.ContainsKey(startPosture.AvatarID))
            {
                Logger.Log(Log_level.L_ERROR, $"Avatar with id {startPosture.AvatarID} not defined");
                return new MAvatarPostureValues();
            }


            //To do -> Convert to proper format
            Dictionary<MJointType, BlendProperty> blendMask = new Dictionary<MJointType, BlendProperty>();

            //To do -> use skeleton access
            return MMICSharp.Common.Tools.Blending.PerformBlend(this.SkeletonAccesses[startPosture.AvatarID], startPosture, targetPosture, (float)weight, blendMask);
        }

        public List<MAvatarPostureValues> BlendMany(MAvatarPostureValues startPosture, MAvatarPostureValues targetPosture, List<double> weights, Dictionary<MJointType, double> mask, Dictionary<string, string> properties)
        {
            List<MAvatarPostureValues> results = new List<MAvatarPostureValues>();

            //Perform a blend for each weight
            foreach (float weight in weights)
                results.Add(Blend(startPosture, targetPosture, weight, mask, properties));

            return results;
        }

        /// <summary>
        /// Generic consume method
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public Dictionary<string, string> Consume(Dictionary<string, string> properties)
        {
            //Do nothing 
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Returns the description of the service
        /// </summary>
        /// <returns></returns>
        public MServiceDescription GetDescription()
        {
            return this.description;
        }

        /// <summary>
        /// Method for signaling the present status
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetStatus()
        {
            return new Dictionary<string, string>()
            {
                { "Running", true.ToString()}
            };
        }

        /// <summary>
        /// Dispose routine that is called if the service should be disposed
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public MBoolResponse Dispose(Dictionary<string, string> properties)
        {
            this.Dispose();
            return new MBoolResponse(true);
        }

        /// <summary>
        /// Method is called if the service should be restarted
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public MBoolResponse Restart(Dictionary<string, string> properties)
        {
            this.Start();
            return new MBoolResponse(true);
        }


        public void Start()
        {
            //Start asynchronously
            this.controller.StartAsync();
        }

        public void Dispose()
        {
            //Dispose the controller if not null
            if (this.controller != null)
                this.controller.Dispose();
        }
    }
}
