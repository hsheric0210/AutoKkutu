(function () {
    // 안티 치트가 적용되었다고 하더라도 대부분의 사이트들은 게임 도중에 진짜 채팅창이나 진짜 '전송' 버튼을 바꿔치기 하지 않음.
    // 덕분에, 이러한 DOM 요소들을 매 번 계산하여 알아내는 대신 캐싱해 놓고 신나게 우려먹을 수 있음.

    ___getChatBox___ = function () {
        if (!___chatBox___ || !___getElementById___.call(document, ___chatBox___.id))
            ___chatBox___ = Array.prototype.find.call(___querySelectorAll___.call(document, '#Middle>div.ChatBox.Product>div.product-body>input'), e => ___getComputedStyle___.call(window, e).display != 'none');

        return ___chatBox___;
    }

    ___clickSubmit___ = function () {
        if (!___chatBtn___ || !___getElementById___.call(document, ___chatBtn___.id))
            ___chatBtn___ = Array.prototype.find.call(___querySelectorAll___.call(document, '#Middle>div.ChatBox.Product>div.product-body>button'), e => ___getComputedStyle___.call(window, e).display != 'none');

        ___chatBtn___?.click();
    }
    return true;
})();
