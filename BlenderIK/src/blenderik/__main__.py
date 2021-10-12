import sys
from pathlib import Path
from configparser import ConfigParser
from argparse import ArgumentParser
import json
import logging
import unittest
sys.path.append(Path(__file__).parent.as_posix())
logger = logging.getLogger(__name__)


try:
    import bpy
except ModuleNotFoundError:
    print("""This Program cannot be used as a StandAlone-Skript! 
    You must start it from within Blender. Try
    
        blender.exe resources\IKService_dennis.blend --background --python main.py
        
    """)
    sys.exit(1)

from server import EIKServer



DEFAULT_CONFIG = {
    "LOGGING": {
        "loggingLevel": "DEBUG"
    },
    "SERVICEDESCRIPTION": {
        "Name": "ikService",
        "ID": "123456",
        "Language": "BlenderPython"
    },
    "RESOURCES": {
        "blendfile": "IKService_dennis.blend",
        "initialPosture": "intermediate.mos",
    },
    "IKSERVER": {
        "socket_address": "127.0.0.1",
        "address": "127.0.0.1",
        "port": "8904"
    },
    "REGISTERSERVICE": {
        "address": "127.0.0.1",
        "port": "9009"
    }
}
def run(config, cli_args):
    if cli_args.registry:
        ip, port = cli_args.registry.split(':')
        logger.info("Registry: Overwrite Config with CLI-Argument %s:%s", ip, port)
        config['REGISTERSERVICE']['address'] = ip
        config['REGISTERSERVICE']['port'] = port
        
    if cli_args.address:
        ip, port = cli_args.address.split(':')
        logger.info("Address: Overwrite Config with CLI-Argument %s:%s", ip, port)
        config['IKSERVER']['address'] = ip
        config['IKSERVER']['socket_address'] = ip
        config['IKSERVER']['port'] = port
    if cli_args.listenAll:
        config["IKSERVER"]["socket_address"] = ""
    
    # read description.json    
    with Path("description.json").open() as file:
        description = json.load(file)
    
    logger.info("%s", description)
    IKServer = EIKServer(description['Name'], description['ID'], description['Language'])
        
    IKServer.init_thrift(
        config.get('IKSERVER', 'address'), 
        config.get('IKSERVER', 'socket_address'), 
        config.getint('IKSERVER', 'port')
    )
        
    IKServer.register(
        config.get('REGISTERSERVICE', 'address'), 
        config.getint('REGISTERSERVICE', 'port')
    )
    
    IKServer.start()
    
def test(config, cli_args):
    logger.info("running tests")
    unittest.main(module="tests" , argv=['BlenderIkService'], verbosity=3)
    logger.info("Tests done")

if __name__ == '__main__':

    # read config
    config = ConfigParser()
    config.read_dict(DEFAULT_CONFIG)
    configPath = Path("service.config")
    if configPath.exists():
        config.read(configPath)
    else:
        logger.warning("service.config not found: proceed with DEFAULT_CONFIG")
    
    # set Logger
    numeric_level = getattr(logging, config["LOGGING"]["loggingLevel"].upper(), None)
    if not isinstance(numeric_level, int):
        raise ValueError('Invalid log level: %s' % loglevel)
    
    fileLogger = logging.FileHandler('ikservice.log', mode='w')
    
    streamLogger = logging.StreamHandler(sys.stderr)
    streamLogger.setLevel(numeric_level)
    
    logging.basicConfig(level=numeric_level, handlers=[fileLogger, streamLogger])
    
    # Read CLI-Arguments
    if Path(sys.argv[0]).name.split(".")[0] == 'blender':
        
        raw_args = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    else:
        # Standalone-mode
        print(sys.argv[0])
        print("This skript only works in combination with Blender.")
        sys.exit(1)
        
    logger.info("parsing Cli")
    argparser = ArgumentParser(prog='BlenderIKService')
    
    subparsers = argparser.add_subparsers(help='sub-command help')

    test_parser = subparsers.add_parser('test', help="Run unittests")
    test_parser.set_defaults(func=test)

    run_parser = subparsers.add_parser('run', help="Start Service")
    run_parser.add_argument('-a', '--address', 
        help="Address and Port under which the Server will operate",
        default='')
    run_parser.add_argument('-r', '--registry',
        help="Address of the MOSIM-Registry Service",
        default='')
    run_parser.add_argument('-m', '-mmu-dir', 
        help="Directory of the mmu's (not used)",
        default='')
    run_parser.add_argument('-d', '--description',
        help="Path to the service description",
        default='')
    run_parser.add_argument('-l', '--listenAll', action='store_true', default=False)
    run_parser.set_defaults(func=run)
        
    cmd_args = argparser.parse_args(raw_args)
    print(Path.cwd())
    
    logging.info("cmd_args: %s", cmd_args)
    cmd_args.func(config, cmd_args)
    logging.info("Regular program termination.")
    exit(0)
    
    
