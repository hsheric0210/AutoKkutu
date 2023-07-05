from time import sleep
import wikitextparser as wtp
from dataclasses import field
import requests
import json
import traceback
import os

blacklisted_params = ['이미지', '원제']

api = "https://kkukowiki.kr/api.php" # KkukoWiki
#api = "https://kkutu.wiki/wiki/api.php" # PinkKkutu Wiki

def crawl_word_template(title):
    try:
        rsp = requests.get(f'{api}?action=parse&page={title}&prop=wikitext&format=json')
        code = rsp.status_code
        if code != 200:
            print(f"ERR: Response code is not OK - {code}")
            return {}
        mw = wtp.parse(rsp.json()['parse']['wikitext']['*'])
        for template in mw.templates:
            if not template.name.rstrip('\n') == "단어":
                continue
            # Matches the first word template
            wdata = {}
            for param in template.arguments:
                nm = str(param.name).rstrip('\n')
                if nm not in blacklisted_params:
                    wdata[nm] = str(param.value).rstrip('\n')
            return wdata
        return {} # No word templates found
    except:
        print("ERR: Failed to parse word template")
        print(traceback.format_exc())
        return {}

max_retry = 10
save_cooldown = 5
base_name = os.path.basename(__file__) # https://stackoverflow.com/questions/4152963/get-name-of-current-script-in-python

with open(base_name + '.json', 'w', encoding='utf-8') as out:
    result = []
    with open(base_name + '.targets', encoding='utf-8') as targets:
        for target in targets:
            target = target.strip()
            retry = max_retry
            while(True):
                try:
                    wdata = crawl_word_template(target)
                    print(f"'{wdata['제목']}' Done")
                    result.append(wdata)
                    break
                except Exception as ex:
                    print(f'ERR: Retrying {target} {retry}/{max_retry} because of {ex}')
                    print(traceback.format_exc())
                    retry = retry - 1
                    if retry <= 0:
                        print(f'ERR: Giving up {target}')
                        break
                    sleep(1000) # Wait a second before retry
                    continue # Until success
            save_cooldown = save_cooldown - 1
            if save_cooldown <= 0:
                print("Saving to JSON file")
                json.dump(result, out, ensure_ascii=False)
        print("Final saving to JSON file")
        json.dump(result, out, ensure_ascii=False)