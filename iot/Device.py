import json
import os
import requests
import threading
import logging
import time
from awscrt import io, mqtt, auth, http
from requests.api import request
from Auth import Auth

DEVICE_ENDPOINT = os.environ["DEVICE_ENDPOINT"]
DEVICE_NAME = os.environ["DEVICE_NAME"]


class Device:

    def __init__(self, device_name, device_endpoint) -> None:
        self.auth = Auth()
        self.name = device_name
        self.mqtt = None
        self.device_endpoint = device_endpoint
        self.msg_handler = {
            "settings": self.sub_get_settings,
            "operate": self.sub_operate_device
        }
        self.sub_topic = f"cmd/vibration/{DEVICE_NAME}/#"
        self.pub_topic = f"data/vibration/{DEVICE_NAME}/"

        self.heartbeat_thread = threading.Thread(
            target=self.heartbeat, name="health", kwargs={"timer": 10})

    def set_mqtt(self, mqtt):
        self.mqtt = mqtt
        self.heartbeat_thread.start()

    def on_message_received(self, topic, payload, dup, qos, retain, **kwargs):
        print(f"Received message: \ntopic = '{topic}'\npayload={payload}")
        try:
            # Based on assumed topic convention
            sensor_name, action = topic.split("/")[-2:]
        except:
            logging.error(
                "Incorrect topic naming convention, message handling failed")
            return

        message = json.loads(payload)
        if "resp-topic" in message.keys():
            resp_topic = message["resp-topic"]
        else:
            resp_topic = topic + "/resp"

        if action in self.msg_handler.keys():
            result = self.msg_handler[action](message, topic, sensor_name)
            print(f"{action} completed with result: {result}")

            # Forward result to response topic
            self.mqtt.publish(
                topic=resp_topic,
                payload=json.dumps(result),
                qos=mqtt.QoS.AT_LEAST_ONCE
            )

        else:
            logging.error(f"No handler present for action {action}")

        return

    def sub_get_settings(self, message: dict, topic: str, sensor_name: str):
        print("Invoking function get_settings")

        method = "GET"  # default
        body = None
        if "action" in message.keys():
            if message['action'] in ["post", "put", "update"]:
                method = "PUT"
                body = message["body"]

        url = f"http://{self.device_endpoint}/api/settings/{sensor_name}"

        try:
            resp = request(method=method, url=url, json=body)
            resp.raise_for_status()
            result = resp.json()
        except Exception as e:
            logging.error(f"Request to {url} failed")
            print("Error: ", e)
            result = {
                "status": "Failed",
                "error": "Settings request failed"
            }

        return result

    def sub_operate_device(self, message: dict, topic: str, sensor_name: str):
        print("Invoking action operate")

        url = f"http://{self.device_endpoint}/api/devices/{sensor_name}"
        method = "POST"  # default
        if "action" in message.keys():
            if message['action'].lower() in ['start', 'stop']:
                try:
                    resp = request(method=method, url=url, params={
                                   "action": message['action']})
                    resp.raise_for_status()
                    result = resp.json()
                except Exception as e:
                    logging.error(f"Request to {url} failed")
                    print("Error: ", e)
                    result = {
                        "status": "Failed",
                        "error": "Operate request failed"
                    }
                return result

        # If gets to here, incorrect body
        result = {
            "status": "Failed",
            "error": "Request failed. Incorrect query parameters in message body"
        }

        return result

    def pub_healthcheck(self, payload: dict):
        topic = self.pub_topic + "heartbeat"
        self.mqtt.publish(
            topic=topic,
            payload=json.dumps(payload),
            qos=mqtt.QoS.AT_LEAST_ONCE)
        print(f"Heartbeat sent to {topic}")

    def heartbeat(self, timer=10):
        while True:
            self.get_healthcheck()
            time.sleep(timer)

    def get_healthcheck(self):
        url = f"http://{self.device_endpoint}/api/health"
        logging.info(f"Getting heartbeat from {url}...")

        try:
            response = requests.get(url)
            response.raise_for_status()
            result = response.json()
            self.pub_healthcheck(result)
        except Exception as e:
            logging.error("Heartbeat failed")
            print("Error: ", e)

        return


if __name__ == "__main__":
    DEVICE_ENDPOINT = "localhost:5000"
    DEVICE_NAME = "tst-device"

    device = Device(DEVICE_NAME, DEVICE_ENDPOINT)
    resp = device.get_healthcheck()
