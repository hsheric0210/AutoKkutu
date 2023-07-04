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

with open(base_name + '.unknown.json', 'w', encoding='utf-8') as uout:
    with open(base_name + '.injeong.json', 'w', encoding='utf-8') as iout:
        with open(base_name + '.noninjeong.json', 'w', encoding='utf-8') as niout:
            with open(base_name + '.nodes.json', 'w', encoding='utf-8') as nout:
                unknown_word = set()
                injeong_word = set()
                noninjeong_word = set()
                nodes = set()
                with open(base_name + '.targets', encoding='utf-8') as targets:
                    for target in targets:
                        target = target.strip()
                        retry = max_retry
                        while(True):
                            try:
                                unknownInjeong = False
                                rsp = requests.get(f'https://kkukowiki.kr/api.php?action=parse&page={target}&prop=wikitext&format=json')
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

                                    if '어인정' not in td[0]:
                                        unknownInjeong = True
                                        ij_index = 0
                                    else:
                                        ij_index = td[0].index('어인정')

                                    for entry in td[1:]: # Skip first header entry
                                        word = entry[windex].lstrip('[[').rstrip(']]')
                                        if unknownInjeong:
                                            unknown_word.add(word)
                                        elif entry[ij_index] == 'O':
                                            injeong_word.add(word)
                                        elif entry[ij_index] == 'X':
                                            noninjeong_word.add(word)
                                        else:
                                            print(f"unknown injeong status: {entry[ij_index]} of word {word}")
                                            unknown_word.add(word)
                                        
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
                    json.dump(list(unknown_word), uout, ensure_ascii=False)
                    json.dump(list(injeong_word), iout, ensure_ascii=False)
                    json.dump(list(noninjeong_word), niout, ensure_ascii=False)
                    json.dump(list(nodes), nout, ensure_ascii=False)
