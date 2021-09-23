from awscrt import mqtt
from awsiot import iotshadow
from awsiot.iotshadow import *
from abc import ABC, abstractmethod, abstractproperty
from uuid import uuid4
import logging

from requests.api import request


class ShadowHandler(ABC):

    def __init__(self, client: IotShadowClient, thing, shadow, health) -> None:
        super().__init__()
        self.client = client
        self.token = str(uuid4())
        self.shadow_request = {
            "thing_name": thing,
            "shadow_name": shadow,
            "client_token": self.token
        }
        self.local_state = None
        self.get_healthcheck = health

    def on_shadow_rejected(self, response: ErrorResponse):
        logging.error(
            f"Get Shadow message rejected:\ncode: {response.code}\nmessage: {response.message}")

    def on_get_shadow_accepted(self, response: GetShadowResponse):
        print("Get shadow successful")
        print(response.state)

    def on_delete_shadow_accepted(self, response: DeleteShadowResponse):
        print(f"Shadow successfully deleted")

    def on_update_shadow_accepted(self, response: UpdateShadowResponse):
        print(f"Update shadow successful")

    def on_update_shadow_delta(self, response: ShadowDeltaUpdatedEvent):
        print(f"Delta received")
        print(response.state)

    def set_state(self, state, override=False):
        self.local_state.update(state)

    @abstractmethod
    def subscribe_to_shadow_topics(self):
        pass

    def update_state(self, override_desired=False):
        print(f"{self.shadow_request['shadow_name']}:Sending new state")
        new_state = ShadowState(reported=self.local_state)
        if(override_desired):
            new_state.desired = self.local_state

        request = {**self.shadow_request, **{"state": new_state}}
        try:
            future = self.client.publish_update_named_shadow(
                request=UpdateNamedShadowRequest(**request),
                qos=mqtt.QoS.AT_LEAST_ONCE
            )
            future.add_done_callback(self.on_future_callback)
        except Exception as e:
            logging.error(
                f"{self.shadow_request['shadow_name']}:Update publish failed")
            logging.error(e)

    def delete_shadow(self):
        try:
            future = self.client.publish_delete_named_shadow(
                request=DeleteNamedShadowRequest(**self.shadow_request),
                qos=mqtt.QoS.AT_LEAST_ONCE,
            )
            future.result()
            print("Shadow deleted")
        except Exception as e:
            logging.error("Delete failed")
            logging.error(f"Error: {e}")

    def on_future_callback(self, future):
        try:
            future.result()
            print(
                f"{self.shadow_request['shadow_name']}:State update publish successful.")
        except Exception as e:
            logging.error("Failed to publish state update request.")
            logging.error(e)


class GlobalShadowHandler(ShadowHandler):
    def __init__(self, client: IotShadowClient, thing, shadow, device_endpoint, health) -> None:
        super().__init__(client, thing, shadow, health)
        self.device_endpoint = device_endpoint

        self.local_state = {
            "device_id": None,
            "sensors": []
        }

        # self.update_state(override_desired=True)
        self.subscribe_to_shadow_topics()
        self.sensor_index = {}

    def set_state(self, state, override=False, update_index=False):
        super().set_state(state, override=override)
        if update_index:
            self.__index_state__()

    def __index_state__(self):
        index = {}
        i = 0
        for i in range(len(self.local_state['sensors'])):
            index[self.local_state['sensors'][i]['sensor_id']] = i
        self.sensor_index = index

    def subscribe_to_shadow_topics(self):
        print(
            f"Subscribing to shadow topic {self.shadow_request['shadow_name']}")
        try:
            # Subscribe to GET topics

            # Subscribe to DELETE topics
            delete_accepted_future, _ = self.client.subscribe_to_delete_named_shadow_accepted(
                request=DeleteNamedShadowSubscriptionRequest(
                    **self.shadow_request),
                qos=mqtt.QoS.AT_LEAST_ONCE,
                callback=self.on_delete_shadow_accepted
            )

            delete_rejected_future, _ = self.client.subscribe_to_delete_named_shadow_rejected(
                request=DeleteNamedShadowSubscriptionRequest(
                    **self.shadow_request),
                qos=mqtt.QoS.AT_LEAST_ONCE,
                callback=self.on_shadow_rejected
            )

            delete_accepted_future.result()
            delete_rejected_future.result()
            print("Successfully subscribed to DELETE topics")

            # Subscribe to UPDATE topics
            update_rejected_future, _ = self.client.subscribe_to_update_named_shadow_rejected(
                request=UpdateNamedShadowSubscriptionRequest(
                    **self.shadow_request),
                qos=mqtt.QoS.AT_LEAST_ONCE,
                callback=self.on_shadow_rejected
            )

            update_rejected_future.result()
            print("Successfully subscribed to DELETE topics")

            # update_delta_future, _ = self.client.subscribe_to_named_shadow_delta_updated_events(
            #     request=NamedShadowDeltaUpdatedSubscriptionRequest(
            #         **self.shadow_request),
            #     qos=mqtt.QoS.AT_LEAST_ONCE,
            #     callback=self.on_update_shadow_delta
            # )

            # update_delta_future.result()
            # print("Successfully subscribed to DELTA topics")

        except Exception as e:
            logging.error("Error in subscribing to key topics")
            logging.error(f"Error: {e}")

    def on_shadow_rejected(self, response: ErrorResponse):
        logging.error(
            f"Get Shadow message rejected:\ncode: {response.code}\nmessage: {response.message}")

    def on_delete_shadow_accepted(self, response: DeleteShadowResponse):
        print(
            f"Shadow {self.shadow_request['shadow_name']} successfully deleted")

    def on_update_shadow_delta(self, response: ShadowDeltaUpdatedEvent):
        print(f"{self.shadow_request['shadow_name']}: Received state delta")
        delta_state: ShadowState = response.state
        try:
            for sensor in delta_state['sensors']:
                if sensor['sensor_id'] in self.sensor_index.keys():
                    result = self.change_sensor_state(
                        sensor["sensor_id"], sensor["active"])
        except Exception as e:
            logging.error("Unexpected key error")
            return

        self.update_state()


class SensorShadowHandler(ShadowHandler):
    def __init__(self, client: IotShadowClient, thing, shadow, sensor_name, endpoint, health) -> None:
        super().__init__(client, thing, shadow, health)
        self.sensor_name = sensor_name
        self.device_endpoint = endpoint
        self.local_state = {
            "active": None,
            "settings": None
        }

        self.subscribe_to_shadow_topics()
        # self.update_state(override_desired=True)

    def set_state(self, key, state):
        self.local_state[key] = state
        print(
            f"{self.shadow_request['shadow_name']}:Updated state for {key} to {state}")

    # TODO handlers
    # TODO subscriptions
    def subscribe_to_shadow_topics(self):
        print(
            f"Subscribing to shadow topic {self.shadow_request['shadow_name']}")
        try:
            # Subscribe to GET topics

            # Subscribe to DELETE topics
            delete_accepted_future, _ = self.client.subscribe_to_delete_named_shadow_accepted(
                request=DeleteNamedShadowSubscriptionRequest(
                    **self.shadow_request),
                qos=mqtt.QoS.AT_LEAST_ONCE,
                callback=self.on_delete_shadow_accepted
            )

            delete_rejected_future, _ = self.client.subscribe_to_delete_named_shadow_rejected(
                request=DeleteNamedShadowSubscriptionRequest(
                    **self.shadow_request),
                qos=mqtt.QoS.AT_LEAST_ONCE,
                callback=self.on_shadow_rejected
            )

            delete_accepted_future.result()
            delete_rejected_future.result()

            print("Successfully subscribed to DELETE topics")

            update_future, _ = self.client.subscribe_to_update_named_shadow_accepted(
                request=UpdateNamedShadowSubscriptionRequest(
                    ** self.shadow_request),
                qos=mqtt.QoS.AT_LEAST_ONCE,
                callback=self.on_update_shadow_accepted
            )
            update_future.result()
            print("Successfully subscribed to udpate topics")

        except Exception as e:
            logging.error("Error in subscribing to key topics")
            logging.error(f"Error: {e}")

    def on_update_shadow_accepted(self, response: UpdateShadowResponse):

        if response.client_token == self.token:
            return
        state: ShadowState = response.state
        desired = state.desired
        if not desired:
            return

        result = {"error": None}
        if 'active' in desired.keys():
            if desired['active'] != self.local_state['active']:
                result = self.change_sensor_running(desired['active'])

        if 'settings' in desired.keys():
            if desired['settings'] != self.local_state['settings']:
                result = self.get_or_update_sensor_settings(
                    desired['settings'])

        if result['error']:
            logging.error(f"Error: {result['error']} ")

        self.update_state()
        return

        # CHeck valid
        # Check source
        # Select handler
        # CHeck return

    def change_sensor_running(self, active: bool):
        print(f"{self.shadow_request['shadow_name']}:Sending start/stop to sensor")

        url = f"http://{self.device_endpoint}/api/devices/{self.sensor_name}"
        method = "POST"  # default
        try:
            resp = request(method=method, url=url, params={
                "active": active})
            resp.raise_for_status()
            result = resp.json()

            self.set_state("active", active)
            # self.update_state()
            result = {
                "status": "Success",
                "error": None
            }

        except Exception as e:
            logging.error(f"Request to {url} failed")
            print("Error: ", e)
            result = {
                "status": "Failed",
                "error": "Operate request failed"
            }
        return result

    def get_or_update_sensor_settings(self, settings=None):
        print(f"{self.shadow_request['shadow_name']}:Invoking function get_settings")

        method = "GET"  # default
        body = None
        url = f"http://{self.device_endpoint}/api/settings/{self.sensor_name}"
        if settings:
            method = "PUT"
            body = settings

        try:
            resp = request(method=method, url=url, json=body)
            resp.raise_for_status()
            result = resp.json()

            self.set_state("settings", result)
            # self.update_state()
            result = {
                "status": "Success",
                "error": None
            }
        except Exception as e:
            logging.error(f"Request to {url} failed")
            print("Error: ", e)
            result = {
                "status": "Failed",
                "error": "Settings request failed"
            }

        return result


if __name__ == "__main__":

    shadow = GlobalShadowHandler("client", "shadow", 'thing')
