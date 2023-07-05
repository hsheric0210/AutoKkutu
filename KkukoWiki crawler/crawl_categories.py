from time import sleep
from dataclasses import field
import requests
import json
import os

api = "https://kkukowiki.kr/api.php" # KkukoWiki
#api = "https://kkutu.wiki/wiki/api.php" # PinkKkutu Wiki

# Originally from https://www.mediawiki.org/wiki/API:Continue

def checkBlacklist(title):
    if qt.startswith('틀:'):
        return False
    if qt.startswith('분류:'):
        return False
    if qt.startswith('분류:'):
        return False
    
    return True

def query(title):
    request = {}
    request['action'] = 'query'
    request['list'] = 'categorymembers'
    request['cmtitle'] = title
    request['cmlimit'] = 50
    request['format'] = 'json'
    prev_continue = {}
    while True:
        # Clone original request
        req = request.copy()
        # Modify it with the values returned in the 'continue' section of the last result.
        req.update(prev_continue)
        # Call API
        result = requests.get(api, params=req).json()
        if 'error' in result:
            raise Exception(result['error'])
        if 'warnings' in result:
            print(result['warnings'])
        if 'query' in result:
            yield result['query']
        if 'continue' not in result:
            break
        prev_continue = result['continue']

base_name = os.path.basename(__file__) # https://stackoverflow.com/questions/4152963/get-name-of-current-script-in-python

with open(base_name + '.json', 'w', encoding='utf-8') as out:
    result = []
    with open(base_name + '.targets', encoding='utf-8') as targets:
        for target in targets:
            target = target.strip()
            for q in query(target):
                for qm in q['categorymembers']:
                    qt = qm['title'].strip()
                    if checkBlacklist(qt):
                        result.append(qt)
        print("Saving to JSON file")
        json.dump(result, out, ensure_ascii=False)