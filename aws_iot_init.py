import boto3
import os
import argparse
import json
from pathlib import Path

from boto3 import session

parser = argparse.ArgumentParser(
    description="Initialise new AWS Iot Core Thing")
parser.add_argument('--thing-name', '-n', nargs=1,
                    type=str, help='name of thing')
parser.add_argument('--thing-group-name', '-g', nargs=1,
                    type=str, help='name of thing group')
parser.add_argument('--output-dir', '-o', nargs=1, type=str,
                    default=".", help='location to save keys/certificates')


def main():
    args = parser.parse_args()

    thing_name = args.thing_name[0]
    thing_group_name = args.thing_group_name[0]
    output_dir = args.output_dir[0]

    session = boto3.Session(profile_name='default')
    iot_client = boto3.client('iot')

    # Create thing
    # iot_client.create_thing(thingName=thing_name)
    # iot_client.add_thing_to_thing_group(
    #     thingName=thing_name, thingGroupName=thing_group_name)

    policy_name = thing_name + "_IoTPolicy"
    # TODO - Currently this just gives permission to all resources. update this for more restricted access
    policy = {
        "Version": "2012-10-17",
        "Statement": [
            {
                "Effect": "Allow",
                "Action": [
                    "iot:Connect",
                    "iot:Receive",
                    "iot:Publish",
                    "iot:Subscribe"
                ],
                "Resource": "*"
            }
        ]
    }

    # Generate keys and certificates
    keys = iot_client.create_keys_and_certificate(setAsActive=True)
    cert_id = keys['certificateId']
    cert_arn = keys['certificateArn']

    # Create and attach policy that allows publish/subscribe to shadows
    response = iot_client.create_policy(policyName=policy_name, policyDocument=json.dumps(policy))
    response = iot_client.attach_policy(policyName=policy_name, target=cert_arn)
    response = iot_client.attach_thing_principal(thingName=thing_name, principal=cert_arn)

    # Save keys and certifactes to files. Defaults to current directory
    with open(Path(output_dir, "device.pem.crt"), 'w') as f:
        f.write(keys['certificatePem'])

    with open(Path(output_dir, "private.pem.crt"), 'w') as f:
        f.write(keys['keyPair']['PrivateKey'])

    with open(Path(output_dir, "public.pem.crt"), 'w') as f:
        f.write(keys['keyPair']['PublicKey'])

    # Get CA certificates

if __name__ == "__main__":
    main()
