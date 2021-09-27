#!/usr/bin/env python

#
# Licensed to the Apache Software Foundation (ASF) under one
# or more contributor license agreements. See the NOTICE file
# distributed with this work for additional information
# regarding copyright ownership. The ASF licenses this file
# to you under the Apache License, Version 2.0 (the
# "License"); you may not use this file except in compliance
# with the License. You may obtain a copy of the License at
#
#   http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing,
# software distributed under the License is distributed on an
# "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
# KIND, either express or implied. See the License for the
# specific language governing permissions and limitations
# under the License.
#

import sys
import bpy
import logging
from pathlib import Path
from typing import List, Dict
#sys.path.append('C:/MOSIM/Gitlab/Core/Python') # location of MMIPython

from mathutils import Vector, Quaternion


# MOSIM-declarations
from MMIStandard.services import MInverseKinematicsService
from MMIStandard.register import MMIRegisterService
from MMIStandard.avatar.ttypes import MAvatarDescription
from MMIStandard.core.ttypes import MIPAddress, MBoolResponse, MServiceDescription

# Load from BlenderIKService
from BlenderMMI.MAvatarPostureGenerator import JSON2MAvatarPosture
from BlenderMMI.IntermediateSkeletonApplication import IntermediateSkeletonApplication

from .ikservice import IKService

## Load from Gitlab!
#from MMIPython.core.services.service_access import ServiceAccess
from MMIPython.core.utils.thrift_client import ThriftClient

# Load from Blender/Python/Lib/site-packages (modified Blender distribution)
from thrift.transport import TSocket
from thrift.transport import TTransport
from thrift.protocol import TCompactProtocol
from thrift.server import TServer

logger = logging.getLogger(__name__)

RESOURCES = Path(bpy.data.filepath).parent
m_avatar_posture = JSON2MAvatarPosture(RESOURCES/"intermediate.mos")

class EIKServer(IKService):
    
    def __init__(self, name, id, language, ip=None, port=None):        
        
        super().__init__(m_avatar_posture)
        self.name = name
        self.id = id
        self.language = language
        self.ip = ip
        self.port = port
        self.server = None
        
    @property
    def description(self):
        description = MServiceDescription()
        description.Name = self.name
        description.ID = self.id
        description.Language = self.language
        if self.ip and self.port:
            description.Addresses = [MIPAddress(Address=self.ip, Port=self.port)]
            
        return description
        
    def GetStatus(self) -> Dict[str, str]:
        """
        Implementation for <MMIServiceBase>: Returns the present status of the 
        service."""
        #logger.info("Call to dummy-Method GetStatus. This is not properly implemented yet.")
        return {"Running": "True"} # dummy
        
    def GetDescription(self) -> MServiceDescription:
        """
        Implementation for <MMIServiceBase>: Returns the specific 
        <MServiceDescription> for the service.
        """
        logger.info("Call to dummy-Method GetDescription. This is not properly implemented yet.")
        return self.description # only exists in the Server!
		
    def Setup(self, description: MAvatarDescription, properties: Dict[str, str]) -> MBoolResponse: # argument names don't match specs
        """
        Implementation for <MMIServiceBase>: Basic method to setup the service. 
        This function can be used to reduce the network traffic. For instance, 
        instead of transferring the full hierarchy, only the posture values can 
        be transmitted if being initialized in before.
        
        Parameters:
         - description 
         - properties

        """
        logger.debug("Call to Setup")
        self.app.disableAllConstraints()
        bpy.context.view_layer.update()
        for b in self.app.object.pose.bones: # Should this be a method of self.app?
            b.rotation_mode="QUATERNION"
            b.rotation_quaternion = Quaternion((1,0,0,0))
            b.location = Vector((0,0,0))
        bpy.context.view_layer.update()
        #self.app.ScaleMAvatarPosture(description)
        return MBoolResponse(Successful=True)
    
    def Consume(self, properties: Dict[str, str]) -> Dict[str, str]:
        """
        Implementation for <MMIServiceBase>: Function to consume a service 
        without needing the explicit interface. This can be utilized if new 
        services are added to the framework which signature is not known yet.
        
        Parameters:
         - properties

        """
        logging.error("Call to Consume: Method is not implemented!")
        pass
    
    def register(self, registry_host, registry_port):    
        # Connect to MMILauncher
        logger.debug("Connecting to Registry at %s::%i.", registry_host, registry_port)
        registered = False
        if not (self.ip and self.port):
            logger.warning("Own address unknown for registration. Please init_thrift before registration!")
            
        while not registered:
            # timeout?
            try:
                with ThriftClient(registry_host, registry_port, MMIRegisterService.Client) as client:
                    response   = client._access.RegisterService(self.description)
                    sessionID  = client._access.CreateSessionID(dict())
                    registered = response.Successful
                    if registered:
                        self.launcherAddress = MIPAddress(Address=registry_host, Port=registry_port)
                        logger.info("Registered successfully at MMIRegister [%s]", self.launcherAddress)
            except TTransport.TTransportException as x:
                logger.warning(f"Registration Server at {registry_host}:{registry_port} unreachable!")
                # time.sleep(1)
            except Exception as x:
                logger.exception("Unknown Exception -> Abort Registration!")
                raise x
                
        return
    
    def init_thrift(self, address, port, nthreads=20):
        logger.info("Initalizing Thrift-Server at %s::%i with %i threads.", address, port, nthreads)
        IKProcessor = MInverseKinematicsService.Processor(self)
        trans_svr   = TSocket.TServerSocket(host=address, port=port) 
        # self.ownAddress = MIPAddress(Address=address, Port=port)
        trans_fac   = TTransport.TBufferedTransportFactory()
        proto_fac   = TCompactProtocol.TCompactProtocolFactory()
        self.server = TServer.TThreadPoolServer(IKProcessor, trans_svr, 
            trans_fac, proto_fac)
        self.server.setNumThreads(nthreads)
        self.ip = address
        self.port = port
        logger.debug('Thrift-Server initialized.')
        return
    
    def start(self): 
        if self.server:
            self.server.serve()
            logger.info('Server running')
        else:
            logger.error("Can't start server; need to initialize first!")
            
        return
        
    #def __del__(self): # This need explanation or logic
     #   logger.info("Server closes down")
        
    @staticmethod
    def fromConfig(config):
        
        name = config.get('IKSERVER', 'name', fallback='ikService')
        id = config.get('IKSERVER', 'id', fallback='123456')
        language = config.get('IKSERVER', 'language', fallback='BlenderPython')
        server = EIKServer(name, id, language)
        return server
        