import os

PRIVATE_KEY = os.environ['PRIVATE_KEY']
ROOT_CA = os.environ['ROOT_CA']
DEVICE_CERT = os.environ['DEVICE_CERT']

class Auth:
    private_key = PRIVATE_KEY
    root_ca_cert = ROOT_CA
    device_cert = DEVICE_CERT

    def __init__(self) -> None:
        pass

    

