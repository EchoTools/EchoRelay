#!/usr/bin/env python3
"""Migrate EchoRelay db account files into Nakama server"""
import json
from pathlib import Path

import requests
import click


def get_session(nkUri, serverKey, deviceId):
    # try to authenticate with CustomID and migrate it to DeviceId
    s = requests.Session()

    res = s.post((nkUri + "/v2/account/authenticate/custom?create=false").format(deviceId),
                        auth=(serverKey,""), json={"id": deviceId})

    # if the customId auths, link the device
    if res.status_code == 200:

        s.headers.update({"Authorization": "Bearer " + res.json()["token"]})

        res = s.post(nkUri + "/v2/account/link/device", json={"id": deviceId})
        assert res.status_code == 200

    else: # try to auth with device

        # Authenticate as the user account with the OVR ID
        res = s.post((nkUri + "/v2/account/authenticate/device?create=true&username={}").format(deviceId),
                            auth=(serverKey,""), json={"id": deviceId})

        if res.status_code != 200:
            raise Exception("Error {code}: {msg}".format(code=res.status_code, msg=res.text))

        s.headers.update({"Authorization": "Bearer " + res.json()["token"]})


    # unlink the custom id
    res = s.post(nkUri + "/v2/account/unlink/custom", json={"id": deviceId})

    return s


def load_account(nkUri, serverKey, account):
    deviceId = account['profile']['client']['xplatformid']

    assert deviceId != ''

    s = get_session(nkUri, serverKey, deviceId)

    # set the display name
    res = s.put(nkUri + "/v2/account", json=
                {
                    "display_name": account['profile']['client']['displayname'],

                })
    if res.status_code != 200:
        raise Exception("Error {code}: {msg}".format(code=res.status_code, msg=res.text))

    res = s.put(nkUri + "/v2/storage", json={
        "objects": [
        {
            "collection": "relayConfig",
            "key": "authSecrets",
            "value": json.dumps({
                "AccountLockHash": account.get('account_lock_hash', None),
                "AccountLockSalt": account.get('account_lock_salt', None),
            }),
            "version": "*" # if it does not exist
        },
        ]
        })

    if res.status_code != 200:
            # auth exists
            pass

     # set the auth object
    res = s.put(nkUri + "/v2/storage", json={
        "objects": [
        {
            "collection": "profile",
            "key": "client",
            "value": json.dumps(account['profile']['client']),
        },
        {
            "collection": "profile",
            "key": "server",
            "value": json.dumps(account['profile']['server']),
        }
        ]
    })

    if res.status_code != 200:
        raise Exception("Error {code}: {msg}".format(code=res.status_code, msg=res.text))
    print('done')

@click.command()
@click.argument('ACCOUNTFILE', type=click.File('r'), nargs=-1)
@click.option('-n', '--nakama-uri', 'nkUri', required=True)
@click.option('-k', '--server-key', 'serverKey', required=True)
def main(accountfile, nkUri, serverKey):

    for path in accountfile:
        account = json.load(path)
        print('Migrating {} ({})'.format(
            account['profile']['client']['xplatformid'],
            account['profile']['client']['displayname']
        ))

        load_account(nkUri, serverKey, account)

if __name__ == "__main__":
    main()
