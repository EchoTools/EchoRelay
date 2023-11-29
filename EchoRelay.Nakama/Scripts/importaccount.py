#!/usr/bin/python
"""Migrate EchoRelay db account files into Nakama server"""
import json
from pathlib import Path

import requests
import click

def get_session(nkUri, serverKey, deviceId, username):
    # try to authenticate with CustomID and migrate it to DeviceId
    s = requests.Session()

    # Authenticate as the user account with the OVR ID
    res = s.post((nkUri + "/v2/account/authenticate/device?create=true&username={}").format(username),
                 auth=(serverKey,""), json={"id": deviceId})

    if res.status_code != 200:
        raise Exception("Error {code}: {msg}".format(code=res.status_code, msg=res.text))

    s.headers.update({"Authorization": "Bearer " + res.json()["token"]})

    return s

    
def load_account(nkUri, serverKey, account, mainaccountid=None):
    deviceId = account['profile']['client']['xplatformid']

    assert deviceId != ''
    username = account['profile']['client']['displayname']
    
    s = get_session(nkUri, serverKey, mainaccountid or deviceId, username)
    
    if mainaccountid:
        data = {
            'id': deviceId,
        }
        res = s.post(nkUri + "/v2/account/link/device", json=data)
        if res.status_code != 200:
            raise Exception("Error {code}: {msg}".format(code=res.status_code, msg=res.text))
        print("Linked {} to {}".format(deviceId, mainaccountid))
        return
    
    url = nkUri + "/v2/rpc/echorelay/setaccount"

    res = s.post(url, json=json.dumps(account))
    
    if res.status_code != 200:
        raise Exception("Error {code}: {msg}".format(code=res.status_code, msg=res.text))


@click.command()
@click.argument('ACCOUNTFILE', type=click.File('r'), nargs=-1)
@click.option('-n', '--nakama-uri', 'nkUri', required=True)
@click.option('-k', '--server-key', 'serverKey', required=True)
@click.option('-C', '--clear-auth', 'clearAuth', is_flag=True, default=False, required=False)
@click.option('-L', '--link-to', 'mainAccount')
def main(accountfile, nkUri, serverKey, clearAuth, mainAccount):
    nkUri = nkUri.strip('/')
    if (mainAccount):
        assert len(accountfile) == 1
        account = json.load(accountfile[0])
        
        load_account(nkUri, serverKey, account, mainAccount)
        return
        
    for path in accountfile:
        account = json.load(path)

        print('Migrating {} ({})'.format(
            account['profile']['client']['xplatformid'],
            account['profile']['client']['displayname']
        ))

        if (clearAuth == True):
            account['account_lock_hash'] = None
            account['account_lock_salt'] = None

        load_account(nkUri, serverKey, account)

if __name__ == "__main__":
    main()
