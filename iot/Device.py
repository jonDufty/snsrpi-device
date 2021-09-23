import json
import os
import requests
import threading
import logging
import time
from awscrt import io, mqtt, auth, http
from requests.api import request
from Auth import Auth
from ShadowHandler import GlobalShadowHandler

DEVICE_ENDPOINT = os.environ["DEVICE_ENDPOINT"]
DEVICE_NAME = os.environ["DEVICE_NAME"]


class Device:

    def __init__(self, device_name, device_endpoint) -> None:
        self.auth = Auth()
        self.name = device_name
        self.mqtt = None
        self.device_endpoint = device_endpoint
        self.heartbeat_thread = threading.Thread(
            target=self.heartbeat, name="health", kwargs={"timer": 10})
        self.disable_heartbeat_event = threading.Event()

        self.global_shadow = None
        self.sensor_shadows = []

    def set_global_shadow(self, shadow_client):
        self.global_shadow = GlobalShadowHandler(
            shadow_client, self.name, "global", self.device_endpoint, self.get_healthcheck)

    def set_mqtt(self, mqtt):
        self.mqtt = mqtt

    def enable_heartbeat(self):
        self.disable_heartbeat_event.clear()
        self.heartbeat_thread.start()

    def disable_heartbeat(self):
        self.disable_heartbeat_event.set()

    def delete_shadows(self):
        self.disable_heartbeat()
        self.global_shadow.delete_shadow()
        for s in self.sensor_shadows:
            s.delete_shadow()

    def heartbeat(self, timer=60):
        while not self.disable_heartbeat_event.is_set():
            self.get_healthcheck()
            time.sleep(timer)

    def get_healthcheck(self):
        url = f"http://{self.device_endpoint}/api/health"
        logging.info(f"Getting heartbeat from {url}...")

        try:
            response = requests.get(url)
            response.raise_for_status()
            result = response.json()
            self.global_shadow.set_state(result)
            self.global_shadow.update_state()
        except Exception as e:
            logging.error("Heartbeat failed")
            print("Error: ", e)

        return


if __name__ == "__main__":
    DEVICE_ENDPOINT = "localhost:5000"
    DEVICE_NAME = "tst-device"

    device = Device(DEVICE_NAME, DEVICE_ENDPOINT)
    resp = device.get_healthcheck()
