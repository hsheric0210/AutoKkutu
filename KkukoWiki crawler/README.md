# 끄코위키 단어 크롤러

## 단일 단어 상세 정보 크롤링 - crawl_single_word_details.py

`crawl_single_word_details.py.targets` 파일을 생성하고, 해당 파일에 정보를 가져올 단어들의 목록을 줄바꿈으로 구분하여 작성합니다.
(이 때, 문서 이름만이 포함되어야 합니다. 위키 주소가 포함되면 안됩니다.)

그 후, `py crawl_single_word_details.py` 를 통해 프로그램을 실행시킵니다.

## 단어 목록 문서 크롤링 - crawl_word_list_doc.py

`crawl_word_list_doc.py.targets` 파일을 생성하고, 해당 파일에 정보를 가져올 단어 목록 문서들 (예시: 'https://kkukowiki.kr/w/공격단어/한국어/ㄱ' -> '공격단어/한국어/ㄱ')을 작성합니다.
(이 때, 문서 이름만이 포함되어야 합니다. 위키 주소가 포함되면 안됩니다.)

그 후, `py crawl_single_word_details.py` 를 통해 프로그램을 실행시킵니다.

단어 목록이 (구분 가능한 경우) 어인정 여부를 기준으로 `crawl_word_list_doc.py.injeong.json`와 `crawl_word_list_doc.py.noninjeong.json`로 나누어 출력됩니다.
만약 어인정 여부를 구분할 수 없을 경우, `crawl_word_list_doc.py.unknown.json`로 단어들을 출력합니다.

한방 단어나 공격 단어 같은 '끝말'을 알 수 있는 단어 목록들의 경우, 이러한 '끝말'들 역시 같이 수집되어 `crawl_word_list_doc.py.nodes.json`로 출력됩니다.

## 분류에 속한 문서 목록 크롤링 - crawl_categories.py

`crawl_categories.py.targets` 파일을 생성하고, 해당 파일에 분류에 속한 문서 목록을 가져올 분류 (예시: 'https://kkukowiki.kr/w/분류:끝말잇기_방어단어' -> '분류:끝말잇기_방어단어')을 작성합니다.
(이 때, 분류 문서 이름만이 포함되어야 합니다. 위키 주소가 포함되면 안됩니다.)

그 후, `py crawl_categories.py` 를 통해 프로그램을 실행시킵니다.


## 미디어위키 API 제공자 주소

* [끄코위키](https://kkukowiki.kr/): https://kkukowiki.kr/api.php
* [분끄위키](https://kkutu.wiki/): https://kkutu.wiki/wiki/api.php
