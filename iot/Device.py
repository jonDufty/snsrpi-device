import json
import os
import requests
import threading
import logging
import time
from Auth import Auth

DEVICE_ENDPOINT = os.environ["DEVICE_ENDPOINT"]
DEVICE_NAME = os.environ["DEVICE_NAME"]


class Device:

    def __init__(self) -> None:
        self.auth = Auth()
        self.name = DEVICE_NAME
        self.device_endpoint = DEVICE_ENDPOINT
        self.msg_handler = {
            "settings": self.sub_get_settings,
            "update_settings": self.sub_update_settings,
            "operate": self.sub_operate_device,
        }
        self.topic = f"vibration/{DEVICE_NAME}/#"
        self.heartbeat_thread = threading.Thread(target=self.heartbeat)
        # self.heartbeat_thread.start()

    def on_message_received(self, topic, payload, dup, qos, retain, **kwargs):
        print(f"Received message from topic '{topic}': {payload}")
        # self.msg_handler[self.infer_topic(topic)](payload)

    def infer_topic(self, topic: str) -> str:
        return topic.split("/")[-1]

    def sub_update_settings(self, payload):
        pass

    def sub_get_settings(self, payload):
        message = json.loads(payload)
        if "res-topic" in message.keys():
            res_topic = message["res-topic"]
        else:
            res_topic = self.topic.replace("#","resp")

        if "sensor" not in message.keys():
            logging.warning("No device in payload")
            return

        sensor = message["sensor"]
        url = f"http://{self.device_endpoint}/api/settings/{sensor}"
        try:
            result = requests.get(url).json()

        except:
            result = {
                "status":"Failed",
                "error":"Settings request failed"
            }
        
        # Forward response to response topic
        # TODO Send Mqtt response



    def pub_healthcheck(self, payload):
        pass

    def sub_operate_device(payload):
        pass

    def heartbeat(self, timer=60):
        while True:
            self.get_healthcheck()
            logging.debug("Sending heartbeat...")
            time.sleep(timer)

    def get_healthcheck(self):
        url = f"http://{self.device_endpoint}/api/health"
        try:
            response = requests.get(url)
            result = response.json()
            self.pub_healthcheck(result)
        except:
            logging.error("Heartbeat failed")