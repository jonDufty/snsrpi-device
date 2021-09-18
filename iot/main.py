# Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
# SPDX-License-Identifier: Apache-2.0.

import argparse
from awscrt import io, mqtt, auth, http
from awsiot import mqtt_connection_builder
import sys, os
import threading
from datetime import datetime, timedelta
import time
from uuid import uuid4
import json
from Device import Device

# This sample uses the Message Broker for AWS IoT to send and receive messages
# through an MQTT connection. On startup, the device connects to the server,
# subscribes to a topic, and begins publishing messages to that topic.
# The device should receive those same messages back from the message broker,
# since it is subscribed to that same topic.

AWS_IOT_ENDPOINT = os.environ["AWS_IOT_ENDPOINT"]

# io.init_logging()

received_count = 0
received_all_event = threading.Event()

# Callback when connection is accidentally lost.
def on_connection_interrupted(connection, error, **kwargs):
    print("Connection interrupted. error: {}".format(error))


# Callback when an interrupted connection is re-established.
def on_connection_resumed(connection, return_code, session_present, **kwargs):
    print("Connection resumed. return_code: {} session_present: {}".format(return_code, session_present))

    if return_code == mqtt.ConnectReturnCode.ACCEPTED and not session_present:
        print("Session did not persist. Resubscribing to existing topics...")
        resubscribe_future, _ = connection.resubscribe_existing_topics()

        # Cannot synchronously wait for resubscribe result because we're on the connection's event-loop thread,
        # evaluate result with a callback instead.
        resubscribe_future.add_done_callback(on_resubscribe_complete)


def on_resubscribe_complete(resubscribe_future):
        resubscribe_results = resubscribe_future.result()
        print("Resubscribe results: {}".format(resubscribe_results))

        for topic, qos in resubscribe_results['topics']:
            if qos is None:
                sys.exit("Server rejected resubscribe to topic: {}".format(topic))


# Callback when the subscribed topic receives a message
def on_message_received(topic, payload, dup, qos, retain, **kwargs):
    print("Received message from topic '{}': {}".format(topic, payload))
    global received_count
    received_count += 1
    if received_count == 10:
        received_all_event.set()

if __name__ == '__main__':
    
    AWS_IOT_ENDPOINT = os.environ["AWS_IOT_ENDPOINT"]
    DEVICE_ENDPOINT = os.environ["DEVICE_ENDPOINT"]
    DEVICE_NAME = os.environ["DEVICE_NAME"]
    
    # Spin up resources
    event_loop_group = io.EventLoopGroup(1)
    host_resolver = io.DefaultHostResolver(event_loop_group)
    client_bootstrap = io.ClientBootstrap(event_loop_group, host_resolver)

    proxy_options = None
    device = Device(DEVICE_NAME, DEVICE_ENDPOINT)

    mqtt_connection = mqtt_connection_builder.mtls_from_path(
        endpoint=AWS_IOT_ENDPOINT,
        port=443,
        cert_filepath=device.auth.device_cert,
        pri_key_filepath=device.auth.private_key,
        client_bootstrap=client_bootstrap,
        ca_filepath=device.auth.root_ca_cert,
        on_connection_interrupted=on_connection_interrupted,
        on_connection_resumed=on_connection_resumed,
        client_id=device.name,
        clean_session=False,
        keep_alive_secs=30,
        http_proxy_options=proxy_options)

    device.set_mqtt(mqtt_connection)

    print(f"Connecting to {AWS_IOT_ENDPOINT} with client ID '{device.name}'...")

    connect_future = mqtt_connection.connect()
    # Future.result() waits until a result is available
    connect_future.result()
    print("Connected!")

    # Subscribe
    print(f"Subscribing to topic '{device.sub_topic}")
    subscribe_future, packet_id = mqtt_connection.subscribe(
        topic=device.sub_topic,
        qos=mqtt.QoS.AT_LEAST_ONCE,
        callback=device.on_message_received)

    subscribe_result = subscribe_future.result()
    print("Subscribed with {}".format(str(subscribe_result['qos'])))
  
    # This waits forever if count was set to 0.
    if not received_all_event.is_set():
        print("Waiting for all messages to be received...")

    received_all_event.wait()
    print("{} message(s) received.".format(received_count))
    end_time = time.time() + 300 
    while time.time() < end_time:
        pass

    # Disconnect
    print("Disconnecting...")
    disconnect_future = mqtt_connection.disconnect()
    disconnect_future.result()
    print("Disconnected!")