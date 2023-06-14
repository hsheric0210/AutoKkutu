from time import sleep
import undetected_chromedriver as uc
from undetected_chromedriver import ChromeOptions

co = ChromeOptions()
driver = uc.Chrome()
driver.get("https://www.google.com")
driver.execute_cdp_cmd('Page.addScriptToEvaluateOnNewDocument', {"source": "console.log('Hello my WS!')"})
with open('wsHookDump.js', 'r', encoding='utf-8') as js:
    driver.execute_cdp_cmd('Page.addScriptToEvaluateOnNewDocument', {"source": js.read()})
print('Wait browser Console tab on DevTools! All WebSocket messages should be logged there.')
while True:
    print('waiting for exit...')
    sleep(3000)
del driver
