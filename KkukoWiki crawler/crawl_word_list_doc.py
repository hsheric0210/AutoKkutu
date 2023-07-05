from time import sleep
import wikitextparser as wtp
from dataclasses import field
import requests
import json
import traceback
import os

max_retry = 10
save_cooldown = 5
base_name = os.path.basename(__file__) # https://stackoverflow.com/questions/4152963/get-name-of-current-script-in-python

api = "https://kkukowiki.kr/api.php" # KkukoWiki
#api = "https://kkutu.wiki/wiki/api.php" # PinkKkutu Wiki

words = []
nodes = set()
with open(base_name + '.targets', encoding='utf-8') as targets:
    for target in targets:
        target = target.strip()
        retry = max_retry
        while(True):
            try:
                unknownInjeong = False
                rsp = requests.get(f'{api}?action=parse&page={target}&prop=wikitext&format=json')
                code = rsp.status_code
                if code != 200:
                    print(f"ERR: Response code is not OK - {code} -> retrying after 3 sec")
                    sleep(5000)
                    continue
                p = wtp.parse(rsp.json()['parse']['wikitext']['*'])

                for tbl in p.tables:
                    td = tbl.data()

                    if '단어' not in td[0]:
                        continue
                    windex = td[0].index('단어')

                    if '끝말' in td[0]:
                        node_index = td[0].index('끝말')
                    else:
                        node_index = 0
                    
                    if '주제' in td[0]:
                        wsubject = td[0].index('주제')
                    else:
                        wsubject = 0

                    if '어인정' not in td[0]:
                        unknownInjeong = True
                        ij_index = 0
                    else:
                        ij_index = td[0].index('어인정')

                    for entry in td[1:]: # Skip first header entry
                        word = entry[windex].lstrip('[[').rstrip(']]')
                        subject = ''
                        if wsubject != 0:
                            subject = entry[wsubject].lstrip('[[').rstrip(']]')

                        injeong = -1
                        if not unknownInjeong:
                            if entry[ij_index] == 'O':
                                injeong = 1
                            elif entry[ij_index] == 'X':
                                injeong = 0
                            else:
                                print(f"unknown injeong status: {entry[ij_index]} of word {word}")
                        
                        words.append({'word': word, 'injeong': injeong, 'subject': subject})
                        if node_index != 0:
                            nodes.add(entry[node_index])

                print(f"{target} Done")
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
    print("Saving to JSON file")
    with open(base_name + '.words.json', 'w', encoding='utf-8') as wout:
        json.dump(words, wout, ensure_ascii=False)
    if len(nodes) > 0:
        with open(base_name + '.nodes.json', 'w', encoding='utf-8') as nout:
            json.dump(list(nodes), nout, ensure_ascii=False)
