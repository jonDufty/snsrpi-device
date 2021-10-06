#!/bin/bash

while getopts: "t:g:h" flag;
do
    case "${flag}" in
        t) THING_NAME=${OPTARG};;
        g) THING_GROUP_NAME=${OPTARG};;
        h) HOST=${OPTARG};;
    esac
done

echo "thing: $THING_NAME";
echo "group: $THING_GROUP_NAME";
echo "host: $HOST";

# aws iot create-thing --thing-name $THING_NAME
# aws iot add-thing-to-thing-group --thing-name $THING_NAME --thing-group-name $THING_GROUP_NAME
# aws iot create-policy $THING_NAME-IOTPolicy
# aws iot attach-policy --policy-name $THING_NAME-IOTPolicy --target
# aws iot create-keys-and-certificates --set-as-active \
#     --certificate-pem-file device.pem.crt \
#     --public-key-outfile public.pem.key \
#     --private-key-outfile private.pem.key